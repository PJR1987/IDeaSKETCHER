using System;using System.Drawing;using System.Windows.Forms;using System.Linq;
namespace IDSketcher{
partial class Studio{
 Bitmap CursorTextIcon(int index){var image=new Bitmap(24,24);using(var g=Graphics.FromImage(image))using(var pen=new System.Drawing.Pen(Color.Teal,2)){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;if(index==0)g.DrawEllipse(pen,5,5,14,14);else{g.DrawLine(pen,5,12,19,12);g.DrawLine(pen,12,5,12,19);}}return image;}
 ContextMenuStrip SavedColourMenu(int value){
  var menu=new UppercaseContextMenu{Font=new Font("Segoe UI",9,FontStyle.Bold)};
  menu.Items.Add("REMOVE",null,delegate{if(preferences.Palette==null)return;int index=preferences.Palette.IndexOf(value);if(index<0)return;preferences.Palette.RemoveAt(index);if(!SaveTools())preferences.Palette.Insert(index,value);if(colourPalette!=null)colourPalette.Close();});
  menu.Closed+=delegate{BeginInvoke((Action)delegate{menu.Dispose();});};return menu;
 }
 public void TestLayerMenuPaletteRotation(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.TestRotationDoubleTap();
  var previous=preferences.Palette;preferences.Palette=new System.Collections.Generic.List<int>{Color.Red.ToArgb(),Color.Blue.ToArgb()};
  using(var menu=SavedColourMenu(Color.Red.ToArgb()))menu.Items[0].PerformClick();
  if(preferences.Palette.Count!=1||preferences.Palette[0]!=Color.Blue.ToArgb())throw new Exception("Remove saved colour");preferences.Palette=previous;
  using(var menu=BuildLayerMenu()){
   if(string.Join(",",menu.Items.Cast<ToolStripItem>().Select(i=>i.Text).ToArray())!="ADD,DELETE,CLEAR,DUPLICATE")throw new Exception("Layer actions");
   var add=(ToolStripMenuItem)menu.Items[0];if(add.DropDownDirection!=ToolStripDropDownDirection.Left||string.Join(",",add.DropDownItems.Cast<ToolStripItem>().Select(i=>i.Text).ToArray())!="PIXEL LAYER,VECTOR LAYER,GLB LAYER")throw new Exception("Left layer submenu");
   int count=canvas.Doc.Layers.Count;add.DropDownItems[1].PerformClick();if(canvas.Doc.Layers.Count!=count+1||!canvas.Doc.Selected.IsVector)throw new Exception("Add vector layer menu");
  }
  if(layerOpacity.Parent.Dock!=DockStyle.Top||layerOpacity.Parent.Controls.OfType<Label>().Any()||!layerLabel.Text.StartsWith("LAYER OPACITY - "))throw new Exception("Layer opacity position/label");
  if(colourEntryHint.Text=="HEX: #RRGGBB")throw new Exception("HEX hint removed");dirty=false;
 }
}
partial class Canvas{
 public void TestRotationDoubleTap(){lastRotationTap=0;rotationMoved=false;ViewAngle=48;StartViewRotation();StopViewRotation();StartViewRotation();if(ViewAngle!=0)throw new Exception("Double R resets view");StopViewRotation();lastRotationTap=0;}
}
}