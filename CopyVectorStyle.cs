using System;using System.Drawing;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;
namespace IDSketcher{
partial class Canvas{
 readonly List<VectorShape> selectionOrder=new List<VectorShape>();
 void ClearSelectedPaths(){selectedPaths.Clear();selectionOrder.Clear();}
 bool AddSelectedPath(VectorShape shape){if(!selectedPaths.Add(shape))return false;selectionOrder.Add(shape);return true;}
 bool RemoveSelectedPath(VectorShape shape){selectionOrder.Remove(shape);return selectedPaths.Remove(shape);}
 public void CopySelectedStyle(){
  if(Doc==null||!Doc.Selected.IsVector||Doc.Selected.IsLocked||!Doc.Selected.Visible)return;
  var ordered=selectionOrder.Where(s=>selectedPaths.Contains(s)&&Doc.Selected.Vectors.Contains(s)).ToList();if(ordered.Count<2)return;
  var source=ordered[0];var targets=ordered.Skip(1).Where(s=>s.Colour.ToArgb()!=source.Colour.ToArgb()||s.Width!=source.Width||s.Opacity!=source.Opacity||s.CornerRadius!=source.CornerRadius||s.Taper!=source.Taper||s.Filled!=source.Filled||s.Fill.Mode!=source.Fill.Mode||s.Fill.Start.ToArgb()!=source.Fill.Start.ToArgb()).ToArray();if(targets.Length==0)return;
  if(BeforeStroke!=null)BeforeStroke();foreach(var shape in targets){shape.Colour=source.Colour;shape.Width=source.Width;shape.Opacity=source.Opacity;shape.CornerRadius=source.CornerRadius;shape.Taper=source.Taper;shape.Filled=source.Filled;shape.Fill=source.Fill.Copy();}
  Doc.Selected.InvalidateVectors();if(AfterStroke!=null)AfterStroke();if(VectorSelected!=null)VectorSelected();Invalidate();
 }
 public void TestStylePick(PointF p,bool add){PickVector(p,add?Keys.Shift:Keys.None);VectorUp(p);}
}
partial class Studio{
 Button copyStyleButton;
 public void TestCopyStyle(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);AddTypedLayer(true);canvas.Zoom=1;canvas.Offset=PointF.Empty;vectorMode.SelectedItem="Select/edit";
  var target=new VectorShape{Kind="Polygon",Colour=Color.Red,Width=4,Opacity=1,Points=new List<PointF>{new PointF(400,200),new PointF(500,200),new PointF(500,300),new PointF(400,300)}};
  var source=new VectorShape{Kind="Line",Colour=Color.Blue,Width=18,Opacity=.65f,Taper=43,Fill=new VectorFillStyle{Mode=1,Start=Color.Gold},Points=new List<PointF>{new PointF(100,100),new PointF(300,100)}};
  canvas.Doc.Selected.Vectors.Add(target);canvas.Doc.Selected.Vectors.Add(source);
  canvas.TestStylePick(new PointF(200,100),false);canvas.TestStylePick(new PointF(450,200),true);int history=undo.Count;var geometry=target.Points.ToArray();copyStyleButton.PerformClick();
  if(undo.Count!=history+1||target.Colour!=source.Colour||target.Width!=source.Width||target.Opacity!=source.Opacity||target.Taper!=source.Taper||target.Fill.Mode!=1||target.Fill.Start!=Color.Gold||!target.Points.SequenceEqual(geometry)||ReferenceEquals(target.Fill,source.Fill))throw new Exception("Copy first-selected style and preserve geometry");
  Travel(undo,redo);if(canvas.Doc.Selected.Vectors[0].Colour!=Color.Red||canvas.Doc.Selected.Vectors[0].Fill.Mode!=0)throw new Exception("Copy style undo");Travel(redo,undo);if(canvas.Doc.Selected.Vectors[0].Taper!=43)throw new Exception("Copy style redo");
  if(copyStyleButton.Text!="COPY STYLE"||simplifyPaths.Text!="SIMPLIFY"||copyStyleButton.TextAlign!=ContentAlignment.MiddleCenter||simplifyPaths.TextAlign!=ContentAlignment.MiddleCenter)throw new Exception("Style/simplify labels");dirty=false;
 }
}
}
