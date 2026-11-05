using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace IDSketcher{
partial class Studio{ public void TestColourCursorTools(){previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;cursorButtons[1].PerformClick();if(canvas.BrushShapeCursor)throw new Exception("Crosshair button");cursorButtons[0].PerformClick();if(!canvas.BrushShapeCursor||cursorMode.Parent!=null)throw new Exception("Brush cursor button");SelectTool("Airbrush");size.Value=500;if(canvas.BrushSize!=500)throw new Exception("Airbrush max size");mode.SelectedItem="RGB";colourEntry.Text="#1234AB";ApplyColourEntry();if(canvas.Ink.ToArgb()!=Color.FromArgb(18,52,171).ToArgb()||colourEntry.Text!="#1234AB")throw new Exception("HEX colour entry");using(var g=Graphics.FromImage(canvas.Doc.Selected.Image))g.Clear(Color.Coral);eyedropperButton.PerformClick();canvas.TestDrag(new Point(80,80),new Point(80,80));if(canvas.EyedropperActive||canvas.Ink.ToArgb()!=Color.Coral.ToArgb())throw new Exception("Eyedropper");canvas.SetSelectionMode("Polygon selection");canvas.TestDrag(new Point(20,20),new Point(20,20));canvas.TestDrag(new Point(150,20),new Point(150,20));canvas.TestDrag(new Point(80,100),new Point(80,100));canvas.TestRightClick();if(!canvas.HasSelection)throw new Exception("Right click closes selection");AddTypedLayer(true);vectorMode.SelectedItem="Line";if(pointSpacing.Enabled||pointSpacingLabel.Enabled)throw new Exception("Spacing disabled");vectorMode.SelectedItem="Freehand";if(!pointSpacing.Enabled||!pointSpacingLabel.Enabled)throw new Exception("Spacing enabled");if(vectorWidth.Parent!=strokeValueBox||vectorWeightSlider.Parent!=null)throw new Exception("Weight control placement");canvas.TestPolygonAlignment();dirty=false;}
 Button[] cursorButtons;Button eyedropperButton;Control weightCaption;
 void AddCursorButtons(FlowLayoutPanel tools){
  var row=new FlowLayoutPanel{Width=120,Height=38,WrapContents=false,Margin=Padding.Empty};cursorButtons=new Button[3];string[] glyphs={"X","O","+"};
  for(int i=0;i<3;i++){int index=i==0?2:i==1?0:1;var b=Theme.Button(glyphs[i],delegate{cursorMode.SelectedIndex=index;canvas.Focus();});b.Size=new Size(32,32);b.Margin=new Padding(2);b.AccessibleName=index==0?"Brush shape":index==1?"Crosshairs":"No cursor";b.Text="";b.Image=index==2?CursorOptionIcon(index):CursorTextIcon(index);b.ImageAlign=ContentAlignment.MiddleCenter;b.TextAlign=ContentAlignment.MiddleCenter;cursorButtons[index]=b;row.Controls.Add(b);}
  cursorMode.SelectedIndexChanged+=delegate{SyncCursorButtons();};tools.Controls.Add(row);SyncCursorButtons();
 }
 void SyncCursorButtons(){if(cursorButtons==null)return;for(int i=0;i<3;i++){cursorButtons[i].BackColor=cursorMode.SelectedIndex==i?Theme.Accent:Theme.Panel;cursorButtons[i].ForeColor=Theme.Text;}}
 void ConfigureColourTools(Panel colour){
  var icon=new Bitmap(24,24);using(var g=Graphics.FromImage(icon))using(var pen=new Pen(Color.Teal,3)){g.SmoothingMode=SmoothingMode.AntiAlias;g.DrawLine(pen,5,20,18,7);g.DrawLine(pen,12,5,21,14);g.FillEllipse(Brushes.Teal,15,2,7,7);}
  eyedropperButton=Theme.Button("",delegate{canvas.EndStroke();canvas.EyedropperActive=!canvas.EyedropperActive;eyedropperButton.BackColor=canvas.EyedropperActive?Theme.Accent:Theme.Panel;canvas.Cursor=Cursors.Cross;canvas.Focus();});
  eyedropperButton.Image=icon;eyedropperButton.AccessibleName="Eyedropper";eyedropperButton.SetBounds(188,72,32,32);colour.Controls.Add(eyedropperButton);
  savedColoursButton.Text="";savedColoursButton.AccessibleName="SAVED COLOURS";savedColoursButton.Image=MixingPaletteIcon();savedColoursButton.ImageAlign=ContentAlignment.MiddleCenter;savedColoursButton.SetBounds(188,112,32,32);shiftedColourControls=shiftedColourControls.Where(c=>c!=savedColoursButton).ToArray();canvas.CanvasColourPicked=delegate(Color picked){canvas.Ink=picked;wheel.SetColor(picked);SyncColour();canvas.ApplyVectorColour();eyedropperButton.BackColor=Theme.Panel;canvas.RefreshCursor();};
  vectorWeightSlider.Parent=null;vectorControls.Remove(vectorWeightSlider);weightCaption=(Control)vectorWeightSlider.Tag;weightCaption.Parent=colour;weightCaption.Text="Weight";weightCaption.SetBounds(164,303,72,16);
  vectorWidth.Parent=colour;vectorWidth.SetBounds(164,320,72,25);vectorWidth.AccessibleName="Stroke weight";vectorControls.Add(vectorWidth);
  for(int i=0;i<vectorFillButtons.Length;i++)vectorFillButtons[i].SetBounds(i*82,312,78,32);
 }
}
partial class Canvas{
 public bool EyedropperActive;public Action<Color> CanvasColourPicked;
 void PickCanvasColour(PointF p){if(Doc==null||p.X<0||p.Y<0||p.X>=Doc.Width||p.Y>=Doc.Height)return;using(var image=Doc.Flatten()){Color picked=image.GetPixel((int)p.X,(int)p.Y);EyedropperActive=false;if(CanvasColourPicked!=null)CanvasColourPicked(picked);}}
 static System.Collections.Generic.List<PointF> AlignedPolygon(PointF centre,PointF edge,int sides,Keys modifiers){
  if((modifiers&(Keys.Control|Keys.Shift))==0)return RegularPolygon(centre,edge,sides);
  double angle=(modifiers&Keys.Control)!=0?-Math.PI/sides:Math.PI/2-Math.PI/sides,radius=StrokeSession.Distance(centre,edge);
  return RegularPolygon(centre,new PointF(centre.X+(float)(radius*Math.Cos(angle)),centre.Y+(float)(radius*Math.Sin(angle))),sides);
 }
 public void TestPolygonAlignment(){
  foreach(int sides in new[]{3,4,5,6,7,8}){var p=AlignedPolygon(new PointF(100,100),new PointF(200,180),sides,Keys.Shift);if(Math.Abs(p[0].Y-p[1].Y)>.001||p.Any(q=>q.Y>p[0].Y+.001))throw new Exception("Horizontal bottom edge");p=AlignedPolygon(new PointF(100,100),new PointF(200,180),sides,Keys.Control);if(Math.Abs(p[0].X-p[1].X)>.001)throw new Exception("Vertical edge");}
 }
}
}
