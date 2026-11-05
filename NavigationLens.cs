using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Drawing.Imaging;using System.Linq;using System.Windows.Forms;
namespace IDSketcher{
partial class Studio{
 public void TestNavigationAndLens(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Focus();var message=new Message();SelectTool("Pencil");ProcessCmdKey(ref message,Keys.V);if(canvas.Tool!="Select")throw new Exception("Pixel V");
  AddTypedLayer(true);foreach(Keys key in new[]{Keys.F,Keys.L,Keys.O,Keys.P,Keys.C,Keys.R,Keys.E,Keys.G}){canvas.Focus();ProcessCmdKey(ref message,key);if(canvas.VectorMode!=VectorShortcut(key)||polygonSides.Enabled!=(key==Keys.G))throw new Exception("Vector shortcut or polygon sides");}
  canvas.ShowGrid=false;ProcessCmdKey(ref message,Keys.Alt|Keys.G);if(!canvas.ShowGrid||canvas.VectorMode!="Polygon")throw new Exception("Alt G grid");canvas.ShowGrid=false;
  foreach(var b in toolButtons.Values.Concat(vectorButtons.Values)){var label=b.Parent.Controls.OfType<Label>().Single();if(label.Text.Length!=1||label.TabStop||b.Left<=label.Right||b.Text.Contains(" ")&&b.Text.StartsWith(label.Text+" "))throw new Exception("Separate shortcut boxes");}
  if(vectorFillButtons.Any(b=>b.Top<channels.Bottom)||strokeValueBox.Top<channels.Bottom)throw new Exception("Vector style below RGB");
  canvas.Doc.Active=0;RefreshLayers();canvas.TestSpacePan();
  byte[] source;using(var bin=new System.IO.MemoryStream())using(var w=new System.IO.BinaryWriter(bin)){foreach(float v in new[]{-.8f,-.7f,-.3f,.9f,-.5f,.4f,0f,.9f,0f})w.Write(v);byte[] bytes=bin.ToArray();string json="{\"asset\":{\"version\":\"2.0\"},\"buffers\":[{\"byteLength\":36}],\"bufferViews\":[{\"buffer\":0,\"byteLength\":36}],\"accessors\":[{\"bufferView\":0,\"componentType\":5126,\"count\":3,\"type\":\"VEC3\"}],\"materials\":[{\"doubleSided\":true}],\"meshes\":[{\"primitives\":[{\"attributes\":{\"POSITION\":0},\"material\":0}]}],\"nodes\":[{\"mesh\":0}],\"scenes\":[{\"nodes\":[0]}],\"scene\":0}";while(json.Length%4!=0)json+=" ";using(var file=new System.IO.MemoryStream())using(var writer=new System.IO.BinaryWriter(file)){var text=System.Text.Encoding.UTF8.GetBytes(json);writer.Write(0x46546c67);writer.Write(2);writer.Write(28+text.Length+bytes.Length);writer.Write(text.Length);writer.Write(0x4e4f534a);writer.Write(text);writer.Write(bytes.Length);writer.Write(0x004e4942);writer.Write(bytes);source=file.ToArray();}}
  AddThreeDModel(GlbModel.Load(source),"LENS TEST");var layer=canvas.Doc.Selected;int history=undo.Count;threeDLens.Value=30;if(layer.Scene.Lens!=30||undo.Count!=history+1)throw new Exception("Lens slider");Travel(undo,redo);if(layer.Scene.Lens!=50)throw new Exception("Lens undo");Travel(redo,undo);if(layer.Scene.Lens!=30)throw new Exception("Lens redo");
  byte[] wide;layer.Scene.X=150;layer.Scene.Y=150;using(var image=layer.Scene.Render(300,300)){wide=Document.Png(image);bool edge=false;for(int y=0;y<image.Height&&!edge;y++)for(int x=0;x<image.Width;x++){int a=image.GetPixel(x,y).A;if(a>0&&a<255){edge=true;break;}}if(!edge)throw new Exception("3D antialiasing");}canvas.SetThreeDLens(100);using(var image=layer.Scene.Render(300,300))if(wide.SequenceEqual(Document.Png(image)))throw new Exception("Lens projection");
  string path=System.IO.Path.Combine(System.IO.Path.GetTempPath(),Guid.NewGuid()+".idsk");try{canvas.Doc.Save(path);using(var loaded=Document.Load(path))if(loaded.Selected.Scene.Lens!=100)throw new Exception("Lens persistence");}finally{System.IO.File.Delete(path);}
  using(var stream=new System.IO.MemoryStream()){using(var writer=new System.IO.BinaryWriter(stream,System.Text.Encoding.UTF8,true))layer.Scene.Write(writer);stream.SetLength(stream.Length-4);stream.Position=0;using(var reader=new System.IO.BinaryReader(stream))if(ThreeDView.Read(reader,false).Lens!=50)throw new Exception("Legacy lens default");}
  canvas.ResetThreeD();canvas.Fit();toolPanel.AutoScrollPosition=Point.Empty;using(var image=new Bitmap(Width,Height)){DrawToBitmap(image,new Rectangle(Point.Empty,image.Size));image.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"lens-preview.png"));}dirty=false;
 }
 static string VectorShortcut(Keys key){switch(key){case Keys.F:return "Freehand";case Keys.L:return "Line";case Keys.O:return "Polyline";case Keys.P:return "Vector pen";case Keys.C:return "Curvature pen";case Keys.R:return "Rectangle";case Keys.E:return "Ellipse";case Keys.G:return "Polygon";case Keys.T:return "Text";default:return null;}}
 void AddShortcutBox(Button b,string shortcut,string title){
  var parent=(FlowLayoutPanel)b.Parent;int at=parent.Controls.GetChildIndex(b);bool vector=vectorControls.Remove(b),pixel=pixelControls.Remove(b);
  var row=new Panel{Width=144,Height=34,Margin=new Padding(0,3,0,3)};
  var key=new Label{Text=shortcut,BorderStyle=BorderStyle.FixedSingle,TextAlign=ContentAlignment.MiddleCenter,Bounds=new Rectangle(0,0,22,34),TabStop=false,AccessibleName="Shortcut "+shortcut};
  parent.Controls.Remove(b);b.Text=title;b.SetBounds(26,0,118,34);b.Margin=Padding.Empty;b.Padding=new Padding(2,0,1,0);row.Controls.Add(key);row.Controls.Add(b);b.Visible=true;parent.Controls.Add(row);parent.Controls.SetChildIndex(row,at);
  if(vector)vectorControls.Add(row);if(pixel)pixelControls.Add(row);Theme.Apply(row);
 }
}
partial class Canvas{
 public void TestSpacePan(){
  Zoom=1;Offset=new PointF(0,0);using(var g=Graphics.FromImage(Doc.Selected.Image))g.FillRectangle(Brushes.Red,100,100,80,80);Doc.Selected.InvalidateVectors();BeginSpaceNavigation();if(!space||!rotationHeld)throw new Exception("Space navigation");OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,10,10,0));if(!panning||rotatingView||navigationImage==null)throw new Exception("Space pan starts outside ring");
  var watch=System.Diagnostics.Stopwatch.StartNew();for(int i=0;i<40;i++){OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,10+i,10,0));TestDraftPaint();}watch.Stop();if(Offset.X!=39)throw new Exception("Pan offset");OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,49,10,0));if(panning||quickNavigation)throw new Exception("Pan release");OnKeyUp(new KeyEventArgs(Keys.Space));if(space||rotationHeld)throw new Exception("Space release");System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"pan-timing.txt"),"40 cached pan frames: "+watch.ElapsedMilliseconds+" ms");
 }
 public void BeginSpaceNavigation(){if(space)return;StartViewRotation();space=true;RefreshCursor();}
 void PreparePanImage(){BeginQuickNavigation();}
 public void SetThreeDLens(float lens){if(Doc==null||!Doc.Selected.Is3D||Doc.Selected.IsLocked)return;lens=Math.Max(30,Math.Min(100,lens));if(lens==Doc.Selected.Scene.Lens)return;if(BeforeStroke!=null)BeforeStroke();Doc.Selected.Scene.Lens=lens;Doc.Selected.InvalidateVectors();if(AfterStroke!=null)AfterStroke();if(ThreeDChanged!=null)ThreeDChanged();Invalidate();}
}
}
