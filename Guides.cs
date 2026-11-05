using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IDSketcher {
// Keep the original pixels in Stroke so the completed stroke can receive an
// end taper without painting over itself or damaging pixels below the stroke.
partial class StrokeSession {
    public Rectangle TakeDirty(){return brush.TakeDirty();}
    public static bool SupportsTaper(string tool){return tool=="Pencil"||tool=="Pen"||tool=="Ink Pen";}
    readonly Stroke brush;
    readonly List<float> pressures=new List<float>();readonly List<PointF> points=new List<PointF>();
    readonly float size;
    readonly bool pressure;
    float length;
    readonly float taperAmount;
    float TipScale {get{return 1-.95f*Math.Min(1,taperAmount/.3f);}}
    public static float TaperPixels(float diameter,int percent){float amount=Math.Max(0,Math.Min(100,percent))/100f,oldMaximum=Math.Max(12,diameter*6);return amount<=.3f?oldMaximum*amount/.3f:oldMaximum+(Math.Max(50,oldMaximum*5/3)-oldMaximum)*(amount-.3f)/.7f;}
    float TaperDistance {get{return TaperPixels(size,(int)Math.Round(taperAmount*100));}}
    public float HardwarePressure=1;readonly bool hardware;
    public StrokeSession(Bitmap image,string tool,Color colour,float diameter,float opacity,PointF start,bool simulate,bool useHardware=false,float initialPressure=1,int taperPercent=50,SymmetryMap copies=null,float flow=1,float hardness=1,byte[] selectionMask=null,byte[] snapshot=null) {
        simulate=simulate&&SupportsTaper(tool);
        taperAmount=Math.Max(0,Math.Min(100,taperPercent))/100f;
        size=diameter;pressure=simulate;hardware=useHardware;HardwarePressure=initialPressure;points.Add(start);pressures.Add(initialPressure);
        brush=new Stroke(image,tool,colour,diameter*(hardware?PressureScale(initialPressure):simulate?TipScale:1),opacity,start,copies,flow,hardness,selectionMask,snapshot);
    }
    public static float Distance(PointF a,PointF b) { float dx=a.X-b.X,dy=a.Y-b.Y;return (float)Math.Sqrt(dx*dx+dy*dy); }
    public static float PressureScale(float value){return Math.Max(.03f,Math.Min(1,value));}
    public void Move(PointF p) {
        float d=Distance(points[points.Count-1],p);if(d<.001)return;
        length+=d;points.Add(p);pressures.Add(HardwarePressure);
        brush.MoveSized(p,size*(hardware?PressureScale(HardwarePressure):pressure&&taperAmount>0?TipScale+(1-TipScale)*Math.Min(1,length/TaperDistance):1));
    }
    public void Finish() {brush.FlushSoftEndpoint();
        if(fittedClosed||!pressure||taperAmount==0||points.Count<2)return;
        float taper=Math.Min(length*.45f,TaperDistance),travel=0;
        brush.Reset(points[0],size*TipScale);
        // Resample long straight segments too, so a ruler stroke has a full
        // width middle even if only its two endpoints were supplied.
        for(int i=1;i<points.Count;i++) {
            PointF from=points[i-1],to=points[i];float d=Distance(from,to);
            int steps=Math.Max(1,(int)Math.Ceiling(d/Math.Max(.5,size*.15)));
            for(int j=1;j<=steps;j++) {
                float at=travel+d*j/steps;
                float scale=TipScale+(1-TipScale)*Math.Max(0,Math.Min(1,Math.Min(at,length-at)/taper));
                brush.MoveSized(new PointF(from.X+(to.X-from.X)*j/steps,from.Y+(to.Y-from.Y)*j/steps),size*scale);
            }
            travel+=d;
        }
    }
}
static class AdvancedTests {
    static int Thickness(Bitmap b,int x){int n=0;for(int y=0;y<b.Height;y++)if(b.GetPixel(x,y).A>64)n++;return n;}
    public static void Run(){
        using(Bitmap b=new Bitmap(260,80)){var pencil=new StrokeSession(b,"Pencil",Color.Black,20,1,new PointF(20,40),true);pencil.Move(new PointF(230,40));pencil.Finish();int middle=0,start=0,end=0;for(int x=21;x<26;x++)for(int y=25;y<55;y++){if(b.GetPixel(x,y).A>0)start++;if(b.GetPixel(x+100,y).A>0)middle++;if(b.GetPixel(x+204,y).A>0)end++;}if(middle<=start||middle<=end)throw new Exception("Pencil must taper at both ends");}
        TestHardness();
        TestFlowAndTools();
        using(Bitmap b=new Bitmap(250,80)){StrokeSession s=new StrokeSession(b,"Pen",Color.Black,24,1,new PointF(20,40),true);s.Move(new PointF(225,40));s.Finish();int middle=Thickness(b,120);if(middle<22||Thickness(b,22)>=middle*.8||Thickness(b,223)>=middle*.8)throw new Exception("Pressure must taper both ends");}
        using(Bitmap b=new Bitmap(250,80)){using(Graphics g=Graphics.FromImage(b))g.Clear(Color.Red);StrokeSession s=new StrokeSession(b,"Eraser",Color.Black,24,1,new PointF(20,40),true);s.Move(new PointF(225,40));s.Finish();if(b.GetPixel(120,48).A!=0||b.GetPixel(22,48).A!=0)throw new Exception("Eraser must not simulate taper");}
        using(Canvas c=new Canvas())c.TestGuideDrawing();
        using(Canvas c=new Canvas())c.TestPenPackets();
        if(System.Runtime.InteropServices.Marshal.SizeOf(typeof(NativePen.PointerInfo))!=96||System.Runtime.InteropServices.Marshal.OffsetOf(typeof(NativePen.PenInfo),"Pressure").ToInt32()!=104)throw new Exception("Windows pen x64 layout");
        if(NativePen.Normalize(512)!=.5f||NativePen.Normalize(2048)!=1)throw new Exception("Pressure normalization");
        using(Bitmap b=new Bitmap(250,80)){
            StrokeSession s=new StrokeSession(b,"Pen",Color.Black,24,1,new PointF(20,40),false);s.HardwarePressure=.1f;s.Move(new PointF(225,40));s.Finish();if(Thickness(b,30)!=Thickness(b,200))throw new Exception("No-pressure width must be constant");
        }
        using(Bitmap a=new Bitmap(250,80))using(Bitmap b=new Bitmap(250,80)){
            Stroke first=new Stroke(a,"Pencil",Color.Black,24,1,new PointF(20,40));first.Move(new PointF(225,40));Stroke second=new Stroke(b,"Pencil",Color.Black,24,1,new PointF(20,40));second.Move(new PointF(225,40));
            int low=0,high=0,different=0;for(int x=30;x<215;x++)for(int y=34;y<46;y++){byte alpha=a.GetPixel(x,y).A;if(alpha<15)low++;if(alpha>70)high++;if(alpha!=b.GetPixel(x,y).A)different++;}
            if(low<100||high<100||different<1000)throw new Exception("Pencil needs grain gaps, dark flecks and variation between strokes");
        }
    }
    static void TestHardness(){
        foreach(string tool in new[]{"Pen","Ink Pen","Marker","Paintbrush","Eraser"})using(Bitmap hard=new Bitmap(100,100))using(Bitmap soft=new Bitmap(100,100)){
            if(tool=="Eraser"){using(Graphics g=Graphics.FromImage(hard))g.Clear(Color.Red);using(Graphics g=Graphics.FromImage(soft))g.Clear(Color.Red);}
            new Stroke(hard,tool,Color.Blue,40,1,new PointF(50,50),null,1,1);
            new Stroke(soft,tool,Color.Blue,40,1,new PointF(50,50),null,1,0);
            int a=hard.GetPixel(60,60).A,b=soft.GetPixel(60,60).A;
            if(tool=="Eraser"?b<=a:b>=a)throw new Exception("Hardness must soften edge: "+tool);
            if(soft.GetPixel(80,50).A!=(tool=="Eraser"?255:0))throw new Exception("Hardness exceeds brush footprint");
            if(tool=="Pen"&&soft.GetPixel(50,50).A<250)throw new Exception("Soft brush centre lost");
        }
        using(Bitmap hard=new Bitmap(260,100))using(Bitmap soft=new Bitmap(260,100)){
            StrokeSession a=new StrokeSession(hard,"Pen",Color.Blue,40,1,new PointF(20,50),true,false,1,50,null,.25f,1);a.Move(new PointF(230,50));a.Finish();
            StrokeSession b=new StrokeSession(soft,"Pen",Color.Blue,40,1,new PointF(20,50),true,false,1,50,null,.25f,0);b.Move(new PointF(230,50));b.Finish();
            if(soft.GetPixel(120,65).A>=hard.GetPixel(120,65).A)throw new Exception("Hardness lost during taper replay with flow");
        }
    }
    static int FlowSample(Bitmap b){int sum=0;for(int x=40;x<220;x++)for(int y=34;y<46;y++)sum+=b.GetPixel(x,y).A;return sum;}
    static void TestFlowAndTools(){
        foreach(string tool in new[]{"Pen","Ink Pen","Marker","Pencil","Eraser"}){
            using(Bitmap b=new Bitmap(260,80)){
                if(tool=="Eraser")using(Graphics g=Graphics.FromImage(b))g.Clear(Color.Red);
                StrokeSession s=new StrokeSession(b,tool,Color.Blue,24,.6f,new PointF(20,40),false,false,1,50,null,.08f);s.Move(new PointF(230,40));int first=FlowSample(b);s.Move(new PointF(20,40));s.Finish();int second=FlowSample(b);
                if(tool=="Eraser"?second>=first:second<=first)throw new Exception("Flow must build up on a repeat pass: "+tool);
                if(tool!="Eraser"&&second>154*180*12)throw new Exception("Flow exceeds opacity cap");
            }
            using(Bitmap b=new Bitmap(64,64)){
                if(tool=="Eraser")using(Graphics g=Graphics.FromImage(b))g.Clear(Color.Red);
                byte initial=b.GetPixel(32,32).A;new Stroke(b,tool,Color.Blue,20,1,new PointF(32,32),null,0);if(b.GetPixel(32,32).A!=initial)throw new Exception("Zero flow must not alter pixels");
            }
        }
        foreach(string tool in new[]{"Marker","Paintbrush","Eraser"}){
            using(Bitmap a=new Bitmap(260,80))using(Bitmap b=new Bitmap(260,80)){
                if(tool=="Eraser"){using(Graphics g=Graphics.FromImage(a))g.Clear(Color.Red);using(Graphics g=Graphics.FromImage(b))g.Clear(Color.Red);}
                StrokeSession first=new StrokeSession(a,tool,Color.Blue,24,1,new PointF(20,40),true,false,1,100);first.Move(new PointF(230,40));first.Finish();StrokeSession second=new StrokeSession(b,tool,Color.Blue,24,1,new PointF(20,40),false);second.Move(new PointF(230,40));second.Finish();
                for(int x=10;x<240;x++)for(int y=25;y<55;y++)if(a.GetPixel(x,y)!=b.GetPixel(x,y))throw new Exception("Non-pen tool was tapered: "+tool);
            }
        }
        if(!StrokeSession.SupportsTaper("Pencil")||!StrokeSession.SupportsTaper("Ink Pen"))throw new Exception("Taper eligibility");
        using(Bitmap a=new Bitmap(260,80))using(Bitmap b=new Bitmap(260,80)){
            Stroke sparse=new Stroke(a,"Pen",Color.Blue,20,1,new PointF(20,40),null,.1f);sparse.Move(new PointF(230,40));Stroke dense=new Stroke(b,"Pen",Color.Blue,20,1,new PointF(20,40),null,.1f);for(int x=21;x<=230;x++)dense.Move(new PointF(x,40));
            for(int x=30;x<220;x++)if(Math.Abs(a.GetPixel(x,40).A-b.GetPixel(x,40).A)>2)throw new Exception("Flow depends on mouse event frequency");
        }
        using(Bitmap a=new Bitmap(64,64))using(Bitmap b=new Bitmap(64,64)){
            new Stroke(a,"Pen",Color.Blue,20,1,new PointF(32,32),null,.1f);new Stroke(b,"Pen",Color.Blue,20,1,new PointF(32,32),new SymmetryMap(64,64,true,true,true,24),.1f);if(a.GetPixel(32,32)!=b.GetPixel(32,32))throw new Exception("Symmetry duplicates flow deposits at centre");
        }
        using(Bitmap b=new Bitmap(80,80)){new Stroke(b,"Ink Pen",Color.Blue,30,1,new PointF(40,40));if(b.GetPixel(48,48).A==0||b.GetPixel(48,32).A!=0)throw new Exception("Ink pen oval nib");}
        using(Bitmap b=new Bitmap(260,80)){StrokeSession s=new StrokeSession(b,"Ink Pen",Color.Blue,24,1,new PointF(20,40),true,false,1,100,null,.25f);s.Move(new PointF(230,40));s.Finish();if(Thickness(b,120)<=Thickness(b,23))throw new Exception("Ink pen flow with taper");}
    }
}
}
