using System;using System.Drawing;using System.Drawing.Imaging;using System.Collections.Generic;using System.Runtime.InteropServices;using System.Windows.Forms;using System.Linq;
namespace IDSketcher{
partial class Canvas{
 static int[] ReadFillPixels(Bitmap image){int w=image.Width,h=image.Height;var pixels=new int[w*h];var data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);try{for(int y=0;y<h;y++)Marshal.Copy(IntPtr.Add(data.Scan0,y*data.Stride),pixels,y*w,w);}finally{image.UnlockBits(data);}return pixels;}
 public void FillVisibleAt(PointF p){
  if(Doc==null||!Doc.Selected.IsRaster||Doc.Selected.IsLocked||!Doc.Selected.Visible)return;
  int w=Doc.Width,h=Doc.Height,x=(int)Math.Floor(p.X),y=(int)Math.Floor(p.Y);if(x<0||y<0||x>=w||y>=h)return;int seed=y*w+x;if(SelectionMask!=null&&SelectionMask[seed]==0)return;
  int[] visible;using(var composite=Doc.Flatten())visible=ReadFillPixels(composite);
  int target=visible[seed],colour=Color.FromArgb(Ink.R,Ink.G,Ink.B).ToArgb();var pixels=ReadFillPixels(Doc.Selected.Image);var visited=new byte[w*h];
  Func<int,bool> matches=delegate(int i){if(visited[i]!=0||SelectionMask!=null&&SelectionMask[i]==0)return false;int value=visible[i];return Math.Abs(((value>>24)&255)-((target>>24)&255))<=12&&Math.Abs(((value>>16)&255)-((target>>16)&255))<=12&&Math.Abs(((value>>8)&255)-((target>>8)&255))<=12&&Math.Abs((value&255)-(target&255))<=12;};
  var stack=new Stack<int>();stack.Push(seed);bool changed=false;
  while(stack.Count>0){int at=stack.Pop();if(!matches(at))continue;int row=at/w,left=at%w,right=left;while(left>0&&matches(row*w+left-1))left--;while(right+1<w&&matches(row*w+right+1))right++;
   for(int xx=left;xx<=right;xx++){int i=row*w+xx;visited[i]=1;if(pixels[i]!=colour){pixels[i]=colour;changed=true;}}
   for(int side=-1;side<=1;side+=2){int yy=row+side;if(yy<0||yy>=h)continue;bool run=false;for(int xx=left;xx<=right;xx++){bool hit=matches(yy*w+xx);if(hit&&!run)stack.Push(yy*w+xx);run=hit;}}
  }
  if(!changed)return;if(BeforeStroke!=null)BeforeStroke();var image=Doc.Selected.Image;var data=image.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);try{for(int row=0;row<h;row++)Marshal.Copy(pixels,row*w,IntPtr.Add(data.Scan0,row*data.Stride),w);}finally{image.UnlockBits(data);}if(AfterStroke!=null)AfterStroke();Invalidate();
 }
}
partial class Studio{
 public void TestVisibleFill(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;
  int brushIndex=toolPanel.Controls.GetChildIndex(toolButtons["Paintbrush"].Parent);if(toolPanel.Controls.GetChildIndex(pageFillButton.Parent)!=brushIndex+1||toolPanel.Controls.GetChildIndex(fillBucketButton.Parent)!=brushIndex+2)throw new Exception("Fill button order");var paint=canvas.Doc.Selected;var boundary=new Layer("Visible outlines",canvas.Doc.Width,canvas.Doc.Height){IsVector=true};boundary.Vectors.Add(new VectorShape{Kind="Polygon",Colour=Color.Black,Width=6,Points=new List<PointF>{new PointF(40,40),new PointF(200,40),new PointF(200,200),new PointF(40,200)}});
  using(var g=Graphics.FromImage(boundary.Image))using(var pen=new Pen(Color.Black,6))g.DrawLine(pen,120,40,120,200);
  canvas.Doc.Layers.Add(boundary);canvas.Ink=Color.Red;canvas.Focus();var msg=new Message();ProcessCmdKey(ref msg,Keys.K);if(!canvas.FillTool)throw new Exception("K tool");
  int history=undo.Count;canvas.TestDrag(new Point(80,80),new Point(80,80));
  if(undo.Count!=history+1||paint.Image.GetPixel(80,80).ToArgb()!=Color.Red.ToArgb()||paint.Image.GetPixel(160,80).A!=0||paint.Image.GetPixel(20,20).A!=0)throw new Exception("Visible pixel/vector boundaries");
  Travel(undo,redo);if(paint.Image.GetPixel(80,80).A!=0)throw new Exception("Visible fill undo");Travel(redo,undo);
  canvas.TestBoxOperation(new Point(140,60),new Point(180,100),0);canvas.Ink=Color.Blue;canvas.FillVisibleAt(new PointF(160,80));if(paint.Image.GetPixel(160,80).ToArgb()!=Color.Blue.ToArgb()||paint.Image.GetPixel(160,120).A!=0)throw new Exception("Visible fill selection");
  canvas.ClearSelection();paint.Locked=true;history=undo.Count;canvas.Ink=Color.Green;canvas.FillVisibleAt(new PointF(160,80));if(undo.Count!=history)throw new Exception("Locked fill");
  paint.Locked=false;boundary.Visible=false;canvas.FillVisibleAt(new PointF(20,20));if(paint.Image.GetPixel(160,120).ToArgb()!=Color.Green.ToArgb())throw new Exception("Hidden outlines ignored");
  SelectTool("Pencil");if(canvas.FillTool)throw new Exception("Exit K tool");dirty=false;
 }
}
}
