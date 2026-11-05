using System;using System.Drawing;using System.Linq;using System.Windows.Forms;using System.IO;
namespace IDSketcher{
partial class Canvas{
 public void AcceptVectorDraft(){if(VectorMode=="Polyline"||VectorMode=="Vector pen"||VectorMode=="Curvature pen"){FinishVector();if(draft!=null){draft=null;Invalidate();}}}
 public void SelectAllVectors(){
  if(Doc==null||!Doc.Selected.IsVector)return;AcceptVectorDraft();ClearSelectedPaths();foreach(var shape in Doc.Selected.Vectors)AddSelectedPath(shape);
  selectedVector=Doc.Selected.Vectors.LastOrDefault();selectedPoints.Clear();VectorBoxMode=false;
  if(selectedVector!=null&&VectorSelected!=null)VectorSelected();NotifyPointSelection();Invalidate();
 }
 public int SelectedVectorCount{get{return selectedPaths.Count;}}
}
partial class Studio{
 public void TestVectorWorkflow(){
  previewing=true;Show();
  foreach(string tool in new[]{"Polyline","Vector pen","Curvature pen"})foreach(int route in new[]{0,1,2}){
   SetDocument(new Document("A4",100),null);AddTypedLayer(true);canvas.Zoom=1;canvas.Offset=PointF.Empty;vectorMode.SelectedItem=tool;
   canvas.TestDrag(new Point(40,40),new Point(40,40));canvas.TestDrag(new Point(140,140),new Point(140,140));canvas.TestDrag(new Point(240,40),new Point(240,40));
   int history=undo.Count;canvas.Focus();var msg=new Message();if(route==0)canvas.TestRightClick();else if(route==1)ProcessCmdKey(ref msg,Keys.V);else vectorPicker.PerformClick();
   if(canvas.Doc.Selected.Vectors.Count!=1||undo.Count!=history+1)throw new Exception("Accept "+tool+" route "+route);
   Travel(undo,redo);if(canvas.Doc.Selected.Vectors.Count!=0)throw new Exception("Accept undo");Travel(redo,undo);
  }
  vectorMode.SelectedItem="Line";canvas.TestDrag(new Point(300,100),new Point(400,100));canvas.Focus();var message=new Message();ProcessCmdKey(ref message,Keys.Control|Keys.A);
  if(canvas.SelectedVectorCount!=2)throw new Exception("Ctrl+A selects every vector");
  foreach(string mode in new[]{"Rectangle","Polygon"}){vectorTaper.Value=80;vectorMode.SelectedItem=mode;if(canvas.VectorTaper!=0||vectorTaper.Value!=0)throw new Exception("Zero taper "+mode);}
  vectorMode.SelectedItem="Rectangle";canvas.TestDrag(new Point(50,200),new Point(200,300));if(canvas.Doc.Selected.Vectors.Last().Taper!=0)throw new Exception("Rectangle taper");
  canvas.SelectAllVectors();canvas.ApplyVectorFill(new VectorFillStyle{Mode=1,Start=Color.Red});if(canvas.Doc.Selected.Vectors.Where(v=>v.CanFill).Any(v=>v.Fill.Mode!=1))throw new Exception("Solid fills retained");
  canvas.ApplyVectorFill(new VectorFillStyle());if(canvas.Doc.Selected.Vectors.Any(v=>v.Fill.Mode!=0))throw new Exception("No fill retained");
  if(vectorControls.Any(c=>c.Text.IndexOf("gradient",StringComparison.OrdinalIgnoreCase)>=0||c.Text=="Linear fill"||c.Text=="Radial fill"))throw new Exception("Removed fill controls");
  ShowAddLayerMenu(canvas);if(!addLayerMenu.Items.Cast<ToolStripItem>().Any(i=>i.Text=="IMPORT GLB"))throw new Exception("GLB import entry");addLayerMenu.Close();
  using(var stream=new MemoryStream()) {using(var writer=new BinaryWriter(stream,System.Text.Encoding.UTF8,true)){writer.Write(2);writer.Write(Color.Red.ToArgb());writer.Write(Color.Blue.ToArgb());writer.Write(90f);}stream.Position=0;using(var reader=new BinaryReader(stream)){var fill=VectorFillStyle.Read(reader);if(fill.Mode!=1||fill.Start.ToArgb()!=Color.Red.ToArgb())throw new Exception("Legacy fill migration");}}
  dirty=false;
 }
}
}
