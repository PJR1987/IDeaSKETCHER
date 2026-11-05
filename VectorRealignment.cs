using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace IDSketcher{
partial class VectorShape{
 public bool RealignPointHandles(int index){
  if(index<0||index>=Points.Count||Kind=="Ellipse"||Kind=="Text"||Kind=="Compound")return false;
  var h=PointHandles(index);float incoming=StrokeSession.Distance(PointF.Empty,h[0]),outgoing=StrokeSession.Distance(PointF.Empty,h[1]);
  PointF direction=outgoing>.001f?h[1]:incoming>.001f?new PointF(-h[0].X,-h[0].Y):new PointF(Points[Math.Min(index+1,Points.Count-1)].X-Points[Math.Max(0,index-1)].X,Points[Math.Min(index+1,Points.Count-1)].Y-Points[Math.Max(0,index-1)].Y);
  float length=StrokeSession.Distance(PointF.Empty,direction);if(length<.001f)return false;if(incoming<.001f)incoming=outgoing>.001f?outgoing:length/6;if(outgoing<.001f)outgoing=incoming;
  var next=new[]{new PointF(-direction.X*incoming/length,-direction.Y*incoming/length),new PointF(direction.X*outgoing/length,direction.Y*outgoing/length)};
  if(StrokeSession.Distance(h[0],next[0])<.0001&&StrokeSession.Distance(h[1],next[1])<.0001)return false;Handles[index]=next;return true;
 }
}
partial class Canvas{
 public bool HideCanvasCursor;
 protected override void OnMouseDoubleClick(MouseEventArgs e){
  base.OnMouseDoubleClick(e);if(e.Button!=MouseButtons.Left||Doc==null||!Doc.Selected.IsVector||Doc.Selected.IsLocked||!Doc.Selected.Visible||VectorMode!="Select/edit")return;
  EndStroke();var p=Pixel(e.Location);var text=Doc.Selected.Vectors.LastOrDefault(v=>v.Kind=="Text"&&v.Hit(p,4/Zoom));if(text!=null){if(TextRequested!=null)TextRequested(text.Points[0],text);return;}
  foreach(var shape in Doc.Selected.Vectors.AsEnumerable().Reverse())for(int i=0;i<shape.Grips().Length;i++)if(StrokeSession.Distance(p,shape.Grips()[i])*Zoom<=9){
   var trial=shape.Copy();if(!trial.RealignPointHandles(i))return;if(BeforeStroke!=null)BeforeStroke();shape.Handles=trial.Handles;ClearSelectedPaths();AddSelectedPath(shape);selectedVector=shape;selectedPoints.Clear();selectedPoints.Add(i);Doc.Selected.InvalidateVectors();if(AfterStroke!=null)AfterStroke();NotifyPointSelection();Invalidate();return;
  }
 }
 public void TestPointDoubleClick(PointF p){var q=ScreenPoint(p);OnMouseDoubleClick(new MouseEventArgs(MouseButtons.Left,2,(int)q.X,(int)q.Y,0));}
}
partial class Studio{
 public void TestWideStrokePerformance(){
  var shape=new VectorShape{Kind="Line",Width=500,Taper=30,Colour=Color.Black,Points=new System.Collections.Generic.List<PointF>{new PointF(30,300),new PointF(3030,300)}};
  using(var path=shape.TaperPath()){if(path.PointCount>100)throw new Exception("Straight taper geometry must remain compact");}
  using(var image=new Bitmap(3200,600))using(var g=Graphics.FromImage(image)){
   var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<20;i++){shape.Points[1]=new PointF(3030,300+i);shape.Draw(g);}watch.Stop();System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"wide-stroke-timing.txt"),"20 redraws at 500px weight, 3000px line: "+watch.ElapsedMilliseconds+" ms");
  }
 } public void TestRealignAndCrossings(){TestWideStrokePerformance();
  previewing=true;Show();SetDocument(new Document("A4",100),null);AddTypedLayer(true);canvas.Zoom=1;canvas.Offset=PointF.Empty;vectorMode.SelectedItem="Select/edit";
  var shape=new VectorShape{Kind="Curvature pen",Width=20,Taper=30,Colour=Color.Black,Points=new System.Collections.Generic.List<PointF>{new PointF(40,100),new PointF(140,100),new PointF(240,100)}};
  shape.Handles[1]=new[]{new PointF(-20,-30),new PointF(40,-10)};canvas.Doc.Selected.Vectors.Add(shape);int history=undo.Count;canvas.TestPointDoubleClick(shape.Points[1]);var h=shape.PointHandles(1);
  if(Math.Abs(h[0].X*h[1].Y-h[0].Y*h[1].X)>.01||h[0].X*h[1].X+h[0].Y*h[1].Y>=0||undo.Count!=history+1)throw new Exception("Double click alignment");
  Travel(undo,redo);if(canvas.Doc.Selected.Vectors.Last().PointHandles(1)[0]!=new PointF(-20,-30))throw new Exception("Realign undo");Travel(redo,undo);
  foreach(bool reverse in new[]{false,true})using(var image=new Bitmap(300,300))using(var g=Graphics.FromImage(image)){
   var cross=new VectorShape{Kind="Curvature pen",Taper=30,Width=26,Colour=Color.Black,Opacity=.5f,Points=new System.Collections.Generic.List<PointF>{new PointF(30,30),new PointF(250,250),new PointF(250,30),new PointF(30,250)}};
   if(reverse)cross.Points.Reverse();for(int i=0;i<cross.Points.Count;i++)cross.Handles[i]=new[]{PointF.Empty,PointF.Empty};cross.Draw(g);
   for(int y=135;y<=145;y++)for(int x=135;x<=145;x++){var pixel=image.GetPixel(x,y);if(pixel.A<120||pixel.A>130)throw new Exception("Taper crossing must have uniform opacity");}
  }
  canvas.Doc.Active=0;RefreshLayers();cursorButtons[2].PerformClick();if(!canvas.HideCanvasCursor||canvas.BrushShapeCursor)throw new Exception("No cursor option");cursorButtons[0].PerformClick();
  using(var icon=Icon.ExtractAssociatedIcon(Application.ExecutablePath))if(icon==null||Icon==null)throw new Exception("Application icon");dirty=false;
 }
}
}
