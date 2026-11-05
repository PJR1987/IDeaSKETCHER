using System;using System.Drawing;using System.Linq;using System.Windows.Forms;
namespace IDSketcher{
partial class Studio{
 public void TestNarrowPaletteAndBoxHighlight(){
  previewing=true;Show();canvas.Dock=DockStyle.Fill;ClientSize=new Size(SharpDpi.Px(this,1280),SharpDpi.Px(this,900));SetDocument(new Document("A4",100),null);AddTypedLayer(false);AddTypedLayer(true);if(layers.ItemHeight!=SharpDpi.Px(this,40)||layers.Font.Size!=10)throw new Exception("Layer font/spacing");
  if(paletteHost.Width!=SharpDpi.Px(this,218))throw new Exception("Narrow palette width");LayoutNarrowPalette(true);int centre=vectorColourPanel.ClientSize.Width/2;
  if(Math.Abs(wheel.Left+wheel.Width/2-centre)>1||Math.Abs((eyedropperButton.Left+savedColoursButton.Right)/2-centre)>1||savedColoursButton.Bottom>=wheel.Top)throw new Exception("Centred wheel/icons");var brightness=vectorColourPanel.Controls.OfType<Label>().Single(c=>c.Text=="BRIGHTNESS");if(brightness.Bottom>=value.Top||brightness.TextAlign!=ContentAlignment.MiddleCenter)throw new Exception("Brightness above slider");
  selectionButtons["Box selection"].PerformClick();if(!canvas.VectorBoxMode||selectionButtons["Box selection"].BackColor!=Theme.Accent)throw new Exception("Box active highlight");vectorPicker.PerformClick();if(canvas.VectorBoxMode||selectionButtons["Box selection"].BackColor==Theme.Accent)throw new Exception("Select clears box highlight");selectionButtons["Box selection"].PerformClick();canvas.TestDrag(new Point(40,40),new Point(200,200));if(canvas.VectorBoxMode||selectionButtons["Box selection"].BackColor==Theme.Accent)throw new Exception("Box completes highlight");
  foreach(var b in vectorFillButtons)if(TextRenderer.MeasureText(b.Text,b.Font).Width>b.ClientSize.Width)throw new Exception("Fill/remove caption fits");
  paletteTabs.SelectedTab=colourPage;canvas.Fit();using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"narrow-vector-preview.png"));}paletteTabs.SelectedTab=layerPage;using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"spaced-layers-preview.png"));}
  canvas.Doc.Active=0;RefreshLayers();paletteTabs.SelectedTab=colourPage;LayoutNarrowPalette(false);if(Math.Abs(wheel.Left+wheel.Width/2-centre)>1||vectorFillButtons.Any(b=>b.Visible))throw new Exception("Pixel palette consistency");dirty=false;
 }
 bool layingOutPalette;int lastPaletteWidth=-1;
 void PaletteBounds(Control c,int x,int y,int w,int h){int left=Math.Max(0,(vectorColourPanel.ClientSize.Width-SharpDpi.Px(this,174))/2);SharpDpi.Bounds(c,x,y,w,h);c.Left+=left;}
 void LayoutNarrowPalette(bool vector){
  if(SharpDpi.IsScaling(this)||layingOutPalette||eyedropperButton==null||weightCaption==null)return;layingOutPalette=true;try{
   mode.Parent=null;PaletteBounds(eyedropperButton,51,0,32,32);PaletteBounds(savedColoursButton,91,0,32,32);PaletteBounds(wheel,7,44,160,160);
   int index=0;foreach(string name in new[]{"BLACK","GREY","WHITE"}){var b=vectorColourPanel.Controls.OfType<Button>().First(c=>c.Text==name);PaletteBounds(b,index++*60,214,54,28);}
   var brightness=vectorColourPanel.Controls.OfType<Label>().First(c=>c.Text=="BRIGHTNESS");brightness.TextAlign=ContentAlignment.MiddleCenter;PaletteBounds(brightness,0,252,174,22);PaletteBounds(value,0,278,174,28);
   PaletteBounds(channels,0,314,174,28);PaletteBounds(swatch,0,356,32,30);colourEntry.AutoSize=false;PaletteBounds(colourEntry,40,356,134,30);
   foreach(var b in vectorColourPanel.Controls.OfType<Button>().Where(c=>c.Text=="APPLY"||c.Text=="ADD"))PaletteBounds(b,b.Text=="APPLY"?0:90,396,84,32);
   weightCaption.Visible=false;for(int i=0;i<vectorFillButtons.Length;i++){var b=vectorFillButtons[i];b.Text=i==0?"FILL":"REMOVE";b.TextAlign=ContentAlignment.MiddleCenter;b.Padding=Padding.Empty;PaletteBounds(b,i==0?0:54,438,i==0?50:62,32);}
   LayoutStrokeValue();PaletteBounds(strokeValueBox,120,438,54,32);SharpDpi.Bounds(vectorWidth,1,5,50,22);vectorWidth.Top=(strokeValueBox.ClientSize.Height-vectorWidth.Height)/2;
   SyncRadiusControls();if(radiusSlider!=null){PaletteBounds(radiusLabel,0,480,174,22);PaletteBounds(radiusSlider,0,506,174,28);}PaletteBounds(colourEntryHint,0,544,174,26);vectorColourPanel.Height=SharpDpi.Px(this,vector?576:460);
  }finally{layingOutPalette=false;}
 }
 void ConfigureNarrowPalette(){foreach(var slider in new[]{vectorOpacitySlider,polygonSidesSlider})if(slider!=null&&slider.Tag is Label){var caption=(Label)slider.Tag;caption.Width=144;caption.Height=22;}paletteHost.Width=SharpDpi.Px(this,218);vectorColourPanel.SizeChanged+=delegate{if(lastPaletteWidth==vectorColourPanel.ClientSize.Width)return;lastPaletteWidth=vectorColourPanel.ClientSize.Width;LayoutNarrowPalette(fillRowShown);};AddRadiusControls(vectorColourPanel);canvas.VectorBoxStateChanged=UpdateSelectionButtons;LayoutNarrowPalette(fillRowShown);}
}
partial class Canvas{public Action VectorBoxStateChanged;void NotifyVectorBox(){if(VectorBoxStateChanged!=null)VectorBoxStateChanged();}}
}
