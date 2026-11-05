using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;using System.IO;
namespace IDSketcher{
partial class Canvas{
 public bool ShowGrid,SnapToGrid;
 public PointF SnapVectorPoint(PointF p){if(!SnapToGrid||Doc==null||!Doc.Selected.IsVector)return p;double step=Doc.Dpi*10/25.4,cx=Doc.Width/2.0,cy=Doc.Height/2.0;return new PointF((float)(cx+Math.Round((p.X-cx)/step)*step),(float)(cy+Math.Round((p.Y-cy)/step)*step));}
 void PaintMillimetreGrid(Graphics g){
  if(!ShowGrid||Doc==null)return;var state=g.Save();g.SetClip(new RectangleF(Offset.X,Offset.Y,Doc.Width*Zoom,Doc.Height*Zoom));
  double step=Doc.Dpi*10/25.4*Zoom,cx=Offset.X+Doc.Width*Zoom/2.0,cy=Offset.Y+Doc.Height*Zoom/2.0;
  {using(var pen=new Pen(Color.FromArgb(55,70,85,100),1)){
   int left=(int)Math.Ceiling((Math.Max(0,Offset.X)-cx)/step),right=(int)Math.Floor((Math.Min(Width,Offset.X+Doc.Width*Zoom)-cx)/step);
   for(int i=left;i<=right;i++){float x=(float)(cx+i*step);g.DrawLine(pen,x,Math.Max(0,Offset.Y),x,Math.Min(Height,Offset.Y+Doc.Height*Zoom));}
   int top=(int)Math.Ceiling((Math.Max(0,Offset.Y)-cy)/step),bottom=(int)Math.Floor((Math.Min(Height,Offset.Y+Doc.Height*Zoom)-cy)/step);
   for(int i=top;i<=bottom;i++){float y=(float)(cy+i*step);g.DrawLine(pen,Math.Max(0,Offset.X),y,Math.Min(Width,Offset.X+Doc.Width*Zoom),y);}
  }}
  using(var pen=new Pen(Color.FromArgb(100,70,85,100),1)){g.DrawLine(pen,(float)cx,Offset.Y,(float)cx,Offset.Y+Doc.Height*Zoom);g.DrawLine(pen,Offset.X,(float)cy,Offset.X+Doc.Width*Zoom,(float)cy);}g.Restore(state);
 }
 void ExpandSelectedGroups(){if(Doc==null)return;var groups=selectedPaths.Where(v=>v.GroupId!="").Select(v=>v.GroupId).ToArray();foreach(var shape in Doc.Selected.Vectors)if(groups.Contains(shape.GroupId))AddSelectedPath(shape);}
 public void GroupSelectedVectors(bool ungroup){
  if(Doc==null||!Doc.Selected.IsVector||Doc.Selected.IsLocked)return;EndStroke();ExpandSelectedGroups();
  if(!ungroup&&selectedPaths.Count>1&&selectedPaths.First().GroupId!=""&&selectedPaths.Select(v=>v.GroupId).Distinct().Count()==1)ungroup=true;if(ungroup?!selectedPaths.Any(v=>v.GroupId!=""):selectedPaths.Count<2)return;
  if(BeforeStroke!=null)BeforeStroke();string id=ungroup?"":Guid.NewGuid().ToString("N");foreach(var shape in selectedPaths)shape.GroupId=id;
  if(AfterStroke!=null)AfterStroke();NotifyPointSelection();Invalidate();
 }
 public void TestGroupPick(PointF p){ClearSelectedPaths();selectedVector=null;PickVector(p,Keys.None);VectorUp(p);}
}
partial class Studio{
 public void TestGridAndGroups(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);AddTypedLayer(true);canvas.Zoom=1;canvas.Offset=PointF.Empty;canvas.Focus();var msg=new Message();
  ProcessCmdKey(ref msg,Keys.Alt|Keys.G);if(!canvas.ShowGrid)throw new Exception("G grid");ProcessCmdKey(ref msg,Keys.Shift|Keys.G);if(!canvas.SnapToGrid)throw new Exception("Shift G snap");
  var centre=new PointF(canvas.Doc.Width/2f,canvas.Doc.Height/2f);var snapped=canvas.SnapVectorPoint(new PointF(centre.X+42f,centre.Y+76f));
  if(Math.Abs(snapped.X-centre.X-canvas.Doc.Dpi*10/25.4)>.001||Math.Abs(snapped.Y-centre.Y-2*canvas.Doc.Dpi*10/25.4)>.001)throw new Exception("1mm centre snap");
  canvas.TestDraftPaint();vectorMode.SelectedItem="Line";canvas.TestDrag(new Point(70,80),new Point(220,80));var line=canvas.Doc.Selected.Vectors.Last();if(line.Points.Any(p=>p!=canvas.SnapVectorPoint(p)))throw new Exception("Draw snaps");
  canvas.TestDrag(new Point(70,180),new Point(220,180));vectorMode.SelectedItem="Select/edit";canvas.SelectAllVectors();int history=undo.Count;canvas.Focus();ProcessCmdKey(ref msg,Keys.Control|Keys.G);
  if(undo.Count!=history+1||canvas.Doc.Selected.Vectors.Any(v=>v.GroupId=="")||canvas.Doc.Selected.Vectors.Select(v=>v.GroupId).Distinct().Count()!=1)throw new Exception("Group");
  canvas.TestGroupPick(new PointF(140,line.Points[0].Y));if(canvas.SelectedVectorCount!=2)throw new Exception("Pick group");
  var before=canvas.Doc.Selected.Vectors.Select(v=>v.Points[0]).ToArray();canvas.TestDrag(new Point(140,(int)line.Points[0].Y),new Point(220,(int)line.Points[0].Y+80));var delta0=new PointF(canvas.Doc.Selected.Vectors[0].Points[0].X-before[0].X,canvas.Doc.Selected.Vectors[0].Points[0].Y-before[0].Y);var delta1=new PointF(canvas.Doc.Selected.Vectors[1].Points[0].X-before[1].X,canvas.Doc.Selected.Vectors[1].Points[0].Y-before[1].Y);if(StrokeSession.Distance(delta0,delta1)>.001||StrokeSession.Distance(delta0,PointF.Empty)<1)throw new Exception("Group move");string file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".idsk");try{canvas.Doc.Save(file);using(var loaded=Document.Load(file))if(loaded.Selected.Vectors.Select(v=>v.GroupId).Distinct().Count()!=1||loaded.Selected.Vectors[0].GroupId=="")throw new Exception("Group persistence");}finally{File.Delete(file);}
  canvas.Focus();ProcessCmdKey(ref msg,Keys.Control|Keys.G);if(canvas.Doc.Selected.Vectors.Any(v=>v.GroupId!=""))throw new Exception("Ungroup");
  Travel(undo,redo);if(canvas.Doc.Selected.Vectors.Any(v=>v.GroupId==""))throw new Exception("Ungroup undo");Travel(redo,undo);if(canvas.Doc.Selected.Vectors.Any(v=>v.GroupId!=""))throw new Exception("Ungroup redo");
  canvas.ShowGrid=false;canvas.SnapToGrid=false;dirty=false;
 }
}
}
