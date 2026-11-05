using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;using System.IO;
namespace IDSketcher{
partial class Stroke{
 internal static bool FastSoftEnabled=true;
 unsafe void DabFastSoft(PointF p,Rectangle rect,float radius){
  PreparePaintbrushKernel(radius);dirtyBounds=dirtyBounds.IsEmpty?rect:Rectangle.Union(dirtyBounds,rect);touchedBounds=touchedBounds.IsEmpty?rect:Rectangle.Union(touchedBounds,rect);
  bool owns=batch==null;if(owns)BeginBatch();try{
   double outer=(radius+.5)*(radius+.5);int f=(int)Math.Round(flow*65536);bool air=tool=="Airbrush";
   for(int y=rect.Top;y<rect.Bottom;y++){
    double dy=y+.5-p.Y,dy2=dy*dy;if(dy2>=outer)continue;double span=Math.Sqrt(outer-dy2);int left=Math.Max(rect.Left,(int)Math.Ceiling(p.X-.5-span)),right=Math.Min(rect.Right-1,(int)Math.Floor(p.X-.5+span));
    byte* row=(byte*)batch.Scan0+y*batch.Stride;
    for(int x=left;x<=right;x++){
     int idx=y*width+x;if(mask[idx]==255||selection!=null&&selection[idx]==0)continue;double dx=x+.5-p.X,coverage=PaintbrushCoverage(dx*dx+dy2,radius);int target=(int)Math.Round(coverage*65535);if(target==0)continue;
     int next=target;if(deposits!=null){int previous=deposits[idx];if(air)next=previous+(int)(((long)(65535-previous)*target*f+2147483648L)>>32);else next=previous+(int)(((long)Math.Max(0,target-previous)*f+32768)>>16);if(next>65535)next=65535;deposits[idx]=(ushort)next;}
     int amount=(next+128)/257;if(amount<=mask[idx])continue;mask[idx]=(byte)amount;int dest=x*4,originalIndex=idx*4;byte old=original[originalIndex+3];
     if(old==0){row[dest]=color.B;row[dest+1]=color.G;row[dest+2]=color.R;row[dest+3]=paintbrushAlpha[amount];}
     else{double a=amount/255.0*opacity,oldA=old/255.0,outA=a+oldA*(1-a);if(outA<=0)continue;row[dest]=(byte)Math.Round((color.B*a+original[originalIndex]*oldA*(1-a))/outA);row[dest+1]=(byte)Math.Round((color.G*a+original[originalIndex+1]*oldA*(1-a))/outA);row[dest+2]=(byte)Math.Round((color.R*a+original[originalIndex+2]*oldA*(1-a))/outA);row[dest+3]=(byte)Math.Round(outA*255);}
    }
   }
  }finally{if(owns)EndBatch();}
 }
}
partial class LayerList{
 int dragRow=-1,dropRow=-1;Point dragStart;bool draggingLayers;public Action<int,int> Reordered;
 protected override void OnMouseMove(MouseEventArgs e){
  base.OnMouseMove(e);if(dragRow<0||(e.Button&MouseButtons.Left)==0)return;
  if(!draggingLayers){var threshold=new Rectangle(dragStart.X-SystemInformation.DragSize.Width/2,dragStart.Y-SystemInformation.DragSize.Height/2,SystemInformation.DragSize.Width,SystemInformation.DragSize.Height);if(threshold.Contains(e.Location))return;draggingLayers=true;}
  if(e.Y<12&&TopIndex>0)TopIndex--;else if(e.Y>ClientSize.Height-12&&TopIndex<Items.Count-1)TopIndex++;
  dropRow=IndexFromPoint(new Point(60,Math.Max(0,Math.Min(ClientSize.Height-1,e.Y))));if(dropRow<0)dropRow=Items.Count-1;Invalidate();
 }
 protected override void OnMouseUp(MouseEventArgs e){
  int from=dragRow,to=dropRow;bool move=draggingLayers&&e.Button==MouseButtons.Left;dragRow=dropRow=-1;draggingLayers=false;Capture=false;base.OnMouseUp(e);if(move&&from>=0&&to>=0&&from!=to&&Reordered!=null)Reordered(from,to);Invalidate();
 }
 protected override void OnMouseCaptureChanged(EventArgs e){if(!Capture){dragRow=dropRow=-1;draggingLayers=false;Invalidate();}base.OnMouseCaptureChanged(e);}
 public void TestDragLayer(int from,int to){var a=GetItemRectangle(from);var b=GetItemRectangle(to);OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,80,a.Top+a.Height/2,0));OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,80,b.Top+b.Height/2,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,80,b.Top+b.Height/2,0));}
}
partial class Studio{
 void ReorderLayer(int from,int to){
  var doc=canvas.Doc;if(doc==null||from<0||to<0||from>=layers.Items.Count||to>=layers.Items.Count)return;var layer=layers.Items[from] as Layer;var target=layers.Items[to] as Layer;if(layer==null||target==null||layer.IsLocked||target.PermanentlyLocked)return;
  canvas.EndStroke();int oldIndex=doc.Layers.IndexOf(layer),newIndex=doc.Layers.IndexOf(target);if(oldIndex<0||newIndex<0||oldIndex==newIndex)return;doc.Layers.RemoveAt(oldIndex);doc.Layers.Insert(newIndex,layer);doc.Active=newIndex;Changed();RefreshLayers();
 }
 public void TestLayerDragAndLargeBrushes(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);AddTypedLayer(false);var a=canvas.Doc.Selected;AddTypedLayer(true);var b=canvas.Doc.Selected;if(compactPalette)paletteTabs.SelectedTab=layerPage;PerformLayout();layerPage.PerformLayout();layers.Parent.PerformLayout();System.IO.File.WriteAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"drag-debug.txt"),"compact="+compactPalette+" visible="+layers.Visible+" bounds="+layers.Bounds+" row0="+layers.GetItemRectangle(0)+" row1="+layers.GetItemRectangle(1)+" parent="+layers.Parent.Bounds);layers.TestDragLayer(0,1);if(canvas.Doc.Layers.IndexOf(b)!=1||canvas.Doc.Selected!=b)throw new Exception("Layer drag down");layers.TestDragLayer(1,0);if(canvas.Doc.Layers.Last()!=b)throw new Exception("Layer drag up");b.Locked=true;layers.TestDragLayer(0,1);if(canvas.Doc.Layers.Last()!=b)throw new Exception("Locked layer drag");b.Locked=false;
  var results=new System.Collections.Generic.List<string>();
  foreach(string tool in new[]{"Paintbrush","Airbrush"}){int size=tool=="Airbrush"?500:150;Bitmap baseline=null;try{
   foreach(bool fast in new[]{false,true})using(var image=new Bitmap(900,700)){Stroke.FastSoftEnabled=fast;var watch=System.Diagnostics.Stopwatch.StartNew();var stroke=new Stroke(image,tool,Color.Teal,size,.75f,new PointF(250,350),null,.35f,.6f);for(int i=1;i<=500;i++)stroke.Move(new PointF(250+i%400,350+(float)Math.Sin(i*.02)*75));watch.Stop();results.Add(tool+" "+size+"px "+(fast?"optimised":"original")+": "+watch.ElapsedMilliseconds+" ms");
    if(!fast)baseline=(Bitmap)image.Clone();else for(int y=0;y<image.Height;y+=3)for(int x=0;x<image.Width;x+=3){var old=baseline.GetPixel(x,y);var now=image.GetPixel(x,y);if(Math.Abs(old.A-now.A)>1)throw new Exception("Soft brush coverage changed");}
   }
  }finally{Stroke.FastSoftEnabled=true;if(baseline!=null)baseline.Dispose();}}
  File.WriteAllLines(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"large-brush-timing.txt"),results.ToArray());dirty=false;
 }
}
}
