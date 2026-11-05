using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace IDSketcher {
partial class Canvas : Control {
    public Document Doc;
    public string Tool="Select",Guide="Freehand",PressureMode="Simulated pressure";
    public bool VerticalMirror,HorizontalMirror,RadialSymmetry;
    public int RadialRepeats=6,TaperPercent=50;
    public bool EditGuide,BrushShapeCursor=true;
    public bool SimulatePressure {get{return PressureMode=="Simulated pressure";}}
    public float BrushSize=5,BrushOpacity=1,BrushFlow=1,BrushHardness=1,Zoom=1;
    public Color Ink {get;set;}
    public int Smoothing=50;
    public PointF Offset;
    public bool PixelCacheUpdated;public byte[] StrokeOriginal;public Action BeforeStroke,AfterStroke,ViewChanged,PenStatusChanged;
    public Action<Point> BrushMenuRequested;
    public string PenStatus="Waiting for pen · enable Windows Ink";
    StrokeSession stroke;
    StrokeSmoother smoother;
    bool panning,space,inside;
    Point mouse,panStart;
    PointF oldOffset,dragOrigin;
    Dictionary<string,PointF[]> savedGuides=new Dictionary<string,PointF[]>();
    PointF[] handles,guidePoints,dragHandles;
    int handle=-1,guideIndex;
    float samplePressure=1;
    bool penInput;

    public Canvas(){Ink=Color.FromArgb(51,51,51);DoubleBuffered=true;Dock=DockStyle.Fill;BackColor=Color.FromArgb(22,25,30);SetStyle(ControlStyles.Selectable,true);TabStop=true;Cursor=Cursors.Cross;}
    public void Fit(){if(Doc==null)return;Zoom=Math.Max(.02f,Math.Min((ClientSize.Width-90f)/Doc.Width,(ClientSize.Height-90f)/Doc.Height));Offset=new PointF((ClientSize.Width-Doc.Width*Zoom)/2,(ClientSize.Height-Doc.Height*Zoom)/2);Invalidate();if(ViewChanged!=null)ViewChanged();}
    PointF Pixel(Point p){var q=UnrotateView(p);return new PointF((q.X-Offset.X)/Zoom,(q.Y-Offset.Y)/Zoom);}
    PointF ScreenPoint(PointF p){var q=new PointF(Offset.X+p.X*Zoom,Offset.Y+p.Y*Zoom);return paintingView?q:RotateView(q);}
    public void SetZoom(float value,Point anchor){BeginQuickNavigation();PointF p=Pixel(anchor);Zoom=Math.Max(.02f,Math.Min(8,value));var unrotated=UnrotateView(anchor);Offset=new PointF(unrotated.X-p.X*Zoom,unrotated.Y-p.Y*Zoom);Invalidate();if(ViewChanged!=null)ViewChanged();}
    public void RefreshCursor(bool repaint=true){if(HideCanvasCursor&&ClientRectangle.Contains(PointToClient(System.Windows.Forms.Cursor.Position))){NativePen.SetCursor(IntPtr.Zero);if(repaint)Invalidate();return;}if(Doc!=null&&Doc.Selected.Is3D){Cursor=Move3D?Cursors.SizeAll:Cursors.Hand;NativePen.SetCursor(Cursor.Handle);if(repaint)Invalidate();return;}Cursor=panning||space?Cursors.Hand:Doc!=null&&Doc.Selected.IsRaster&&Tool=="Select"&&!FillTool&&SelectionMode=="Draw"?Cursors.Arrow:Doc!=null&&Doc.Selected.IsVector?(VectorMode=="Select/edit"?Cursors.Arrow:Cursors.Cross):EditGuide&&Guide!="Freehand"?Cursors.Arrow:Cursors.Cross;if(VectorMode=="Brushes"&&SelectionMode=="Draw"&&TransformMode==null&&Tool!="Select"&&!FillTool&&!EyedropperActive&&!HideCanvasCursor&&BrushShapeCursor&&!panning&&!(EditGuide&&Guide!="Freehand")&&ClientRectangle.Contains(PointToClient(System.Windows.Forms.Cursor.Position)))NativePen.SetCursor(IntPtr.Zero);if(TransformMode!=null||FillTool||SelectionMode!="Draw"||Doc!=null&&Doc.Selected.IsVector)NativePen.SetCursor(Cursor.Handle);if(repaint)Invalidate();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Doc==null)return;Graphics g=e.Graphics;paintingView=true;ApplyViewRotation(g);g.TranslateTransform(Offset.X,Offset.Y);g.ScaleTransform(Zoom,Zoom);ConfigureViewSampling(g);if(!PaintQuickNavigation(g))PaintTransformDocument(g);g.ResetTransform();ApplyViewRotation(g);PaintMillimetreGrid(g);using(Pen p=new Pen(Color.Gray))g.DrawRectangle(p,Offset.X,Offset.Y,Doc.Width*Zoom,Doc.Height*Zoom);PaintSymmetry(g);PaintGuides(g);PaintVectors(g);PaintStraight(g);PaintSelection(g);PaintTransform(g);
        if(VectorMode=="Brushes"&&SelectionMode=="Draw"&&TransformMode==null&&Tool!="Select"&&!FillTool&&!EyedropperActive&&!HideCanvasCursor&&inside&&BrushShapeCursor&&!panning&&!(Guide!="Freehand"&&EditGuide)){
            PointF centre=paintingView?UnrotateView(mouse):mouse;if(Guide!="Freehand"&&guidePoints!=null)centre=ScreenPoint(guidePoints[NearestGuide(Pixel(mouse))]);
            float diameter=BrushSize*Zoom*(PressureMode=="Wacom pressure"&&penInput?StrokeSession.PressureScale(samplePressure):1);
            GraphicsState cursorState=g.Save();g.SmoothingMode=SmoothingMode.AntiAlias;g.TranslateTransform(centre.X,centre.Y);if(Tool=="Ink Pen")g.RotateTransform(45);float height=Tool=="Ink Pen"?diameter*.65f:diameter;using(Pen p=new Pen(Color.Black,2))g.DrawEllipse(p,-diameter/2,-height/2,diameter,height);using(Pen p=new Pen(Color.White,1))g.DrawEllipse(p,-diameter/2,-height/2,diameter,height);g.Restore(cursorState);
        }
        paintingView=false;g.ResetTransform();PaintRotationControl(g);WarmNavigationAfterPaint();
    }
    protected override void OnMouseDown(MouseEventArgs e){if(RotationMouseDown(e))return;base.OnMouseDown(e);Focus();if(Doc==null)return;if(e.Button==MouseButtons.Left&&EyedropperActive){PickCanvasColour(Pixel(e.Location));return;}if(e.Button==MouseButtons.Right&&!Doc.Selected.Is3D&&SelectionMode=="Polygon selection"){FinishSelection();return;}mouse=e.Location;inside=true;
        if(e.Button==MouseButtons.Middle||(e.Button==MouseButtons.Left&&space)){panning=true;PreparePanImage();panStart=e.Location;oldOffset=Offset;Capture=true;RefreshCursor();return;}
        if(Doc.Selected.Is3D){BeginThreeD(e);return;}if(e.Button==MouseButtons.Right&&Doc.Selected.IsVector){AcceptVectorDraft();if(VectorMode=="Select/edit"&&VectorPointMenuRequested!=null)VectorPointMenuRequested(e.Location);return;}if(e.Button!=MouseButtons.Left)return;PointF p=Pixel(e.Location);
        if(TransformDown(p))return;if(VectorBoxDown(p))return;if(Doc.Selected.IsVector){if(SelectionMode!="Draw"&&SelectionDown(p))return;VectorDown(p);return;}if(SelectionDown(p))return;if(Tool=="Select"&&!FillTool)return;if(FillTool){FillVisibleAt(p);return;}if(Guide!="Freehand"&&EditGuide){BeginHandle(p);return;}if(Doc.Selected.IsLocked)return;
        if(p.X<0||p.Y<0||p.X>=Doc.Width||p.Y>=Doc.Height)return;
        if(!Doc.Selected.Visible){MessageBox.Show("Show the selected layer before drawing.","Hidden layer");return;}
        if((ModifierKeys&Keys.Shift)!=0){BeginStraight(p);return;}ResetShift();if(Guide!="Freehand"){guideIndex=NearestGuide(p);p=guidePoints[guideIndex];}
        arcStroke=(ModifierKeys&Keys.Alt)!=0&&Guide=="Freehand";ellipseStroke=(ModifierKeys&Keys.Control)!=0&&Guide=="Freehand";if(BeforeStroke!=null)BeforeStroke();
        stroke=new StrokeSession(Doc.Selected.Image,Tool,Ink,BrushSize,BrushOpacity,p,SimulatePressure,PressureMode=="Wacom pressure"&&penInput,samplePressure,TaperPercent,new SymmetryMap(Doc.Width,Doc.Height,VerticalMirror,HorizontalMirror,RadialSymmetry,RadialRepeats),BrushFlow,BrushHardness,SelectionMask,StrokeOriginal);
        Doc.Selected.UpdatePixelAdjustment(stroke.TakeDirty());StrokeOriginal=null;smoother=new StrokeSmoother(p,Smoothing,Zoom,stroke.Move);if(!penInput)Capture=true;Invalidate();
    }
    protected override void OnMouseMove(MouseEventArgs e){if(RotationMouseMove(e))return;if(ViewAngle!=0)Invalidate();base.OnMouseMove(e);Rectangle oldCursor=CursorBounds();bool localPaint=!panning&&handle<0;mouse=e.Location;inside=true;
        if(panning){var a=UnrotateView(e.Location);var b=UnrotateView(panStart);Offset=new PointF(oldOffset.X+a.X-b.X,oldOffset.Y+a.Y-b.Y);}
        else if(MoveThreeD(e))return;else if(VectorBoxMove(Pixel(e.Location)))return;else if(TransformMove(Pixel(e.Location)))return;else if(SelectionMove(Pixel(e.Location)))return;else if(straightDragging){straightEnd=Pixel(e.Location);Invalidate();return;}else if(Doc!=null&&Doc.Selected.IsVector){VectorMove(Pixel(e.Location));return;}else if(handle>=0)DragHandle(Pixel(e.Location));
        else if(stroke!=null){MoveStroke(Pixel(e.Location));}
        RefreshCursor(!localPaint);if(localPaint){Invalidate(oldCursor);Invalidate(CursorBounds());if(stroke!=null){Rectangle r=stroke.TakeDirty();Doc.Selected.UpdatePixelAdjustment(r);if(!r.IsEmpty){PointF start=ScreenPoint(r.Location);Invalidate(Rectangle.FromLTRB((int)Math.Floor(start.X)-ViewSamplingPadding,(int)Math.Floor(start.Y)-ViewSamplingPadding,(int)Math.Ceiling(start.X+r.Width*Zoom)+ViewSamplingPadding,(int)Math.Ceiling(start.Y+r.Height*Zoom)+ViewSamplingPadding));}}}
    }
    Rectangle CursorBounds(){PointF centre=paintingView?UnrotateView(mouse):mouse;if(Guide!="Freehand"&&guidePoints!=null)centre=ScreenPoint(guidePoints[NearestGuide(Pixel(mouse))]);float radius=BrushSize*Zoom/2+5;return Rectangle.FromLTRB((int)Math.Floor(centre.X-radius),(int)Math.Floor(centre.Y-radius),(int)Math.Ceiling(centre.X+radius),(int)Math.Ceiling(centre.Y+radius));}
    protected override void OnMouseUp(MouseEventArgs e){if(RotationMouseUp(e))return;if(panning){EndStroke();base.OnMouseUp(e);return;}if(Doc!=null&&Doc.Selected.Is3D&&!panning){EndThreeD();base.OnMouseUp(e);return;}if(e.Button==MouseButtons.Left&&VectorBoxUp(Pixel(e.Location))){base.OnMouseUp(e);return;}if(e.Button==MouseButtons.Left&&TransformUp(Pixel(e.Location))){base.OnMouseUp(e);return;}if(e.Button==MouseButtons.Left&&SelectionUp(Pixel(e.Location))){base.OnMouseUp(e);return;}if(e.Button==MouseButtons.Left&&straightDragging){FinishStraight(Pixel(e.Location));base.OnMouseUp(e);return;}if(e.Button==MouseButtons.Left&&Doc!=null&&Doc.Selected.IsVector){VectorUp(Pixel(e.Location));base.OnMouseUp(e);return;}if(e.Button==MouseButtons.Left){if(handle>=0)DragHandle(Pixel(e.Location));else if(stroke!=null){MoveStroke(Pixel(e.Location));}}EndStroke();base.OnMouseUp(e);if(e.Button==MouseButtons.Right&&Doc!=null&&Doc.Selected.IsRaster&&SelectionMode!="Polygon selection"&&BrushMenuRequested!=null)BrushMenuRequested(PointToScreen(e.Location));}
    public void TestRightClick(){OnMouseDown(new MouseEventArgs(MouseButtons.Right,1,100,100,0));OnMouseUp(new MouseEventArgs(MouseButtons.Right,1,100,100,0));}
    public void TestColourStroke(){Guide="Freehand";PressureMode="No pressure";Smoothing=0;Zoom=1;Offset=PointF.Empty;BrushSize=12;BrushOpacity=1;OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,50,50,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,150,50,0));}
    public void EndStroke(){EndThreeD();if(vectorBoxDragging){vectorBoxDragging=false;VectorBoxMode=false;}CancelTransform();ResetShift();if(stroke!=null){if(Guide=="Freehand"){if(ellipseStroke)stroke.FitClosedEllipse();else if(arcStroke)stroke.FitArc();else{smoother.Finish();if(Predictive)stroke.Predict(Prediction,Zoom);}}stroke.Finish();Doc.Selected.UpdatePixelAdjustment(stroke.TakeDirty());PixelCacheUpdated=true;stroke=null;smoother=null;ellipseStroke=arcStroke=false;if(AfterStroke!=null)AfterStroke();}handle=-1;panning=false;StopQuickPan();Capture=false;RefreshCursor();}
    protected override void OnMouseCaptureChanged(EventArgs e){if(TransformMode!=null){if(!Capture&&transformDragging)CancelTransform();base.OnMouseCaptureChanged(e);return;}if(!Capture&&!penInput){if(selectionDragging)CancelSelectionDraft();if(vectorDragging)VectorUp(Pixel(mouse));EndStroke();}base.OnMouseCaptureChanged(e);}
    protected override void OnMouseLeave(EventArgs e){inside=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseEnter(EventArgs e){inside=true;RefreshCursor();base.OnMouseEnter(e);}
    protected override void OnMouseWheel(MouseEventArgs e){if(Doc!=null&&Doc.Selected.Is3D&&(ModifierKeys&Keys.Control)==0){ZoomThreeD((float)Math.Pow(1.12,e.Delta/120.0));return;}if(stroke==null&&handle<0)SetZoom(Zoom*(float)Math.Pow(1.15,e.Delta/120.0),e.Location);base.OnMouseWheel(e);}
    protected override void OnKeyDown(KeyEventArgs e){UpdateFreehandModifiers(e.Modifiers);if(e.KeyCode==Keys.Space){BeginSpaceNavigation();e.Handled=true;}base.OnKeyDown(e);}
    protected override void OnKeyUp(KeyEventArgs e){if(e.KeyCode==Keys.Space){if(panning)EndStroke();space=false;StopViewRotation();RefreshCursor();e.Handled=true;}base.OnKeyUp(e);}
    protected override void OnLostFocus(EventArgs e){StopViewRotation();space=false;if(selectionDragging)CancelSelectionDraft();if(vectorDragging)VectorUp(Pixel(mouse));EndStroke();base.OnLostFocus(e);}

    public void SelectGuide(string name){ResetShift();EndStroke();Guide=name;EditGuide=false;if(name!="Freehand"){if(savedGuides.ContainsKey(name)){handles=savedGuides[name];RebuildGuide();}else ResetGuide();}RefreshCursor();}
    public void ResetAllGuides(){EllipseRotation=0;savedGuides.Clear();ResetGuide();}
    public void ResetGuide(){if(Doc==null)return;if(Guide=="Ellipse")EllipseRotation=0;float w=Doc.Width,h=Doc.Height;
        handles=Guide=="Spline"?new[]{new PointF(w*.2f,h*.65f),new PointF(w*.35f,h*.15f),new PointF(w*.65f,h*.85f),new PointF(w*.8f,h*.35f)}:Guide=="Ellipse"?new[]{new PointF(w*.3f,h*.25f),new PointF(w*.7f,h*.75f)}:new[]{new PointF(w*.2f,h*.5f),new PointF(w*.8f,h*.5f)};
        if(Guide!="Freehand"){savedGuides[Guide]=handles;RebuildGuide();}Invalidate();
    }
    public static PointF Bezier(PointF[] p,float t){float u=1-t;return new PointF(u*u*u*p[0].X+3*u*u*t*p[1].X+3*u*t*t*p[2].X+t*t*t*p[3].X,u*u*u*p[0].Y+3*u*u*t*p[1].Y+3*u*t*t*p[2].Y+t*t*t*p[3].Y);}
    bool French {get{return Guide.StartsWith("French curve");}}
    bool GuideClosed {get{return Guide=="Ellipse"||French;}}
    void RebuildGuide(){if(French){RebuildFrench();return;}
        float length=0;for(int i=1;i<handles.Length;i++)length+=StrokeSession.Distance(handles[i-1],handles[i]);
        if(Guide=="Ellipse")length*=4;
        int count=Math.Max(64,Math.Min(20000,(int)Math.Ceiling(length)));
        bool closed=Guide=="Ellipse";guidePoints=new PointF[count+(closed?0:1)];
        for(int i=0;i<guidePoints.Length;i++){float t=(float)i/count;
            if(Guide=="Spline")guidePoints[i]=Bezier(handles,t);
            else if(closed){float rx=Math.Abs(handles[1].X-handles[0].X)/2,ry=Math.Abs(handles[1].Y-handles[0].Y)/2;guidePoints[i]=new PointF((handles[0].X+handles[1].X)/2+rx*(float)Math.Cos(t*Math.PI*2),(handles[0].Y+handles[1].Y)/2+ry*(float)Math.Sin(t*Math.PI*2));}
            else guidePoints[i]=new PointF(handles[0].X+(handles[1].X-handles[0].X)*t,handles[0].Y+(handles[1].Y-handles[0].Y)*t);
            if(closed)guidePoints[i]=RotateEllipse(guidePoints[i],EllipseRotation);
        }
    }
    public float EllipseRotation;
    PointF RotateEllipse(PointF p,float degrees){PointF c=Centre();double a=degrees*Math.PI/180,x=p.X-c.X,y=p.Y-c.Y;return new PointF(c.X+(float)(x*Math.Cos(a)-y*Math.Sin(a)),c.Y+(float)(x*Math.Sin(a)+y*Math.Cos(a)));}
    public void SetEllipseRotation(float degrees){EndStroke();EllipseRotation=degrees;if(Guide=="Ellipse"){RebuildGuide();Invalidate();}}
    int NearestGuide(PointF p){int best=0;float distance=float.MaxValue;for(int i=0;i<guidePoints.Length;i++){float d=StrokeSession.Distance(p,guidePoints[i]);if(d<distance){best=i;distance=d;}}return best;}
    PointF Centre(){return new PointF((handles[0].X+handles[handles.Length-1].X)/2,(handles[0].Y+handles[handles.Length-1].Y)/2);}
    PointF RotationHandle(){PointF c=Centre();float top=Math.Min(handles[0].Y,handles[1].Y);return RotateEllipse(new PointF(c.X,top-40/Zoom),EllipseRotation);}
    float rotationStart,rotationPointerStart;
    PointF[] GuideGrips(){if(Guide!="Ellipse")return handles;var c=Centre();return new[]{new PointF(handles[0].X,c.Y),new PointF(handles[1].X,c.Y),new PointF(c.X,handles[0].Y),new PointF(c.X,handles[1].Y)};}
    void BeginHandle(PointF p){if(Guide=="Ellipse"&&StrokeSession.Distance(p,RotationHandle())*Zoom<=15){handle=handles.Length+1;rotationStart=EllipseRotation;PointF c=Centre();rotationPointerStart=(float)(Math.Atan2(p.Y-c.Y,p.X-c.X)*180/Math.PI);Capture=true;return;}if(Guide=="Ellipse"){var grips=GuideGrips();for(int i=0;i<grips.Length;i++)if(StrokeSession.Distance(p,RotateEllipse(grips[i],EllipseRotation))*Zoom<=13){handle=10+i;Capture=true;return;}}for(int i=0;i<=handles.Length;i++){if(Guide=="Ellipse"&&i<handles.Length)continue;PointF target=i==handles.Length?Centre():Guide=="Ellipse"?RotateEllipse(handles[i],EllipseRotation):handles[i];if(StrokeSession.Distance(p,target)*Zoom<=13){handle=i;dragOrigin=p;dragHandles=(PointF[])handles.Clone();if(!penInput)Capture=true;break;}}}
    void DragHandle(PointF p){
        if(Guide=="Ellipse"&&handle>=10){PointF c=Centre(),q=RotateEllipse(p,-EllipseRotation);float rx=Math.Abs(handles[1].X-handles[0].X)/2,ry=Math.Abs(handles[1].Y-handles[0].Y)/2;if(handle<12)rx=Math.Max(1,Math.Abs(q.X-c.X));else ry=Math.Max(1,Math.Abs(q.Y-c.Y));if((ModifierKeys&Keys.Shift)!=0)rx=ry=handle<12?rx:ry;handles[0]=new PointF(c.X-rx,c.Y-ry);handles[1]=new PointF(c.X+rx,c.Y+ry);RebuildGuide();Invalidate();return;}if(Guide=="Ellipse"&&handle==handles.Length+1){PointF c=Centre();EllipseRotation=rotationStart+(float)(Math.Atan2(p.Y-c.Y,p.X-c.X)*180/Math.PI)-rotationPointerStart;RebuildGuide();Invalidate();return;}if(handle==handles.Length){for(int i=0;i<handles.Length;i++)handles[i]=new PointF(dragHandles[i].X+p.X-dragOrigin.X,dragHandles[i].Y+p.Y-dragOrigin.Y);}
        else{if(Guide=="Ellipse")p=RotateEllipse(p,-EllipseRotation);if(Guide=="Ellipse"&&(ModifierKeys&Keys.Shift)!=0){PointF other=handles[1-handle];float side=Math.Max(Math.Abs(p.X-other.X),Math.Abs(p.Y-other.Y));p=new PointF(other.X+(p.X<other.X?-side:side),other.Y+(p.Y<other.Y?-side:side));}handles[handle]=p;}
        RebuildGuide();Invalidate();
    }
    void MoveStroke(PointF p){
        stroke.HardwarePressure=samplePressure;
        if(Guide=="Freehand"){if(ellipseStroke||arcStroke)stroke.Move(p);else smoother.Move(p);return;}
        int next=NearestGuide(p),delta=next-guideIndex;
        if(GuideClosed){int n=guidePoints.Length;if(delta>n/2)delta-=n;else if(delta< -n/2)delta+=n;}
        int direction=Math.Sign(delta);for(int i=0;i<Math.Abs(delta);i++){guideIndex=(guideIndex+direction+guidePoints.Length)%guidePoints.Length;stroke.Move(guidePoints[guideIndex]);}
    }
    void PaintGuides(Graphics g){if(Guide=="Freehand"||guidePoints==null)return;g.SmoothingMode=SmoothingMode.AntiAlias;
        using(Pen pen=new Pen(Color.FromArgb(EditGuide?140:65,0,135,174),1.1f)){pen.DashStyle=DashStyle.Dash;PointF[] screen=new PointF[guidePoints.Length];for(int i=0;i<screen.Length;i++)screen[i]=ScreenPoint(guidePoints[i]);if(GuideClosed)g.DrawPolygon(pen,screen);else g.DrawLines(pen,screen);
            if(EditGuide){if(Guide=="Ellipse"){PointF r=ScreenPoint(RotationHandle()),edge=ScreenPoint(RotateEllipse(new PointF(Centre().X,Math.Min(handles[0].Y,handles[1].Y)),EllipseRotation));g.DrawLine(pen,edge,r);g.FillEllipse(Brushes.White,r.X-12,r.Y-12,24,24);using(var iconPen=new Pen(Color.Teal,2)){g.DrawArc(iconPen,r.X-7,r.Y-7,14,14,30,290);g.DrawLine(iconPen,r.X+6,r.Y-4,r.X+9,r.Y-8);g.DrawLine(iconPen,r.X+6,r.Y-4,r.X+2,r.Y-7);}g.DrawString("Rotate",Font,Brushes.Teal,r.X+15,r.Y-8);}if(Guide=="Spline"){g.DrawLine(pen,ScreenPoint(handles[0]),ScreenPoint(handles[1]));g.DrawLine(pen,ScreenPoint(handles[2]),ScreenPoint(handles[3]));}
                var grips=GuideGrips();for(int i=0;i<=grips.Length;i++){PointF p=ScreenPoint(i==grips.Length?Centre():Guide=="Ellipse"?RotateEllipse(grips[i],EllipseRotation):grips[i]);g.FillEllipse(Brushes.White,p.X-6,p.Y-6,12,12);g.DrawEllipse(pen,p.X-6,p.Y-6,12,12);g.DrawString(i==grips.Length?"Move":Guide=="Spline"?(i==0?"Start":i==3?"End":"Curve"):Guide=="Ellipse"?"Resize":French?"Scale / rotate":"End",Font,Brushes.Teal,p.X+9,p.Y-18);}
            }
        }
    }

    public void TestGuideDrawing(){Doc=new Document("A4",100);Zoom=1;Offset=PointF.Empty;Tool="Pen";BrushSize=8;Smoothing=0;try{
        foreach(string kind in new[]{"Ruler","Ellipse","Spline"}){
            SelectGuide(kind);if(EditGuide)throw new Exception("Guides must default to drawing");
            using(Graphics g=Graphics.FromImage(Doc.Selected.Image))g.Clear(Color.Transparent);
            EditGuide=true;PointF old=Guide=="Ellipse"?GuideGrips()[0]:handles[0];OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,(int)old.X,(int)old.Y,0));OnMouseMove(new MouseEventArgs(MouseButtons.Left,0,(int)old.X+10,(int)old.Y+5,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,(int)old.X+10,(int)old.Y+5,0));if(handles[0].X<old.X+9)throw new Exception("Handle editing "+kind);
            PointF mid=guidePoints[guidePoints.Length/8];if(Doc.Selected.Image.GetPixel((int)mid.X,(int)mid.Y).A!=0)throw new Exception("Editing must not draw");
            EditGuide=false;PointF start=guidePoints[0],end=guidePoints[guidePoints.Length/4];Tool="Pen";BrushSize=8;
            OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,(int)start.X,(int)start.Y,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,(int)end.X,(int)end.Y,0));
            if(Doc.Selected.Image.GetPixel((int)mid.X,(int)mid.Y).A==0)throw new Exception("Guide drawing "+kind);
            PointF untouched=guidePoints[guidePoints.Length*3/4];if(Doc.Selected.Image.GetPixel((int)untouched.X,(int)untouched.Y).A!=0)throw new Exception("Guide draws only traversed segment");
            Tool="Eraser";BrushSize=14;OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,(int)end.X,(int)end.Y,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,(int)start.X,(int)start.Y,0));if(Doc.Selected.Image.GetPixel((int)mid.X,(int)mid.Y).A!=0)throw new Exception("Guide erasing "+kind);
        }
        // Cross the ellipse's sample seam using the short arc, in both directions.
        SelectGuide("Ellipse");using(Graphics g=Graphics.FromImage(Doc.Selected.Image))g.Clear(Color.Transparent);Tool="Pen";BrushSize=8;
        int n=guidePoints.Length;PointF seamStart=guidePoints[n-10],seamEnd=guidePoints[10],seam=guidePoints[0],opposite=guidePoints[n/2];
        OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,(int)seamStart.X,(int)seamStart.Y,0));OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,(int)seamEnd.X,(int)seamEnd.Y,0));
        if(Doc.Selected.Image.GetPixel((int)seam.X,(int)seam.Y).A==0||Doc.Selected.Image.GetPixel((int)opposite.X,(int)opposite.Y).A!=0)throw new Exception("Ellipse seam must take the short arc");
    }finally{EndStroke();Doc.Dispose();Doc=null;}}
}
}
