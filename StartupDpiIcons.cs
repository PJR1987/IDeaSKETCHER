using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace IDSketcher{
partial class Studio{
 public void TestStartupDpiIcons(){
  for(int run=0;run<2;run++)using(var fresh=new Studio()){fresh.previewing=true;fresh.Opacity=0;fresh.ShowInTaskbar=false;fresh.Show();fresh.CheckStartupDpiGeometry();for(int frame=0;frame<4;frame++){Application.DoEvents();fresh.CheckStartupDpiGeometry();}fresh.SetDocument(new Document("A4",100),null);fresh.CheckStartupDpiGeometry();float actual;using(var g=fresh.CreateGraphics())actual=g.DpiX/96f;
   foreach(float scale in new[]{1f,1.25f,1.5f,2f,2f,1.5f,actual}){SharpDpi.TestScale(fresh,scale);Application.DoEvents();fresh.CheckStartupDpiGeometry();}
   fresh.paletteTabs.SelectedTab=fresh.colourPage;using(var image=new Bitmap(fresh.Width,fresh.Height)){fresh.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-pixel-dpi-preview.png"));}fresh.AddTypedLayer(true);fresh.CheckStartupDpiGeometry();using(var image=new Bitmap(fresh.Width,fresh.Height)){fresh.DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"startup-vector-dpi-preview.png"));}
  }
 }
 void CheckStartupDpiGeometry(){
  if(wheel.Width!=SharpDpi.Px(this,160)||wheel.Height!=SharpDpi.Px(this,160)||wheel.Left<0||wheel.Right>vectorColourPanel.ClientSize.Width||Math.Abs(wheel.Left+wheel.Width/2-vectorColourPanel.ClientSize.Width/2)>1)throw new Exception("DPI wheel clipped or double-scaled");
  foreach(var b in toolButtons.Values.Concat(vectorButtons.Values).Concat(new[]{vectorPicker,pageFillButton,fillBucketButton})){int pixels=SharpDpi.Px(this,20);if(b.Image==null||b.Image.Width!=pixels||b.Image.Height!=pixels||pixels>b.Height)throw new Exception("DPI tool icon size");}
  int pixelsWheel=wheel.Width;using(var image=new Bitmap(pixelsWheel,pixelsWheel)){wheel.DrawToBitmap(image,new Rectangle(0,0,pixelsWheel,pixelsWheel));foreach(var point in new[]{new Point(pixelsWheel/2,8),new Point(pixelsWheel-9,pixelsWheel/2),new Point(pixelsWheel/2,pixelsWheel-9),new Point(8,pixelsWheel/2)})if(image.GetPixel(point.X,point.Y).ToArgb()==wheel.BackColor.ToArgb())throw new Exception("Colour wheel quadrant missing");}
 }
 int toolIconPixels;
 public void CompleteDpiLayout(){if(paletteHost==null||vectorColourPanel==null||eyedropperButton==null||SharpDpi.IsScaling(this))return;paletteHost.Width=SharpDpi.Px(this,218);lastPaletteWidth=-1;UpdateDisplayLayout();LayoutNarrowPalette(fillRowShown);SyncColour();ResizeToolIcons();canvas.Invalidate();}
 static void SetToolIcon(Button button,Bitmap image){var old=button.Image;button.Image=image;if(old!=null)old.Dispose();button.ImageAlign=ContentAlignment.MiddleRight;}
 void ResizeToolIcons(){int pixels=SharpDpi.Px(this,20);if(toolIconPixels==pixels)return;toolIconPixels=pixels;
  foreach(var pair in toolButtons){Bitmap icon;if(pair.Key=="Select")icon=PickerImage(pixels);else{icon=new Bitmap(pixels,pixels);using(var g=Graphics.FromImage(icon))ToolIcons.Draw(g,pair.Key,new Rectangle(0,0,pixels,pixels));}SetToolIcon(pair.Value,icon);}
  foreach(var pair in vectorButtons)SetToolIcon(pair.Value,pair.Key=="Text"?TextToolIcon(pixels):VectorIcon(pair.Key,pixels));SetToolIcon(vectorPicker,PickerImage(pixels));SetToolIcon(pageFillButton,ScaledFillIcon(pixels,false));SetToolIcon(fillBucketButton,ScaledFillIcon(pixels,true));
 }
 static Bitmap TextToolIcon(int size){var image=new Bitmap(size,size);using(var g=Graphics.FromImage(image))using(var path=new GraphicsPath())using(var family=new FontFamily("Segoe UI")){g.SmoothingMode=SmoothingMode.AntiAlias;path.AddString("T",family,(int)FontStyle.Bold,size*.9f,new PointF(size*.12f,-size*.07f),StringFormat.GenericTypographic);g.FillPath(Brushes.Teal,path);}return image;}
 static Bitmap ScaledFillIcon(int size,bool visible){var image=new Bitmap(size,size);using(var g=Graphics.FromImage(image))using(var pen=new Pen(Color.SlateGray,1.2f)){g.ScaleTransform(size/24f,size/24f);g.SmoothingMode=SmoothingMode.AntiAlias;if(!visible){g.FillRectangle(Brushes.Teal,4,4,16,16);g.DrawRectangle(pen,3,3,18,18);}else{Point[] points={new Point(10,3),new Point(21,14),new Point(12,21),new Point(2,11)};g.FillPolygon(Brushes.LightSteelBlue,points);g.DrawPolygon(pen,points);g.DrawArc(pen,7,1,9,9,180,180);g.FillEllipse(Brushes.Teal,19,17,4,6);}}return image;}
}
}
