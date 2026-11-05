using System;using System.Drawing;using System.Collections.Generic;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.Linq;using System.IO;
namespace IDSketcher{
partial class Canvas{
 void BeginVectorPen(PointF p){
  if(draft==null)draft=new VectorShape{Kind="Curvature pen",Colour=Ink,Width=VectorWidth,Opacity=VectorOpacity,Taper=VectorTaper,Fill=VectorFill.Copy()};
  if(draft.Points.Count==0||StrokeSession.Distance(p,draft.Points.Last())>.001f){draft.Points.Add(p);draft.Handles[draft.Points.Count-1]=new[]{PointF.Empty,PointF.Empty};}
  vectorDragging=true;Capture=true;Invalidate();
 }
 void MoveVectorPen(PointF p){
  int i=draft.Points.Count-1;var anchor=draft.Points[i];var h=new PointF(p.X-anchor.X,p.Y-anchor.Y);
  var handles=(PointF[])draft.Handles[i].Clone();handles[1]=h;if((ModifierKeys&Keys.Alt)==0)handles[0]=new PointF(-h.X,-h.Y);draft.Handles[i]=handles;Invalidate();
 }
 void PaintVectorPenDraft(Graphics g){
  if(VectorMode!="Vector pen"||draft==null)return;
  using(var pen=new Pen(Color.Teal,1/Zoom))for(int i=0;i<draft.Points.Count;i++){
   var a=draft.Points[i];g.FillRectangle(Brushes.Teal,a.X-3/Zoom,a.Y-3/Zoom,6/Zoom,6/Zoom);
   for(int side=0;side<2;side++){var h=draft.HandlePosition(i,side);g.DrawLine(pen,a,h);g.FillEllipse(Brushes.White,h.X-3/Zoom,h.Y-3/Zoom,6/Zoom,6/Zoom);g.DrawEllipse(pen,h.X-3/Zoom,h.Y-3/Zoom,6/Zoom,6/Zoom);}
  }
 }
}
partial class Studio{
 public void TestVectorPenChanges(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);AddTypedLayer(true);canvas.Zoom=1;canvas.Offset=PointF.Empty;vectorMode.SelectedItem="Vector pen";
  int history=undo.Count;canvas.TestDrag(new Point(40,100),new Point(70,60));canvas.TestDraftPaint();canvas.TestDrag(new Point(140,100),new Point(170,140));canvas.TestDrag(new Point(240,100),new Point(240,100));
  if(canvas.Doc.Selected.Vectors.Count!=0)throw new Exception("Pen must wait for Enter");
  canvas.Focus();var msg=new Message();ProcessCmdKey(ref msg,Keys.Enter);
  var shape=canvas.Doc.Selected.Vectors.Last();if(shape.Points.Count!=3||shape.Handles.Count!=3||undo.Count!=history+1)throw new Exception("Pen geometry/undo unit");
  if(shape.PointHandles(0)[1]!=new PointF(30,-40)||shape.PointHandles(2)[1]!=PointF.Empty)throw new Exception("Pen drag handle/click corner");
  using(var path=shape.Path())if(!path.PathTypes.Any(t=>(t&7)==3))throw new Exception("Pen Beziers");
  Travel(undo,redo);if(canvas.Doc.Selected.Vectors.Count!=0)throw new Exception("Pen undo");Travel(redo,undo);shape=canvas.Doc.Selected.Vectors.Last();
  shape.Handles[1]=new[]{new PointF(-30,-40),new PointF(30,40)};var opposite=shape.PointHandles(1)[0];shape.SetPointHandle(1,1,new PointF(190,100),true);if(shape.PointHandles(1)[0]!=opposite)throw new Exception("Alt must preserve opposite handle");
  shape.SetPointHandle(1,1,new PointF(180,80));if(shape.PointHandles(1)[0]!=opposite)throw new Exception("Broken alignment persists");
  var linked=new VectorShape{Kind="Curvature pen",Points=new List<PointF>{new PointF(0,0),new PointF(100,100),new PointF(200,0)}};
  linked.SetPointHandle(1,1,new PointF(130,150));var pair=linked.PointHandles(1);if(Math.Abs(pair[0].X*pair[1].Y-pair[0].Y*pair[1].X)>.01)throw new Exception("Normal drag keeps alignment");
  string file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".idsk");try{canvas.Doc.Save(file);using(var loaded=Document.Load(file)){var saved=loaded.Selected.Vectors.Last();if(saved.PointHandles(1)[0]!=opposite||saved.PointHandles(1)[1]!=shape.PointHandles(1)[1])throw new Exception("Pen handle persistence");}}finally{if(File.Exists(file))File.Delete(file);}
  vectorMode.SelectedItem="Vector pen";canvas.TestDrag(new Point(50,50),new Point(60,70));ProcessCmdKey(ref msg,Keys.Escape);if(canvas.Doc.Selected.Vectors.Count!=1)throw new Exception("Cancel pen draft");dirty=false;
 }
}
}
