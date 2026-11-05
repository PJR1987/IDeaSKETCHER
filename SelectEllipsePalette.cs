using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;using System.IO;
namespace IDSketcher{
partial class Canvas{
 int ellipseStage;bool ellipseCircle;PointF ellipseCentre,ellipseAxis;
 bool EllipseSelectionDown(PointF p){return BeginEllipseSelection(p,(ModifierKeys&Keys.Shift)!=0);}
 bool BeginEllipseSelection(PointF p,bool circle){
  p=ClampSelection(p);selectionPointer=p;
  if(ellipseStage==0){ellipseCentre=p;ellipseAxis=p;ellipseCircle=circle;ellipseStage=1;selectionOperation=(ModifierKeys&Keys.Control)!=0?-1:0;selectionPoints.Clear();selectionPoints.Add(p);if(circle){selectionDragging=true;Capture=true;}}
  else if(ellipseStage==1){if(StrokeSession.Distance(p,ellipseCentre)>.1f){ellipseAxis=p;ellipseStage=2;}}
  else FinishSelection();Invalidate();return true;
 }
 GraphicsPath EllipseSelectionPath(){
  var path=new GraphicsPath();if(ellipseStage==0)return path;PointF u;
  if(ellipseCircle||ellipseStage==1)u=new PointF(selectionPointer.X-ellipseCentre.X,selectionPointer.Y-ellipseCentre.Y);else u=new PointF(ellipseAxis.X-ellipseCentre.X,ellipseAxis.Y-ellipseCentre.Y);
  float a=StrokeSession.Distance(PointF.Empty,u);if(a<.1f)return path;float b=ellipseCircle?a:Math.Abs((selectionPointer.X-ellipseCentre.X)*(-u.Y/a)+(selectionPointer.Y-ellipseCentre.Y)*(u.X/a));if(b<.1f)b=.1f;
  var v=new PointF(-u.Y/a*b,u.X/a*b);path.AddEllipse(-1,-1,2,2);using(var matrix=new Matrix(u.X,u.Y,v.X,v.Y,ellipseCentre.X,ellipseCentre.Y))path.Transform(matrix);return path;
 }
 public void TestEllipseSelection(bool circle){
  SetSelectionMode("Ellipse selection");BeginEllipseSelection(new PointF(150,150),circle);
  if(circle){selectionPointer=new PointF(200,150);selectionDragging=false;FinishSelection();}
  else{BeginEllipseSelection(new PointF(230,150),false);BeginEllipseSelection(new PointF(150,190),false);}
  if(SelectionMask==null||SelectionMask[150*Doc.Width+150]==0||SelectionMask[150*Doc.Width+210]!=(circle?0:1)||SelectionMask[205*Doc.Width+150]!=0)throw new Exception("Ellipse/circle selection");
  TestDraftPaint();ClearSelection();SetSelectionMode("Draw");
 }
}
partial class Studio{
 void AddPaletteColour(){
  Color colour;try{colour=ParseColour(colourEntry.Text,"HEX");}catch{colour=canvas.Ink;}
  if(preferences.Palette==null)preferences.Palette=new System.Collections.Generic.List<int>();int value=colour.ToArgb();if(preferences.Palette.Contains(value))return;
  if(preferences.Palette.Count>=48){MessageBox.Show(this,"The colour palette is full (48 colours).");return;}preferences.Palette.Add(value);if(!SaveTools())preferences.Palette.Remove(value);
 }
 public void TestSelectEllipsePalette(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;SelectTool("Select");int history=undo.Count;canvas.TestDrag(new Point(40,60),new Point(200,60));if(undo.Count!=history)throw new Exception("Select must not paint");
  SelectTool("Pencil");canvas.Focus();var msg=new Message();ProcessCmdKey(ref msg,Keys.Escape);if(canvas.Tool!="Select")throw new Exception("Escape selects Select");canvas.TestEllipseSelection(false);canvas.TestEllipseSelection(true);
  var before=preferences.Palette;preferences.Palette=new System.Collections.Generic.List<int>();colourEntry.Text="#12AB34";AddPaletteColour();if(preferences.Palette.Count!=1||preferences.Palette[0]!=Color.FromArgb(18,171,52).ToArgb())throw new Exception("Save palette colour");AddPaletteColour();if(preferences.Palette.Count!=1)throw new Exception("Palette duplicate");
  string file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".xml");try{preferences.Save(file);var loaded=ToolPreferences.Load(file);if(loaded.Palette.Count!=1||loaded.Palette[0]!=preferences.Palette[0])throw new Exception("Palette persistence");}finally{if(File.Exists(file))File.Delete(file);}preferences.Palette=before;
  WindowState=FormWindowState.Normal;ClientSize=new Size(SharpDpi.Px(this,1280),SharpDpi.Px(this,800));PerformLayout();using(var snapshot=new Bitmap(Width,Height)){DrawToBitmap(snapshot,new Rectangle(Point.Empty,snapshot.Size));snapshot.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"latest-ui.png"));}if(!Font.Bold||!toolButtons["Select"].Font.Bold)throw new Exception("Bold interface");dirty=false;
 }
}
}
