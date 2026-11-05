using System;using System.Linq;using System.Collections.Generic;using System.Drawing;using System.Windows.Forms;
namespace IDSketcher {
partial class Studio {
 public void TestVersion42(){
  foreach(string name in new[]{"Pencil","Pen","Ink Pen"}){toolSettings[name].Pressure="No pressure";toolSettings[name].Taper=7;}
  preferences.Tools=toolSettings.Values.Select(t=>t.Copy()).ToList();LoadTools();
  foreach(string name in new[]{"Pencil","Pen","Ink Pen"}){SelectTool(name);if(canvas.PressureMode!="Simulated pressure"||canvas.TaperPercent!=50||taperSlider.Value!=50)throw new Exception("Startup pressure "+name);}
  pressureMode.SelectedItem="No pressure";taperSlider.Value=72;SelectTool("Pen");SelectTool("Ink Pen");if(canvas.PressureMode!="No pressure"||canvas.TaperPercent!=72)throw new Exception("Pressure remains adjustable during session");
  foreach(string selection in new[]{"Box selection","Polygon selection","Lasso selection"}){
   SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;using(var g=Graphics.FromImage(canvas.Doc.Selected.Image))g.FillRectangle(Brushes.Blue,100,100,100,60);
   selectionMode.SelectedItem=selection;canvas.TestSelectionForTransform(selection);canvas.Focus();var message=new Message();
   if(!ProcessCmdKey(ref message,Keys.S)||canvas.TransformMode!="Scale"||canvas.SelectionMode!="Draw")throw new Exception("Scale from active selection tool "+selection);
   int history=undo.Count;canvas.TestTransformGesture(2,0,false);if(undo.Count!=history+1||canvas.Doc.Selected.Image.GetPixel(240,130).B!=255||canvas.SelectionMask[130*canvas.Doc.Width+240]==0)throw new Exception("Selection scale "+selection);
   Travel(undo,redo);selectionMode.SelectedItem=selection;canvas.Focus();ProcessCmdKey(ref message,Keys.R);if(canvas.TransformMode!="Rotate")throw new Exception("Selection rotation shortcut");canvas.TestTransformGesture(1,90,false);if(canvas.Doc.Selected.Image.GetPixel(150,175).B!=255)throw new Exception("Selection rotation result "+selection);
  }
  SetDocument(new Document(CanvasPreset.All.Last()),null);AddTypedLayer(true);canvas.Zoom=.2f;canvas.Offset=PointF.Empty;canvas.TestFastVectorTransform();dirty=false;
 }
}
partial class Canvas {
 public void TestSelectionForTransform(string mode){
  if(mode=="Box selection"){TestDrag(new Point(100,100),new Point(200,160));return;}
  if(mode=="Polygon selection"){foreach(var p in new[]{new Point(100,100),new Point(200,100),new Point(200,160),new Point(100,160)})TestDrag(p,p);FinishSelection();return;}
  // Feed enough samples to the smoothed lasso to enclose the painted rectangle.
  OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,80,80,0));foreach(var end in new[]{new Point(220,80),new Point(220,180),new Point(80,180),new Point(80,80)}){for(int i=0;i<30;i++)OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,end.X,end.Y,0));}OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,80,80,0));
 }
 public void TestFastVectorTransform(){
  var shape=new VectorShape{Kind="Curvature pen",Width=8,Taper=40,Colour=Color.Blue,Points=new List<PointF>{new PointF(500,500),new PointF(1500,1200),new PointF(2500,500)}};shape.SetPointHandle(1,1,new PointF(1800,900));Doc.Selected.Vectors.Add(shape);Doc.Selected.InvalidateVectors();selectedVector=shape;ClearSelectedPaths();AddSelectedPath(shape);BeginTransform("Scale");var original=shape.Copy();var start=TransformCorners()[2];TransformDown(start);
  if(transformPreview.Width>1600||transformBase.Width>1600||transformPreview.Width>=Doc.Width)throw new Exception("Vector preview allocation must be bounded");long version=Doc.Selected.VisualVersion;using(var frame=new Bitmap(800,500))using(var g=Graphics.FromImage(frame)){for(int i=0;i<10;i++){TransformMove(new PointF(start.X+20*i,start.Y+10*i));g.ResetTransform();g.ScaleTransform(Zoom,Zoom);PaintTransformDocument(g);if(Doc.Selected.VisualVersion!=version)throw new Exception("Preview changed document cache");}}
  var end=new PointF(start.X+200,start.Y+100);TransformMove(end);var expected=original.Points.ToArray();transformMatrix.TransformPoints(expected);original.TransformHandles(transformMatrix);TransformUp(end);for(int i=0;i<expected.Length;i++)if(StrokeSession.Distance(shape.Points[i],expected[i])>.001f)throw new Exception("Exact vector commit");if(StrokeSession.Distance(shape.Handles[1][1],original.Handles[1][1])>.001f||shape.Taper!=40||shape.Width!=8)throw new Exception("Vector handles/styles retained");
 }
}
}
