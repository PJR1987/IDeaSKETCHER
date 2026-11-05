using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace IDSketcher {
class CentredLayerMenuRenderer:ToolStripProfessionalRenderer{
 protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e){var r=e.Item.ContentRectangle;r.X=e.TextRectangle.X;r.Width=e.TextRectangle.Width;e.TextRectangle=r;e.TextFormat=TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.SingleLine;base.OnRenderItemText(e);}
}
class LayerMenu : ContextMenuStrip {
    readonly Font menuFont=new Font("Segoe UI",12,FontStyle.Bold);
    public LayerMenu(){Font=menuFont;ShowImageMargin=false;Renderer=new CentredLayerMenuRenderer();}
    public bool TestKey(Keys Key){return ProcessDialogKey(Key);}
    protected override void Dispose(bool disposing){base.Dispose(disposing);if(disposing)menuFont.Dispose();}
}
partial class LayerList : ListBox {
    bool iconMouseDown;
    // Handle icons before the Native ListBox changes selection. Switching Layer
    // tool panels can Move Focus during Native selection and swallow MouseDown.
    protected override void WndProc(ref Message message){if(message.Msg==0x202&&dragRow>=0){int ux=(short)(message.LParam.ToInt64()&0xffff),uy=(short)((message.LParam.ToInt64()>>16)&0xffff);OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,ux,uy,0));message.Result=IntPtr.Zero;return;}if(message.Msg==0x201){int dx=(short)(message.LParam.ToInt64()&0xffff),dy=(short)((message.LParam.ToInt64()>>16)&0xffff),row=IndexFromPoint(new Point(dx,dy));if(dx>=56&&row>=0){SelectedIndex=row;Focus();dragRow=row;dragStart=new Point(dx,dy);Capture=true;message.Result=IntPtr.Zero;return;}}if(message.Msg==0x200&&dragRow>=0&&(message.WParam.ToInt64()&1)!=0){int mx=(short)(message.LParam.ToInt64()&0xffff),my=(short)((message.LParam.ToInt64()>>16)&0xffff);OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,mx,my,0));message.Result=IntPtr.Zero;return;}if(message.Msg==0x201||message.Msg==0x203){int x=(short)(message.LParam.ToInt64()&0xffff),y=(short)((message.LParam.ToInt64()>>16)&0xffff);int index=IndexFromPoint(new Point(x,y));if(index>=0&&x>=0&&x<56){iconMouseDown=true;if(x<32){if(VisibilityClicked!=null)VisibilityClicked(index);}else if(LockClicked!=null)LockClicked(index);message.Result=IntPtr.Zero;return;}}if(message.Msg==0x202&&iconMouseDown){iconMouseDown=false;message.Result=IntPtr.Zero;return;}base.WndProc(ref message);}
    public Action<int> VisibilityClicked;
    public LayerList(){DrawMode=DrawMode.OwnerDrawFixed;AccessibleName="layers; click the eye to Show or Hide a Layer";}
    protected override void OnDrawItem(DrawItemEventArgs e){if(e.Index<0||e.Index>=Items.Count)return;e.DrawBackground();Layer layer=(Layer)Items[e.Index];Color Ink=(e.State&DrawItemState.Selected)!=0?SystemColors.HighlightText:ForeColor;var g=e.Graphics;var State=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;float x=e.Bounds.X+7,y=e.Bounds.Y+e.Bounds.Height/2f;using(var pen=new Pen(Ink,1.5f)){g.DrawBezier(pen,x,y,x+5,y-9,x+13,y-9,x+18,y);g.DrawBezier(pen,x,y,x+5,y+9,x+13,y+9,x+18,y);if(layer.Visible){using(var Brush=new SolidBrush(Ink))g.FillEllipse(Brush,x+6,y-3,6,6);}else g.DrawLine(pen,x-1,y+8,x+19,y-8);}PaintLock(g,layer,e.Bounds,Ink);g.Restore(State);TextRenderer.DrawText(g,(layer.Is3D?"3D  ":layer.IsVector?"◇  ":"▦  ")+layer.Name.ToUpperInvariant(),Font,new Rectangle(e.Bounds.X+58,e.Bounds.Y,e.Bounds.Width-61,e.Bounds.Height),Ink,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);if(draggingLayers&&dropRow==e.Index)using(var marker=new Pen(Color.Teal,3))g.DrawLine(marker,e.Bounds.Left,e.Bounds.Top,e.Bounds.Right,e.Bounds.Top);e.DrawFocusRectangle();}
    protected override void OnMouseDown(MouseEventArgs e){base.OnMouseDown(e);int index=IndexFromPoint(e.Location);if(e.Button==MouseButtons.Left&&e.X>=56&&index>=0){dragRow=index;dragStart=e.Location;Capture=true;}if(e.Button==MouseButtons.Left&&e.X<32&&index>=0&&VisibilityClicked!=null)VisibilityClicked(index);else if(e.Button==MouseButtons.Left&&e.X>=32&&e.X<56&&index>=0&&LockClicked!=null)LockClicked(index);}
    protected override void OnDoubleClick(EventArgs e){if(PointToClient(Cursor.Position).X>=56)base.OnDoubleClick(e);}
    public void TestEye(int index){Rectangle r=GetItemRectangle(index);OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,15,r.Top+r.Height/2,0));}
}
partial class Studio {
    LayerMenu layerMenu;
    void LayerMouseUp(object sender,MouseEventArgs e){
        if(e.Button!=MouseButtons.Right||canvas.Doc==null)return;
        int index=layers.IndexFromPoint(e.Location);if(index<0||index>=layers.Items.Count)return;
        canvas.EndStroke();layers.SelectedIndex=index;
        // keep the menu alive until its owning window is Disposed. ToolStrip
        // still dispatches input after Closed, so disposing There is Unsafe.
        if(layerMenu==null)layerMenu=BuildLayerMenu();
        layerMenu.Close();layerMenu.Tag=canvas.Doc.Selected;
        layerMenu.Items[0].Enabled=canvas.Doc.Layers.Count<32;layerMenu.Items[1].Enabled=canvas.Doc.Layers.Count>1&&!canvas.Doc.Selected.IsLocked;layerMenu.Items[2].Enabled=!canvas.Doc.Selected.IsLocked&&!canvas.Doc.Selected.Is3D;layerMenu.Items[3].Enabled=canvas.Doc.Layers.Count<32;
        Theme.Apply(layerMenu);foreach(ToolStripItem item in layerMenu.Items){item.ForeColor=Theme.Text;item.BackColor=Theme.Panel;}
        layerMenu.Show(layers,e.Location);
    }
    void RunLayerAction(ContextMenuStrip menu,Action action){
        Layer Target=menu.Tag as Layer;
        if(canvas.Doc==null||Target==null)return;
        int index=canvas.Doc.Layers.IndexOf(Target);if(index<0)return;
        canvas.EndStroke();canvas.Doc.Active=index;RefreshLayers();action();
    }
    LayerMenu BuildLayerMenu(){
      var menu=new LayerMenu();menu.Tag=canvas.Doc==null?null:canvas.Doc.Selected;
      var add=new ToolStripMenuItem("ADD"){DropDownDirection=ToolStripDropDownDirection.Left};add.DropDown.Renderer=menu.Renderer;
      add.DropDownItems.Add("PIXEL LAYER",null,delegate{RunLayerAction(menu,delegate{AddTypedLayer(false);});});
      add.DropDownItems.Add("VECTOR LAYER",null,delegate{RunLayerAction(menu,delegate{AddTypedLayer(true);});});
      add.DropDownItems.Add("GLB LAYER",null,delegate{RunLayerAction(menu,ImportGLB);});
      menu.Items.Add(add);
      menu.Items.Add("DELETE",null,delegate{RunLayerAction(menu,DeleteLayer);});
      menu.Items.Add("CLEAR",null,delegate{RunLayerAction(menu,ClearLayer);});
      menu.Items.Add("DUPLICATE",null,delegate{RunLayerAction(menu,DuplicateLayer);});
      foreach(ToolStripItem item in menu.Items)item.Padding=new Padding(8,6,8,6);Theme.Apply(menu);return menu;
    }
    public void TestLayerMenuLifecycle(){
        SetDocument(new Document("A4",100),null);AddLayer();
        Layer Top=canvas.Doc.Selected,bottom=canvas.Doc.Layers[0];
        for(int i=0;i<30;i++){
            int row=i%2;Rectangle r=layers.GetItemRectangle(row);
            LayerMouseUp(layers,new MouseEventArgs(MouseButtons.Right,1,55,r.Top+r.Height/2,0));
            if(layerMenu.IsDisposed||!layerMenu.Visible||layerMenu.Font.Size<12)throw new Exception("Layer menu Open/Font");
            if(i%3==0)layerMenu.TestKey(Keys.Escape);
            else {int choice=i%3==1?0:1;layerMenu.Items[choice].Select();layerMenu.TestKey(Keys.Enter);Layer Target=row==0?Top:bottom;if(Target.Image.GetPixel(0,0).ToArgb()!=(choice==0?0:canvas.Ink.ToArgb()))throw new Exception("Layer menu Keyboard action");}
            Application.DoEvents();if(layerMenu.IsDisposed)throw new Exception("menu Disposed during input dispdtch");
        }
        new Stroke(bottom.Image,"Pen",Color.Red,10,1,new PointF(50,50));
        Rectangle last=layers.GetItemRectangle(1);LayerMouseUp(layers,new MouseEventArgs(MouseButtons.Right,1,55,last.Top+5,0));
        layerMenu.Items[0].Select();layerMenu.TestKey(Keys.Enter);Application.DoEvents();
        if(bottom.Image.GetPixel(50,50).A!=0)throw new Exception("Visible menu clear action");
        Travel(undo,redo);if(bottom.Image.GetPixel(50,50).A==0)throw new Exception("menu clear undo");
        // A Saved menu Target must not affect a replacement Document.
        SetDocument(new Document("A4",100),null);layerMenu.Items[1].PerformClick();
        if(canvas.Doc.Selected.Image.GetPixel(0,0).A!=0)throw new Exception("stale menu Target");
        LayerMouseUp(layers,new MouseEventArgs(MouseButtons.Right,1,55,layers.GetItemRectangle(0).Top+5,0));
        if(layerMenu.Items[2].Enabled)throw new Exception("cannot Delete last Layer");
        layerMenu.Close();dirty=false;
    }
    void PreviewLayerMenu(){Rectangle r=layers.GetItemRectangle(0);LayerMouseUp(layers,new MouseEventArgs(MouseButtons.Right,1,55,r.Top+5,0));using(var b=new Bitmap(layerMenu.Width,layerMenu.Height)){layerMenu.DrawToBitmap(b,new Rectangle(Point.Empty,b.Size));b.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"layer-menu-preview.png"));}layerMenu.Close();}
    void DuplicateLayer(){if(canvas.Doc==null||canvas.Doc.Layers.Count>=32)return;canvas.EndStroke();Layer source=canvas.Doc.Selected;var Copy=new Layer(source.Name+" copy",1,1);Copy.Image.Dispose();Copy.Image=(Bitmap)source.Image.Clone();Copy.Locked=source.IsLocked;Copy.Adjustment=source.Adjustment.Copy();Copy.Scene=source.Scene==null?null:source.Scene.Copy();Copy.MirrorVertical=source.MirrorVertical;Copy.MirrorHorizontal=source.MirrorHorizontal;Copy.MirrorRadial=source.MirrorRadial;Copy.MirrorRepeats=source.MirrorRepeats;Copy.IsVector=source.IsVector;Copy.Vectors=source.CopyVectors();Copy.Opacity=source.Opacity;Copy.Visible=source.Visible;canvas.Doc.Layers.Insert(canvas.Doc.Active+1,Copy);canvas.Doc.Active++;Changed();RefreshLayers();}
    void FillLayer(){if(canvas.Doc==null||canvas.Doc.Selected.Is3D||canvas.Doc.Selected.IsLocked)return;canvas.EndStroke();Push(undo,canvas.Doc.Selected);redo.Clear();canvas.Doc.Selected.Vectors.Clear();canvas.CancelVector();using(var g=Graphics.FromImage(canvas.Doc.Selected.Image))g.Clear(canvas.Doc.Selected.IsVector?Color.Transparent:Color.FromArgb(255,canvas.Ink.R,canvas.Ink.G,canvas.Ink.B));if(canvas.Doc.Selected.IsVector)canvas.Doc.Selected.Vectors.Add(new VectorShape{Kind="Polygon",Filled=true,Colour=canvas.Ink,Points=new System.Collections.Generic.List<PointF>{new PointF(0,0),new PointF(canvas.Doc.Width,0),new PointF(canvas.Doc.Width,canvas.Doc.Height),new PointF(0,canvas.Doc.Height)}});Changed();RefreshLayers();}
    void ClearLayer(){if(canvas.Doc==null||canvas.Doc.Selected.Is3D||canvas.Doc.Selected.IsLocked)return;canvas.EndStroke();Push(undo,canvas.Doc.Selected);redo.Clear();canvas.Doc.Selected.Vectors.Clear();canvas.CancelVector();using(var g=Graphics.FromImage(canvas.Doc.Selected.Image))g.Clear(Color.Transparent);Changed();RefreshLayers();}
    public void TestLayerControls(){
        SetDocument(new Document("A4",100),null);AddLayer();Layer Top=canvas.Doc.Selected,bottom=canvas.Doc.Layers[0];
        layers.TestEye(1);if(bottom.Visible||!Top.Visible||canvas.Doc.Selected!=bottom)throw new Exception("eye must Target clicked Layer");
        layers.TestEye(1);if(!bottom.Visible)throw new Exception("eye must Show Layer");
        new Stroke(bottom.Image,"Pen",Color.Red,10,1,new PointF(50,50));bottom.Visible=false;bottom.Opacity=.4f;
        using(var menu=BuildLayerMenu()){
            if(menu.Items.Count!=5||menu.Items[0].Text!="CLEAR"||menu.Items[1].Text!="FILL"||menu.Items[2].Text!="DELETE")throw new Exception("Layer menu order");
            menu.Items[0].PerformClick();if(bottom.Image.GetPixel(50,50).A!=0)throw new Exception("clear Layer");
            Travel(undo,redo);if(bottom.Image.GetPixel(50,50).A==0||bottom.Visible)throw new Exception("undo clear preserves visibility");
            canvas.Ink=Color.FromArgb(23,146,201);menu.Items[1].PerformClick();
            foreach(Point Point in new[]{new Point(0,0),new Point(50,50),new Point(bottom.Image.Width-1,bottom.Image.Height-1)})if(bottom.Image.GetPixel(Point.X,Point.Y).ToArgb()!=canvas.Ink.ToArgb())throw new Exception("Fill uses Selected Colour across entire Layer");
            if(bottom.Visible||bottom.Opacity!=.4f||Top.Image.GetPixel(0,0).A!=0)throw new Exception("Fill preserves Layer properties and other layers");
            Travel(undo,redo);if(bottom.Image.GetPixel(0,0).A!=0||bottom.Image.GetPixel(50,50).R!=255)throw new Exception("undo Fill");
            Travel(redo,undo);if(bottom.Image.GetPixel(0,0).ToArgb()!=canvas.Ink.ToArgb())throw new Exception("redo Fill");
        }
        mode.SelectedItem="RGB";SyncColour();foreach(Control panel in channels.Controls)foreach(Control field in panel.Controls)if(field is NumericUpDown&&field.Font.SizeInPoints!=opacity.Font.SizeInPoints)throw new Exception("Colour value Font must Match Opacity");
        if(radialRepeats.Font.SizeInPoints!=opacity.Font.SizeInPoints)throw new Exception("Radial value Font must Match Opacity");dirty=false;
    }
    public void TestSizeLimits(){foreach(string tool in new[]{"Pencil","Pen","Ink Pen"}){SelectTool(tool);if(size.Minimum!=2||size.Maximum!=20||sizeSlider.Minimum!=2||sizeSlider.Maximum!=20)throw new Exception("Narrow tool Size limits");size.Value=20;canvas.Focus();Message m=new Message();ProcessCmdKey(ref m,Keys.OemCloseBrackets);if(size.Value!=20)throw new Exception("Size Upper shortcut limit");size.Value=2;ProcessCmdKey(ref m,Keys.OemOpenBrackets);if(size.Value!=2)throw new Exception("Size lower shortcut limit");canvas.TestRightClick();if(brushPopup.SizeValue.Minimum!=2||brushPopup.SizeValue.Maximum!=20||brushPopup.SizeSlider.Orientation!=Orientation.Horizontal||brushPopup.SizeValue.Parent!=null)throw new Exception("Popup limits and layout");brushPopup.Close();}SelectTool("Eraser");if(size.Maximum!=200||size.Minimum!=1)throw new Exception("other tools retain range");var old=new ToolPreset{Tool="Pencil",Size=300};ApplyTool(old);if(size.Value!=20)throw new Exception("Legacy Preset Size clamp");dirty=false;}
}
}
