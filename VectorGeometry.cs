using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;
namespace IDSketcher {
partial class Studio {
 public void TestVersion18(){
  SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;
  foreach(string tool in new[]{"Marker","Paintbrush","Eraser"}){SelectTool(tool);int min=tool=="Eraser"?1:5,max=tool=="Marker"?100:tool=="Paintbrush"?150:200;if(size.Minimum!=min||size.Maximum!=max)throw new Exception("New tool limits");canvas.TestRightClick();if(brushPopup.SizeValue.Minimum!=min||brushPopup.SizeValue.Maximum!=max)throw new Exception("Popup tool limits");brushPopup.Close();}
  guideMode.SelectedItem="Spline";guideMode.SelectedItem="Freehand";canvas.Focus();var msg=new System.Windows.Forms.Message();ProcessCmdKey(ref msg,System.Windows.Forms.Keys.Tab);if(canvas.Guide!="Spline"||!canvas.EditGuide)throw new Exception("Tab recalls last guide");ProcessCmdKey(ref msg,System.Windows.Forms.Keys.Tab);if(canvas.EditGuide)throw new Exception("Tab returns to guide drawing");guideMode.SelectedItem="Freehand";
  AddTypedLayer(true);vectorMode.SelectedItem="Curvature pen";canvas.TestDrag(new Point(100,100),new Point(100,100));canvas.TestDrag(new Point(200,200),new Point(200,200));canvas.TestDrag(new Point(300,100),new Point(300,100));canvas.Focus();ProcessCmdKey(ref msg,System.Windows.Forms.Keys.Enter);var spline=canvas.Doc.Selected.Vectors.Last();if(spline.Kind!="Curvature pen"||spline.Points.Count!=3||!spline.Hit(new PointF(200,200),1))throw new Exception("Spline through click points");vectorPicker.PerformClick();canvas.TestDrag(new Point(200,200),new Point(200,200));canvas.TestDrag(new Point(200,200),new Point(210,220));if(spline.Points[1]!=new PointF(210,220))throw new Exception("Spline point editing");
  canvas.TestVectorRotationDrag();
  var ellipse=new VectorShape{Kind="Ellipse",Colour=Color.Red,Points=new System.Collections.Generic.List<PointF>{new PointF(100,100),new PointF(300,200)}};ellipse.Rotate(45);PointF c,u,v;ellipse.EllipseAxes(out c,out u,out v);var grip=ellipse.Grips()[0];ellipse.SetGrip(0,new PointF(c.X+(grip.X-c.X)*2,c.Y+(grip.Y-c.Y)*2));PointF c2,u2,v2;ellipse.EllipseAxes(out c2,out u2,out v2);if(StrokeSession.Distance(c,c2)>.001||StrokeSession.Distance(v,v2)>.001||Math.Abs(StrokeSession.Distance(PointF.Empty,u2)-200)>.01)throw new Exception("Rotated ellipse axis resizing");canvas.Doc.Selected.Vectors.Add(ellipse);
  string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),Guid.NewGuid()+".idsketch");try{canvas.Doc.Save(path);using(var loaded=Document.Load(path)){var list=loaded.Selected.Vectors;if(list[0].Kind!="Curvature pen"||list.Last().Points.Count!=3||Document.Png(loaded.Selected.Rendered()).Length==0)throw new Exception("Spline and rotated ellipse persistence");}}finally{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}dirty=false;
 }
}
partial class Canvas {
 public void TestVectorRotationDrag(){var shape=selectedVector;var first=shape.Points[0];var centre=shape.CentrePoint();var grip=VectorRotationGrip();var end=new PointF(centre.X-(grip.Y-centre.Y),centre.Y+(grip.X-centre.X));TestDrag(Point.Round(grip),Point.Round(end));if(StrokeSession.Distance(shape.Points[0],first)<5)throw new Exception("Rotation handle drag");}
}
partial class VectorShape {
 public void EllipseAxes(out PointF c,out PointF u,out PointF v){if(Points.Count==3){c=Points[0];u=new PointF(Points[1].X-c.X,Points[1].Y-c.Y);v=new PointF(Points[2].X-c.X,Points[2].Y-c.Y);}else{c=new PointF((Points[0].X+Points[1].X)/2,(Points[0].Y+Points[1].Y)/2);u=new PointF(Math.Max(.01f,Math.Abs(Points[1].X-Points[0].X)/2),0);v=new PointF(0,Math.Max(.01f,Math.Abs(Points[1].Y-Points[0].Y)/2));}}
 public PointF CentrePoint(){if(Kind=="Ellipse"){PointF c,u,v;EllipseAxes(out c,out u,out v);return c;}return new PointF(Points.Average(p=>p.X),Points.Average(p=>p.Y));}
 public PointF[] Grips(){if(Kind!="Ellipse")return Points.ToArray();PointF c,u,v;EllipseAxes(out c,out u,out v);return new[]{new PointF(c.X+u.X,c.Y+u.Y),new PointF(c.X-u.X,c.Y-u.Y),new PointF(c.X+v.X,c.Y+v.Y),new PointF(c.X-v.X,c.Y-v.Y)};}
 public void StoreAxes(PointF c,PointF u,PointF v){Points.Clear();Points.Add(c);Points.Add(new PointF(c.X+u.X,c.Y+u.Y));Points.Add(new PointF(c.X+v.X,c.Y+v.Y));}
 public void SetGrip(int index,PointF p){if(Kind!="Ellipse"){Points[index]=p;return;}PointF c,u,v;EllipseAxes(out c,out u,out v);PointF a=index<2?u:v;double len=Math.Sqrt(a.X*a.X+a.Y*a.Y);float scale=(float)(Math.Max(1,Math.Abs(((p.X-c.X)*a.X+(p.Y-c.Y)*a.Y)/len))/len);a=new PointF(a.X*scale,a.Y*scale);if(index<2)u=a;else v=a;StoreAxes(c,u,v);}
 public void Rotate(float degrees){if(Kind=="Ellipse"&&Points.Count==2){PointF c,u,v;EllipseAxes(out c,out u,out v);StoreAxes(c,u,v);}var centre=CentrePoint();var pts=Points.ToArray();using(var m=new Matrix()){m.RotateAt(degrees,centre);m.TransformPoints(pts);TransformHandles(m);}Points=new System.Collections.Generic.List<PointF>(pts);}
}
}
