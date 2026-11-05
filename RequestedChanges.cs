using System;using System.Drawing;using System.Drawing.Imaging;using System.Runtime.InteropServices;using System.Windows.Forms;using System.IO;using System.Linq;
namespace IDSketcher {
partial class Studio {
 ToolStripDropDown colourPalette;
 void ShowColourPalette(){
  if(colourPalette!=null){colourPalette.Close();colourPalette.Dispose();colourPalette=null;}
  var grid=new TableLayoutPanel{Width=268,Height=204,Padding=new Padding(6),BackColor=Theme.Panel,ColumnCount=8,RowCount=6,Margin=Padding.Empty};
  for(int i=0;i<8;i++)grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,32));
  for(int i=0;i<6;i++)grid.RowStyles.Add(new RowStyle(SizeType.Absolute,32));
  var popup=new ToolStripDropDown{Padding=Padding.Empty,AutoClose=true};
  colourPalette=popup;
  var colours=preferences.Palette??new System.Collections.Generic.List<int>();
  for(int i=0;i<48;i++){bool occupied=i<colours.Count;Color chosen=occupied?Color.FromArgb(colours[i]):Theme.Panel;var button=new Button{Dock=DockStyle.Fill,Margin=new Padding(2),BackColor=chosen,FlatStyle=FlatStyle.Flat,AccessibleName=occupied?"Saved colour "+(i+1):"Empty colour slot",UseVisualStyleBackColor=false,Enabled=occupied};
   if(occupied)button.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Right){var menu=SavedColourMenu(chosen.ToArgb());menu.Show(button,e.Location);}};if(occupied)button.Click+=delegate{canvas.EndStroke();canvas.Ink=chosen;wheel.SetColor(chosen);SyncColour();canvas.ApplyVectorColour();popup.Close();canvas.Focus();};grid.Controls.Add(button,i%8,i/8);}
  popup.Items.Add(new ToolStripControlHost(grid){Margin=Padding.Empty,Padding=Padding.Empty,AutoSize=false,Size=grid.Size});
  popup.Closed+=delegate{BeginInvoke((Action)delegate{if(colourPalette==popup)colourPalette=null;popup.Dispose();});};
  popup.Show(wheel.PointToScreen(new Point(0,wheel.Height)));
 }
 public void TestPencilAndPalette(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;SelectTool("Pencil");
  canvas.TestDrag(new Point(40,80),new Point(240,80));
  int count=0;for(int y=65;y<95;y++)for(int x=40;x<240;x++)if(canvas.Doc.Selected.Image.GetPixel(x,y).A>0)count++;
  if(count==0)throw new Exception("Default pixel pencil produces no ink");
  var paletteBefore=preferences.Palette;preferences.Palette=new System.Collections.Generic.List<int>{Color.Red.ToArgb(),Color.Tan.ToArgb()};ShowColourPalette();var grid=(TableLayoutPanel)((ToolStripControlHost)colourPalette.Items[0]).Control;
  if(grid.Controls.Count!=48||grid.ColumnCount!=8||grid.RowCount!=6)throw new Exception("Palette must be 8 by 6");
  var buttons=grid.Controls.OfType<Button>().ToArray();buttons[0].PerformClick();Application.DoEvents();if(canvas.Ink.ToArgb()!=Color.Red.ToArgb())throw new Exception("Palette red");
  for(int i=0;i<2;i++){ShowColourPalette();grid=(TableLayoutPanel)((ToolStripControlHost)colourPalette.Items[0]).Control;grid.Controls.OfType<Button>().ElementAt(i).PerformClick();Application.DoEvents();}
  preferences.Palette=paletteBefore;canvas.TestDrag(new Point(40,120),new Point(240,120));if(!Enumerable.Range(40,200).Any(x=>canvas.Doc.Selected.Image.GetPixel(x,120).A>0))throw new Exception("Pencil after palette");dirty=false;
 } public void TestRequestedDefaults(){if(canvas.Ink.ToArgb()!=Color.FromArgb(51,51,51).ToArgb())throw new Exception("Default 80 percent grey");SelectTool("Eraser");if(size.Value!=25)throw new Exception("Default eraser 25");if(canvas.VectorTaper!=30||vectorTaper.Value!=30)throw new Exception("Default vector taper 30");Show();}public void TestRequestedChanges(){
  SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;canvas.Ink=Color.Orange;canvas.Focus();var msg=new Message();int history=undo.Count;pageFillButton.PerformClick();
  if(canvas.FillTool||undo.Count!=history+1||canvas.Doc.Selected.Image.GetPixel(10,10).ToArgb()!=Color.Orange.ToArgb()||canvas.Doc.Selected.Image.GetPixel(canvas.Doc.Width-1,canvas.Doc.Height-1).ToArgb()!=Color.Orange.ToArgb())throw new Exception("F fills full layer");
  Travel(undo,redo);if(canvas.Doc.Selected.Image.GetPixel(10,10).A!=0)throw new Exception("Fill undo");Travel(redo,undo);
  canvas.TestBoxOperation(new Point(20,20),new Point(80,80),0);canvas.Ink=Color.Blue;canvas.Focus();ProcessCmdKey(ref msg,Keys.F);
  if(canvas.Doc.Selected.Image.GetPixel(30,30).ToArgb()!=Color.Blue.ToArgb()||canvas.Doc.Selected.Image.GetPixel(100,100).ToArgb()!=Color.Orange.ToArgb())throw new Exception("Selection fill");
  history=undo.Count;canvas.FillSelectionColour();if(undo.Count!=history)throw new Exception("No-op fill");canvas.ClearSelection();canvas.Doc.Selected.Locked=true;canvas.Ink=Color.Red;canvas.FillSelectionColour();if(undo.Count!=history)throw new Exception("Locked fill");canvas.Doc.Selected.Locked=false;
  SelectTool("Airbrush");if(toolButtons["Airbrush"].Parent.Parent.Controls.GetChildIndex(toolButtons["Airbrush"].Parent)!=toolButtons["Marker"].Parent.Parent.Controls.GetChildIndex(toolButtons["Marker"].Parent)+1)throw new Exception("Airbrush order");
  using(var b=new Bitmap(120,120)){var brush=new Stroke(b,"Airbrush",Color.Blue,80,1,new PointF(60,60));int centre=b.GetPixel(60,60).A,edge=b.GetPixel(95,60).A;brush.Move(new PointF(80,60));brush.Move(new PointF(60,60));if(centre<=edge||centre>=100||b.GetPixel(60,60).A<=centre)throw new Exception("Soft airbrush buildup");}
  AddTypedLayer(true);vectorMode.SelectedItem="Polyline";canvas.TestDrag(new Point(20,20),new Point(20,20));canvas.TestDrag(new Point(80,80),new Point(80,80));canvas.TestDrag(new Point(140,20),new Point(140,20));canvas.Focus();ProcessCmdKey(ref msg,Keys.Enter);var shape=canvas.Doc.Selected.Vectors.Last();using(var path=shape.Path())if(shape.Kind!="Polyline"||shape.Points.Count!=3||path.PointCount!=3||path.PathTypes.Any(t=>(t&7)==3))throw new Exception("Sharp polyline");
  string file=Path.Combine(Path.GetTempPath(),Guid.NewGuid()+".idsk");try{canvas.Doc.Save(file);using(var loaded=Document.Load(file))if(loaded.Selected.Vectors.Last().Kind!="Polyline")throw new Exception("Polyline persistence");}finally{if(File.Exists(file))File.Delete(file);}dirty=false;
 }
}
partial class Canvas {
 public void FillSelectionColour(){
  EndStroke();if(Doc==null||!Doc.Selected.IsRaster||Doc.Selected.IsLocked||!Doc.Selected.Visible)return;
  int w=Doc.Width,h=Doc.Height,colour=Color.FromArgb(Ink.R,Ink.G,Ink.B).ToArgb();var image=Doc.Selected.Image;int[] pixels=new int[w*h];
  var data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
  try{for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*w,w);}finally{image.UnlockBits(data);}
  bool changed=false;for(int i=0;i<pixels.Length;i++)if((SelectionMask==null||SelectionMask[i]!=0)&&pixels[i]!=colour){pixels[i]=colour;changed=true;}
  if(!changed)return;if(BeforeStroke!=null)BeforeStroke();
  data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
  try{for(int y=0;y<h;y++)Marshal.Copy(pixels,y*w,IntPtr.Add(data.Scan0,y*data.Stride),w);}finally{image.UnlockBits(data);}
  if(AfterStroke!=null)AfterStroke();Invalidate();
 }
}
}
