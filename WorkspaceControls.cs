using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Linq;using System.Windows.Forms;
namespace IDSketcher{
partial class Studio{ public void TestWorkspaceControls(){previewing=true;Show();SetDocument(new Document("A4",100),null);canvas.Zoom=1;canvas.Offset=PointF.Empty;canvas.TestRotatedView();if(pageFillButton.Width!=toolButtons["Paintbrush"].Width||fillBucketButton.Width!=toolButtons["Paintbrush"].Width)throw new Exception("Equal button widths");if(guideMode.Parent!=null)throw new Exception("Guides dropdown removed");AddTypedLayer(true);if(guidesButton.Visible||vectorFillButtons.Any(b=>b.TextAlign!=ContentAlignment.MiddleCenter))throw new Exception("Vector guide visibility/fill alignment");if(vectorFillButtons.Any(b=>b.Parent!=vectorColourPanel||!b.Visible))throw new Exception("Vector colour fills placement");vectorMode.SelectedItem="Curvature pen";canvas.TestDrag(new Point(50,50),new Point(50,50));canvas.TestDrag(new Point(150,100),new Point(150,100));canvas.TestDrag(new Point(250,50),new Point(250,50));canvas.FinishVector();vectorPicker.PerformClick();canvas.SelectAllVectors();canvas.SelectVectorPoint(1,false);using(var menu=CreateVectorPointMenu()){if(menu.Items.Count!=1||menu.Items[0].Text!="REMOVE POINT/S")throw new Exception("One-point context menu");}canvas.SelectVectorPoint(0,true);using(var menu=CreateVectorPointMenu()){if(menu.Items.Count!=2)throw new Exception("Two-point context menu");int count=canvas.SelectedVector.Points.Count;menu.Items[1].PerformClick();if(canvas.SelectedVector.Points.Count!=count+1)throw new Exception("Context add point");}if(vectorControls.OfType<Button>().Any(b=>b.Text.StartsWith("Add point")||b.Text.StartsWith("Remove points")))throw new Exception("Point buttons removed");canvas.Doc.Active=0;RefreshLayers();if(vectorFillButtons.Any(b=>b.Visible))throw new Exception("Pixel fills hidden");dirty=false;}
 Button[] pressureButtons;FlowLayoutPanel pressureButtonRow;
 void AddPressureButtons(FlowLayoutPanel tools){
  pressureButtonRow=new FlowLayoutPanel{Width=120,Height=38,WrapContents=false,Margin=new Padding(0)};pressureButtons=new Button[3];string[] symbols={"X","S","P"};
  for(int i=0;i<3;i++){int index=i;var b=Theme.Button(symbols[i],delegate{pressureMode.SelectedIndex=index;canvas.Focus();});b.Size=new Size(32,32);b.Margin=new Padding(2);b.AccessibleName=pressureMode.Items[i].ToString();b.TextAlign=ContentAlignment.MiddleCenter;if(i==0){b.Text="";b.Image=CursorOptionIcon(2);b.ImageAlign=ContentAlignment.MiddleCenter;}pressureButtons[i]=b;pressureButtonRow.Controls.Add(b);}
  tools.Controls.Add(pressureButtonRow);pressureMode.SelectedIndexChanged+=delegate{SyncPressureButtons();};SyncPressureButtons();
 }
 void SyncPressureButtons(){if(pressureButtons==null)return;for(int i=0;i<pressureButtons.Length;i++){bool active=pressureMode.SelectedIndex==i;pressureButtons[i].BackColor=active?Theme.Accent:Theme.Panel;pressureButtons[i].ForeColor=active?Theme.Bg:Theme.Text;}}
 public void TestPressureButtons(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);SelectTool("Pencil");
  if(pressureMode.Parent!=null||pageFillButton.Image==null)throw new Exception("Pressure dropdown/fill icon");
  for(int i=0;i<3;i++){pressureButtons[i].PerformClick();if(pressureMode.SelectedIndex!=i||canvas.PressureMode!=pressureMode.Items[i].ToString()||pressureButtons[i].Width!=pressureButtons[i].Height)throw new Exception("Pressure icon action");}
  pressureButtons[1].PerformClick();SelectTool("Pen");SelectTool("Pencil");if(pressureMode.SelectedIndex!=1)throw new Exception("Pressure recall");
  predictionSlider.Value=73;if(predictiveToggle.Text!="PREDICT · 73%")throw new Exception("Predictive percentage");
  if(toolPanel.Controls.OfType<Button>().Any(b=>b.Text=="Draw on guide"||b.Text=="Reset guide"))throw new Exception("Removed guide buttons");dirty=false;
 } Button guidesButton;Panel vectorColourPanel;Control[] shiftedColourControls;Button[] vectorFillButtons;bool fillRowShown;
 void ConfigureCompactTools(FlowLayoutPanel top,FlowLayoutPanel tools,Panel colour){
  foreach(Control c in tools.Controls){c.Width=118;if(c is Button)((Button)c).TextAlign=ContentAlignment.MiddleRight;}
  simplifyPaths.TextAlign=copyStyleButton.TextAlign=ContentAlignment.MiddleCenter;foreach(var booleanButton in booleanButtons)booleanButton.TextAlign=ContentAlignment.MiddleCenter;var guides=guidesButton=Theme.Button("Guides",delegate(object sender,EventArgs e){var menu=new UppercaseContextMenu{Font=new System.Drawing.Font("Segoe UI",9,System.Drawing.FontStyle.Bold)};foreach(string name in guideMode.Items){string choice=name;var item=menu.Items.Add(name);((ToolStripMenuItem)item).Checked=guideMode.Text==name;item.Click+=delegate{guideMode.SelectedItem=choice;canvas.Focus();};}menu.Closed+=delegate{BeginInvoke((Action)delegate{menu.Dispose();});};menu.Show((Control)sender,new Point(0,((Control)sender).Height));});top.Controls.Add(guides);
  vectorColourPanel=colour;vectorFillButtons=vectorControls.OfType<Button>().Where(b=>string.Equals(b.Text,"Fill",StringComparison.OrdinalIgnoreCase)||string.Equals(b.Text,"No fill",StringComparison.OrdinalIgnoreCase)).ToArray();shiftedColourControls=colour.Controls.Cast<Control>().Where(c=>c.Top>=192).ToArray();
  for(int i=0;i<vectorFillButtons.Length;i++){var b=vectorFillButtons[i];b.Parent=colour;b.TextAlign=ContentAlignment.MiddleCenter;b.Text=i==0?"Fill":"Remove fill";b.SetBounds(i*120,192,114,28);}
  ConfigureToolCaptions(top);ConfigureColourTools(colour);ConfigureNarrowPalette();canvas.VectorPointMenuRequested=ShowVectorPointMenu;SyncVectorFillPlacement(canvas.Doc!=null&&canvas.Doc.Selected.IsVector);
 }
 void SyncVectorFillPlacement(bool vector){
  if(guidesButton!=null)guidesButton.Visible=!vector;if(vectorColourPanel==null)return;foreach(var b in vectorFillButtons)b.Visible=vector;if(weightCaption!=null)weightCaption.Visible=vector;vectorWidth.Visible=vector;
  fillRowShown=vector;LayoutNarrowPalette(vector);
 }
 ContextMenuStrip CreateVectorPointMenu(){
  var menu=new UppercaseContextMenu{Font=new System.Drawing.Font("Segoe UI",9,System.Drawing.FontStyle.Bold)};
  if(canvas.HasSelectedVectorPoints)menu.Items.Add("Remove point/s",null,delegate{canvas.EditCurvePoint(false);canvas.Focus();}).Enabled=canvas.CanRemoveCurvePoint;
  if(canvas.CanAddCurvePoint)menu.Items.Add("Add point",null,delegate{canvas.EditCurvePoint(true);canvas.Focus();});
  return menu;}void ShowVectorPointMenu(Point location){var menu=CreateVectorPointMenu();if(menu.Items.Count==0){menu.Dispose();return;}menu.Closed+=delegate{BeginInvoke((Action)delegate{menu.Dispose();});};menu.Show(canvas,location);
 }
}
partial class Canvas{
 public Action<Point> VectorPointMenuRequested;public bool HasSelectedVectorPoints{get{return selectedPoints.Count>0;}}
 public float ViewAngle;bool rotationHeld,rotatingView,paintingView;double lastViewAngle;long lastRotationTap;bool rotationMoved;
 PointF ViewCentre{get{return new PointF(ClientSize.Width/2f,ClientSize.Height/2f);}}
 PointF TurnView(PointF p,float degrees){var c=ViewCentre;double a=degrees*Math.PI/180,x=p.X-c.X,y=p.Y-c.Y;return new PointF(c.X+(float)(x*Math.Cos(a)-y*Math.Sin(a)),c.Y+(float)(x*Math.Sin(a)+y*Math.Cos(a)));}
 PointF RotateView(PointF p){return TurnView(p,ViewAngle);}
 PointF UnrotateView(PointF p){return TurnView(p,-ViewAngle);}
 void ApplyViewRotation(Graphics g){var c=ViewCentre;g.TranslateTransform(c.X,c.Y);g.RotateTransform(ViewAngle);g.TranslateTransform(-c.X,-c.Y);}
 public void StartViewRotation(){if(rotationHeld)return;long now=DateTime.UtcNow.Ticks;if(!rotationMoved&&lastRotationTap!=0&&now-lastRotationTap<=TimeSpan.TicksPerMillisecond*SystemInformation.DoubleClickTime){ViewAngle=0;lastRotationTap=0;}else lastRotationTap=now;rotationMoved=false;EndStroke();rotationHeld=true;Invalidate();}
 public void StopViewRotation(){rotationHeld=false;rotatingView=false;Capture=false;Invalidate();}
 bool RotationMouseDown(MouseEventArgs e){if(!rotationHeld||e.Button!=MouseButtons.Left)return false;var c=ViewCentre;if(StrokeSession.Distance(e.Location,c)<28||StrokeSession.Distance(e.Location,c)>65)return false;lastViewAngle=Math.Atan2(e.Y-c.Y,e.X-c.X);rotatingView=true;Capture=true;return true;}
 bool RotationMouseMove(MouseEventArgs e){if(!rotatingView)return false;if(rotatingView){rotationMoved=true;var c=ViewCentre;double angle=Math.Atan2(e.Y-c.Y,e.X-c.X);ViewAngle=(ViewAngle+(float)((angle-lastViewAngle)*180/Math.PI))%360;lastViewAngle=angle;Invalidate();}return true;}
 bool RotationMouseUp(MouseEventArgs e){if(!rotatingView)return false;rotatingView=false;Capture=false;return true;}
 void PaintRotationControl(Graphics g){if(!rotationHeld)return;var c=ViewCentre;g.SmoothingMode=SmoothingMode.AntiAlias;using(var back=new SolidBrush(Color.FromArgb(190,245,245,245)))g.FillEllipse(back,c.X-62,c.Y-62,124,124);using(var pen=new Pen(Color.Teal,3)){pen.CustomEndCap=new AdjustableArrowCap(4,5);g.DrawArc(pen,c.X-47,c.Y-47,94,94,20,135);g.DrawArc(pen,c.X-47,c.Y-47,94,94,200,135);}using(var font=new Font(Font.FontFamily,10))g.DrawString(Math.Round(ViewAngle)+"°",font,Brushes.Teal,c.X-18,c.Y-8);}
 public void TestRotatedView(){
  ViewAngle=37;var p=new PointF(120,90);var screen=ScreenPoint(p);var actual=Pixel(Point.Round(screen));if(StrokeSession.Distance(p,actual)>2/Zoom)throw new Exception("Rotated pointer mapping");
  var a=ScreenPoint(new PointF(100,100));var b=ScreenPoint(new PointF(200,100));Tool="Pen";PressureMode="No pressure";Smoothing=0;BrushSize=8;TestDrag(Point.Round(a),Point.Round(b));if(Doc.Selected.Image.GetPixel(150,100).A==0)throw new Exception("Rotated view drawing alignment");TestDraftPaint();if(paintingView)throw new Exception("Paint view state");StartViewRotation();var c=ViewCentre;RotationMouseDown(new MouseEventArgs(MouseButtons.Left,1,(int)c.X+50,(int)c.Y,0));RotationMouseMove(new MouseEventArgs(MouseButtons.Left,0,(int)c.X,(int)c.Y+50,0));if(Math.Abs(ViewAngle-127)>1)throw new Exception("Clockwise rotation");RotationMouseMove(new MouseEventArgs(MouseButtons.Left,0,(int)c.X+50,(int)c.Y,0));if(Math.Abs(ViewAngle-37)>1)throw new Exception("Anticlockwise rotation");StopViewRotation();ViewAngle=0;
 }
}
}
