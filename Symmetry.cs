using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace IDSketcher {
// One shared stroke coverage mask is used for every transformed dab. Copies
// crossing an axis do not accumulate opacity or allocate full-size bitmaps.
class SymmetryMap {
    readonly List<double[]> matrices=new List<double[]>();
    readonly float cx,cy;
    public int Count {get{return matrices.Count;}}
    public SymmetryMap(int width,int height,bool vertical,bool horizontal,bool radial,int repeats){
        if(repeats<3||repeats>24)throw new ArgumentOutOfRangeException("repeats");
        cx=width/2f;cy=height/2f;int count=radial?repeats:1;
        for(int i=0;i<count;i++){
            double angle=2*Math.PI*i/count,c=Math.Cos(angle),s=Math.Sin(angle);
            for(int x=0;x<(vertical?2:1);x++)for(int y=0;y<(horizontal?2:1);y++){
                int sx=x==0?1:-1,sy=y==0?1:-1;double[] m={c*sx,-s*sy,s*sx,c*sy};
                bool duplicate=false;foreach(double[] other in matrices){double error=0;for(int k=0;k<4;k++)error+=Math.Abs(m[k]-other[k]);if(error<.000001){duplicate=true;break;}}
                if(!duplicate)matrices.Add(m);
            }
        }
    }
    public PointF Transform(PointF point,int index){double[] m=matrices[index];double x=point.X-cx,y=point.Y-cy;return new PointF((float)(cx+m[0]*x+m[1]*y),(float)(cy+m[2]*x+m[3]*y));}
}
partial class Canvas {
    void PaintSymmetry(Graphics g){
        if(!VerticalMirror&&!HorizontalMirror&&!RadialSymmetry)return;
        GraphicsState state=g.Save();g.SetClip(new RectangleF(Offset.X,Offset.Y,Doc.Width*Zoom,Doc.Height*Zoom));
        PointF centre=ScreenPoint(new PointF(Doc.Width/2f,Doc.Height/2f));
        using(Pen p=new Pen(Color.FromArgb(115,139,87,176),1)){p.DashStyle=DashStyle.Dash;
            if(VerticalMirror)g.DrawLine(p,centre.X,Offset.Y,centre.X,Offset.Y+Doc.Height*Zoom);
            if(HorizontalMirror)g.DrawLine(p,Offset.X,centre.Y,Offset.X+Doc.Width*Zoom,centre.Y);
            if(RadialSymmetry){double radius=Math.Sqrt(Doc.Width*(double)Doc.Width+Doc.Height*(double)Doc.Height)*Zoom;
                for(int i=0;i<RadialRepeats;i++){double a=i*2*Math.PI/RadialRepeats;g.DrawLine(p,centre,new PointF(centre.X+(float)(radius*Math.Cos(a)),centre.Y+(float)(radius*Math.Sin(a))));}
            }
        }g.Restore(state);
    }
    public void TestSymmetryInput(){
        Doc=new Document("A4",100);Zoom=1;Offset=PointF.Empty;Smoothing=0;Tool="Pen";BrushSize=12;PressureMode="Simulated pressure";VerticalMirror=true;HorizontalMirror=true;int before=0,after=0;
        BeforeStroke=delegate{before++;};AfterStroke=delegate{after++;};
        try{OnMouseDown(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,50,60,0));OnMouseUp(new System.Windows.Forms.MouseEventArgs(System.Windows.Forms.MouseButtons.Left,1,150,60,0));
            foreach(Point p in new[]{new Point(100,60),new Point(Doc.Width-100,60),new Point(100,Doc.Height-60),new Point(Doc.Width-100,Doc.Height-60)})if(Doc.Selected.Image.GetPixel(p.X,p.Y).A==0)throw new Exception("Mirror input/taper replay");
            if(before!=1||after!=1)throw new Exception("Symmetry must commit one history action");
        }finally{EndStroke();Doc.Dispose();Doc=null;}
    }
}
static class SymmetryTests {
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static int WidthAt(Bitmap b,int x){int n=0;for(int y=0;y<b.Height;y++)if(b.GetPixel(x,y).A>64)n++;return n;}
    public static void Run(){
        Check(Math.Abs(StrokeSession.TaperPixels(5,30)-30)<.001&&Math.Abs(StrokeSession.TaperPixels(5,100)-50)<.001,"Taper range: former maximum at 30%, 50px at 100%");
        foreach(int n in new[]{3,6,24}){
            SymmetryMap map=new SymmetryMap(200,200,false,false,true,n);Check(map.Count==n,"Radial count");
            using(Bitmap b=new Bitmap(200,200)){
                new Stroke(b,"Pen",Color.Blue,5,.5f,new PointF(160,100),map);
                for(int i=0;i<n;i++){PointF p=map.Transform(new PointF(160,100),i);Check(b.GetPixel((int)p.X,(int)p.Y).A>100,"Radial copy pixels");}
                new Stroke(b,"Eraser",Color.Black,8,1,new PointF(160,100),map);
                for(int i=0;i<n;i++){PointF p=map.Transform(new PointF(160,100),i);Check(b.GetPixel((int)p.X,(int)p.Y).A==0,"Radial erase");}
            }
        }
        Check(new SymmetryMap(200,200,true,true,false,6).Count==4,"Both mirrors");
        Check(new SymmetryMap(200,200,true,true,true,6).Count==12,"Combined transforms remove duplicates");
        using(Bitmap b=new Bitmap(200,200)){new Stroke(b,"Pen",Color.Red,20,.5f,new PointF(100,100),new SymmetryMap(200,200,true,true,true,24));Check(Math.Abs(b.GetPixel(100,100).A-128)<=1,"Crossing copies must not multiply opacity");}
        using(Bitmap low=new Bitmap(260,80))using(Bitmap high=new Bitmap(260,80))using(Bitmap off=new Bitmap(260,80)){
            foreach(var pair in new[]{Tuple.Create(low,25),Tuple.Create(high,100),Tuple.Create(off,0)}){StrokeSession s=new StrokeSession(pair.Item1,"Pen",Color.Black,24,1,new PointF(20,40),true,false,1,pair.Item2);s.Move(new PointF(230,40));s.Finish();}
            Check(WidthAt(high,24)<WidthAt(low,24)&&WidthAt(high,226)<WidthAt(low,226),"Taper increases at both ends");
            Check(WidthAt(high,45)<WidthAt(low,45),"Higher taper extends the tapered region");
            Check(WidthAt(off,25)==WidthAt(off,120),"Zero taper constant width");
        }
        using(Canvas c=new Canvas())c.TestSymmetryInput();
    }
}
}
