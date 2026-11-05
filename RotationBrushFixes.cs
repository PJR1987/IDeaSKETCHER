using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;using System.IO;
namespace IDSketcher{
partial class Stroke{
 float[] paintbrushKernel;float paintbrushKernelRadius=-1;double paintbrushKernelScale;byte[] paintbrushAlpha;
 void PreparePaintbrushKernel(float radius){
  if(paintbrushKernel==null){paintbrushKernel=new float[8193];paintbrushAlpha=new byte[256];for(int i=0;i<256;i++)paintbrushAlpha[i]=(byte)Math.Round(i*opacity);
   for(int i=0;i<=8192;i++){double normalized=Math.Sqrt(i/8192.0),t=Math.Max(0,Math.Min(1,(normalized-hardness)/Math.Max(.0001,1-hardness)));double coverage=hardness>=1?1:1-t*t*(3-2*t);if(tool=="Airbrush")coverage*=(1-i/8192.0)*(1-i/8192.0);paintbrushKernel[i]=(float)coverage;}
  }
  if(paintbrushKernelRadius==radius)return;paintbrushKernelRadius=radius;paintbrushKernelScale=8192/Math.Max(.000001,radius*radius);
 }
 double PaintbrushCoverage(double squared,float radius){double value=paintbrushKernel[Math.Min(8192,(int)(squared*paintbrushKernelScale))];if(squared>(radius-.5)*(radius-.5)||radius<.5)value*=Math.Min(1,Math.Max(0,radius+.5-Math.Sqrt(squared)));return value;}
}partial class Studio{
 static Bitmap CursorOptionIcon(int index){var b=new Bitmap(24,24);using(var g=Graphics.FromImage(b))using(var pen=new Pen(Color.Teal,2.4f)){g.SmoothingMode=SmoothingMode.AntiAlias;if(index==1){g.DrawLine(pen,3,12,21,12);g.DrawLine(pen,12,3,12,21);}else{g.DrawEllipse(pen,5,5,14,14);if(index==2)g.DrawLine(pen,7,17,17,7);}}return b;}
 public void TestRotationAndBrushFixes(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Dock=DockStyle.None;canvas.Size=new Size(600,400);canvas.Zoom=1;canvas.Offset=PointF.Empty;
  using(var g=Graphics.FromImage(canvas.Doc.Selected.Image))g.FillRectangle(Brushes.Red,140,90,20,20);
  var vector=new Layer("Vector test",canvas.Doc.Width,canvas.Doc.Height){IsVector=true};vector.Vectors.Add(new VectorShape{Kind="Line",Colour=Color.Blue,Width=12,Points=new System.Collections.Generic.List<PointF>{new PointF(180,150),new PointF(220,150)}});canvas.Doc.Layers.Add(vector);canvas.Doc.Selected.InvalidateVectors();
  canvas.ViewAngle=90;canvas.TestRotatedArtwork();canvas.ViewAngle=-90;canvas.TestRotatedArtwork();canvas.ViewAngle=0;canvas.Dock=DockStyle.Fill;
  if(cursorButtons.Any(b=>b.Image==null)||pressureButtons[0].Image==null||cursorButtons.Any(b=>b.ImageAlign!=ContentAlignment.MiddleCenter))throw new Exception("Cursor icons");
  using(var reference=new Bitmap(200,200)){new Stroke(reference,"Paintbrush",Color.Black,150,.7f,new PointF(100,100),null,.35f,.6f);for(int y=24;y<177;y++)for(int x=24;x<177;x++){double dx=x+.5-100,dy=y+.5-100,dist=Math.Sqrt(dx*dx+dy*dy),coverage=Math.Min(1,Math.Max(0,75.5-dist)),t=Math.Max(0,Math.Min(1,(dist-45)/30));coverage*=1-t*t*(3-2*t);int deposit=(int)Math.Round(Math.Round(coverage*65535)*.35);int mask=(int)Math.Round(deposit/65535.0*255);int expected=(int)Math.Round(mask/255.0*.7*255);if(Math.Abs(reference.GetPixel(x,y).A-expected)>1)throw new Exception("Paintbrush coverage regression");}}var watch=System.Diagnostics.Stopwatch.StartNew();using(var image=new Bitmap(1200,600)){var stroke=new Stroke(image,"Paintbrush",Color.Teal,150,.7f,new PointF(100,300),null,.35f,.6f);for(int i=1;i<=1200;i++)stroke.Move(new PointF(100+i%900,300+(float)Math.Sin(i*.03)*100));}watch.Stop();File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"paintbrush-timing.txt"),"1200 moves, 150px soft paintbrush: "+watch.ElapsedMilliseconds+" ms");dirty=false;
 }
}
partial class Canvas{
 Rectangle RotatedScreenBounds(Rectangle dirty){
  var pts=new[]{new PointF(dirty.Left,dirty.Top),new PointF(dirty.Right,dirty.Top),new PointF(dirty.Right,dirty.Bottom),new PointF(dirty.Left,dirty.Bottom)};
  for(int i=0;i<pts.Length;i++)pts[i]=RotateView(new PointF(Offset.X+pts[i].X*Zoom,Offset.Y+pts[i].Y*Zoom));
  return Rectangle.Intersect(ClientRectangle,Rectangle.FromLTRB((int)Math.Floor(pts.Min(p=>p.X))-ViewSamplingPadding,(int)Math.Floor(pts.Min(p=>p.Y))-ViewSamplingPadding,(int)Math.Ceiling(pts.Max(p=>p.X))+ViewSamplingPadding,(int)Math.Ceiling(pts.Max(p=>p.Y))+ViewSamplingPadding));
 }
 public void TestRotatedArtwork(){using(var image=new Bitmap(Width,Height))using(var g=Graphics.FromImage(image)){OnPaint(new PaintEventArgs(g,ClientRectangle));var red=ScreenPoint(new PointF(150,100));var blue=ScreenPoint(new PointF(200,150));if(image.GetPixel((int)red.X,(int)red.Y).R<240||image.GetPixel((int)red.X,(int)red.Y).G>30||image.GetPixel((int)blue.X,(int)blue.Y).B<240||image.GetPixel((int)blue.X,(int)blue.Y).R>30)throw new Exception("Pixel and vector artwork must rotate with page");}}
}
}
