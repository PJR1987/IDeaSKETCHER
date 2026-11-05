using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.Linq;
namespace IDSketcher{
partial class Studio{
 void ConfigureToolCaptions(FlowLayoutPanel top){
  foreach(var pair in toolButtons){string text=pair.Value.Text;string key=pair.Key=="Select"?"V":text.Substring(0,1);string title=pair.Key=="Select"?"SELECT":text.Substring(2);FormatToolButton(pair.Value,title);AddShortcutBox(pair.Value,key,title);}
  FormatToolButton(pageFillButton,"FILL");AddShortcutBox(pageFillButton,"F","FILL");FormatToolButton(fillBucketButton,"VISIBLE");AddShortcutBox(fillBucketButton,"K","VISIBLE");FormatToolButton(vectorPicker,"SELECT");AddShortcutBox(vectorPicker,"V","SELECT");
  string[] keys={"F","L","O","P","C","R","E","G","T"};int index=0;foreach(var pair in vectorButtons){string title=UppercaseUI.Caption(pair.Key);FormatToolButton(pair.Value,title);AddShortcutBox(pair.Value,keys[index++],title);}foreach(Button b in threeDControls.OfType<Button>())b.TextAlign=ContentAlignment.MiddleCenter;
  foreach(Button b in top.Controls.OfType<Button>())if(b.Text.StartsWith("EXPORT")){b.Text="EXPORT";b.TextAlign=ContentAlignment.MiddleCenter;}
 }
 static void FormatToolButton(Button b,string caption){
  b.Text=caption;b.TextAlign=ContentAlignment.MiddleLeft;b.ImageAlign=ContentAlignment.MiddleRight;b.TextImageRelation=TextImageRelation.Overlay;b.Padding=new Padding(4,0,3,0);
  if(b.Image!=null){var original=b.Image;b.Image=new Bitmap(original,new Size(20,20));original.Dispose();}
 }
 static Bitmap MixingPaletteIcon(){
  var image=new Bitmap(24,24);using(var g=Graphics.FromImage(image))using(var outline=new Pen(Color.Teal,1.5f))using(var path=new GraphicsPath()){
   g.SmoothingMode=SmoothingMode.AntiAlias;path.AddClosedCurve(new[]{new PointF(2,12),new PointF(5,5),new PointF(13,2),new PointF(21,7),new PointF(22,15),new PointF(18,21),new PointF(12,20),new PointF(13,15),new PointF(8,15),new PointF(5,19),new PointF(2,17)},.25f);g.FillPath(Brushes.Wheat,path);g.DrawPath(outline,path);g.FillEllipse(Brushes.IndianRed,6,5,4,4);g.FillEllipse(Brushes.Goldenrod,12,4,4,4);g.FillEllipse(Brushes.SteelBlue,17,9,4,4);g.FillEllipse(Brushes.SeaGreen,15,15,4,4);g.FillEllipse(Brushes.White,6,10,4,4);g.DrawEllipse(outline,6,10,4,4);
  }return image;
 }
 public void TestNamesAndColourLayout(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);
  if(!Text.StartsWith("IDeaSKETCHER",StringComparison.Ordinal))throw new Exception("Product title case");
  if(toolButtons["Pen"].Text!="BIRO        N"&& !toolButtons["Pen"].Text.EndsWith("BIRO"))throw new Exception("Biro caption");
  if(!toolButtons["Paintbrush"].Text.EndsWith("BRUSH")||!toolButtons["Ink Pen"].Text.EndsWith("INK")||!fillBucketButton.Text.EndsWith("VISIBLE"))throw new Exception("Tool captions");
  if(vectorButtons.ContainsKey("Free polygon")||vectorMode.Items.Contains("Free polygon"))throw new Exception("Free polygon removed");
  if(vectorButtons["Curvature pen"].Text!="CURVE PEN")throw new Exception("Curve pen caption");
  if(layers.Font.Size!=layerLabel.Font.Size+1)throw new Exception("Layer font matches heading");
  if(Math.Abs(wheel.Left+wheel.Width/2-vectorColourPanel.ClientSize.Width/2)>SharpDpi.Px(this,1)||savedColoursButton.Top!=eyedropperButton.Top||savedColoursButton.Bottom>=wheel.Top||savedColoursButton.Image==null)throw new Exception("Colour controls layout");
  if(selectionButtons.Values.Any(b=>b.TextAlign!=ContentAlignment.MiddleCenter))throw new Exception("Selection label alignment");  SelectTool("Pencil");canvas.Focus();var message=new Message();ProcessCmdKey(ref message,Keys.A);if(canvas.Tool!="Airbrush"||toolButtons["Airbrush"].Text!="AIRBRUSH")throw new Exception("Airbrush A shortcut");
  if(toolButtons.Values.Concat(vectorButtons.Values).Any(b=>b.ImageAlign!=ContentAlignment.MiddleRight||b.TextAlign!=ContentAlignment.MiddleLeft))throw new Exception("Tool text/icon alignment");
  mode.SelectedItem="RGB";SyncColour();channels.PerformLayout();if(channels.Controls.Count!=3||channels.Controls.Cast<Control>().Any(c=>c.Top!=channels.Controls[0].Top)||channels.Controls.Cast<Control>().Any(c=>c.Right>channels.ClientSize.Width))throw new Exception("RGB single row");
  paletteTabs.SelectedTab=colourPage;SelectTool("Select");using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tidy-pixel-preview.png"));}
  AddTypedLayer(true);var ordered=vectorButtons.Values.OrderBy(b=>b.Parent.Parent.Controls.GetChildIndex(b.Parent)).Select(b=>b.Text).ToArray();if(string.Join(",",ordered)!="FREEHAND,LINE,POLYLINE,PEN,CURVE PEN,RECTANGLE,ELLIPSE,POLYGON,TEXT"||vectorPicker.Text!="SELECT")throw new Exception("Vector order/captions");
  using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"tidy-vector-preview.png"));}dirty=false;
 }
}
}
