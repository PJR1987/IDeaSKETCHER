using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace IDSketcher {
static class NativePen {
    [DllImport("user32.dll")] public static extern bool PhysicalToLogicalPointForPerMonitorDPI(IntPtr window,ref Point point);
    [DllImport("user32.dll")] public static extern bool LogicalToPhysicalPointForPerMonitorDPI(IntPtr window,ref Point point);
    [DllImport("user32.dll")] public static extern IntPtr SetCursor(IntPtr cursor);
    [DllImport("user32.dll")] public static extern IntPtr GetMessageExtraInfo();
    [DllImport("user32.dll")] public static extern bool GetPointerType(uint id,out uint type);
    [DllImport("user32.dll")] public static extern bool GetPointerPenInfo(uint id,out PenInfo info);
    [DllImport("user32.dll")] public static extern bool GetPointerPenInfoHistory(uint id,ref uint count,[Out] PenInfo[] history);
    [StructLayout(LayoutKind.Sequential)] public struct PointerInfo {
        public uint Type,Id,Frame,Flags;
        public IntPtr Device,Target;
        public Point Pixel,Himetric,PixelRaw,HimetricRaw;
        public uint Time,History;
        public int InputData;
        public uint KeyStates;
        public ulong PerformanceCount;
        public uint ButtonChange;
    }
    [StructLayout(LayoutKind.Sequential)] public struct PenInfo {
        public PointerInfo Pointer;
        public uint Flags,Mask,Pressure,Rotation;
        public int TiltX,TiltY;
    }
    public static float Normalize(uint pressure){return Math.Max(0,Math.Min(1,pressure/1024f));}
}
partial class Canvas {
    uint activePen;
    long lastPenTick;
    protected override void WndProc(ref Message m){
        // Hide the native cursor only over this canvas; the brush ring is drawn
        // at canvas scale without changing the system's cursor visibility count.
        if(m.Msg==0x20&&(m.LParam.ToInt64()&0xffff)==1&&HideCanvasCursor){NativePen.SetCursor(IntPtr.Zero);m.Result=(IntPtr)1;return;}if(m.Msg==0x20&&(m.LParam.ToInt64()&0xffff)==1&&Doc!=null&&(Doc.Selected.IsVector||Doc.Selected.Is3D)){NativePen.SetCursor(Cursor.Handle);m.Result=(IntPtr)1;return;}
        if(m.Msg==0x20&&(m.LParam.ToInt64()&0xffff)==1&&SelectionMode=="Draw"&&TransformMode==null&&Tool!="Select"&&!FillTool&&!EyedropperActive&&BrushShapeCursor&&!panning&&!(Guide!="Freehand"&&EditGuide)){NativePen.SetCursor(IntPtr.Zero);m.Result=(IntPtr)1;return;}
        if(m.Msg>=0x200&&m.Msg<=0x209){
            long extra=NativePen.GetMessageExtraInfo().ToInt64();if((extra&0xffffff00L)==0xff515700L&&DateTime.UtcNow.Ticks-lastPenTick<TimeSpan.TicksPerSecond/2){m.Result=IntPtr.Zero;return;}
            if(activePen!=0){m.Result=IntPtr.Zero;return;}
            penInput=false;samplePressure=1;
        }
        if(m.Msg==0x246||m.Msg==0x245||m.Msg==0x247||m.Msg==0x24a||m.Msg==0x24c){
            uint id=(uint)(m.WParam.ToInt64()&0xffff),type;
            if(id==activePen&&activePen!=0&&m.Msg==0x24c){EndStroke();activePen=0;penInput=false;m.Result=IntPtr.Zero;return;}
            if(NativePen.GetPointerType(id,out type)&&type==3){
                lastPenTick=DateTime.UtcNow.Ticks;
                if(m.Msg==0x24a){inside=false;Invalidate();m.Result=IntPtr.Zero;return;}
                NativePen.PenInfo info;
                if(NativePen.GetPointerPenInfo(id,out info)){
                    if((info.Pointer.Flags&0x8000)!=0){EndStroke();activePen=0;penInput=false;}
                    else if(m.Msg==0x245&&activePen==id&&info.Pointer.History>1){
                        uint count=Math.Min(256,info.Pointer.History);NativePen.PenInfo[] history=new NativePen.PenInfo[count];
                        if(NativePen.GetPointerPenInfoHistory(id,ref count,history)){for(int i=(int)count-1;i>=0;i--)DispatchPen(history[i],0x245);}else DispatchPen(info,m.Msg);
                    }else DispatchPen(info,m.Msg);
                }else if(id==activePen&&(m.Msg==0x247||m.Msg==0x24c)){EndStroke();activePen=0;penInput=false;}
                m.Result=IntPtr.Zero;return;
            }
        }
        base.WndProc(ref m);
    }
    void DispatchPen(NativePen.PenInfo info,int message){
        if(activePen!=0&&activePen!=info.Pointer.Id)return;
        if(activePen==0&&stroke!=null&&message==0x245)return;
        Point screen=info.Pointer.Pixel;
        NativePen.PhysicalToLogicalPointForPerMonitorDPI(Handle,ref screen);
        Point local=PointToClient(screen);
        bool valid=(info.Mask&1)!=0;
        penInput=true;
        // A pen-up report can have zero pressure because contact has ended.
        // Keep the last contact pressure for the final positional sample.
        if(message!=0x247)samplePressure=valid?NativePen.Normalize(info.Pressure):1;
        string state=valid?"Pen pressure: "+Math.Round(NativePen.Normalize(info.Pressure)*100)+"%":"Pen detected · no pressure data";
        if(PenStatus!=state){PenStatus=state;if(PenStatusChanged!=null)PenStatusChanged();}
        if(message==0x246){activePen=info.Pointer.Id;OnMouseDown(new MouseEventArgs(MouseButtons.Left,1,local.X,local.Y,0));}
        else if(message==0x247){if(activePen==info.Pointer.Id)OnMouseUp(new MouseEventArgs(MouseButtons.Left,1,local.X,local.Y,0));activePen=0;penInput=false;samplePressure=1;}
        else OnMouseMove(new MouseEventArgs(activePen==info.Pointer.Id?MouseButtons.Left:MouseButtons.None,0,local.X,local.Y,0));
    }
    public void TestPenPackets(){
        Doc=new Document("A4",100);Zoom=1;Offset=PointF.Empty;Tool="Pen";BrushSize=30;Smoothing=0;PressureMode="Wacom pressure";
        try{
            NativePen.PenInfo sample=new NativePen.PenInfo();sample.Pointer.Id=99;sample.Pointer.Type=3;sample.Mask=1;sample.Pressure=102;sample.Pointer.Pixel=TestPhysicalPoint(30,100);DispatchPen(sample,0x246);
            sample.Pointer.Pixel=TestPhysicalPoint(70,100);DispatchPen(sample,0x245);
            sample.Pressure=1024;sample.Pointer.Pixel=TestPhysicalPoint(150,100);DispatchPen(sample,0x245);
            sample.Pointer.Pixel=TestPhysicalPoint(210,100);DispatchPen(sample,0x245);DispatchPen(sample,0x247);
            if(Doc.Selected.Image.GetPixel(50,110).A!=0||Doc.Selected.Image.GetPixel(190,110).A==0)throw new Exception("Pen packets must change rendered width");
            if(activePen!=0||stroke!=null)throw new Exception("Pen-up must finish stroke");
        }finally{EndStroke();Doc.Dispose();Doc=null;}
    }
    Point TestPhysicalPoint(int x,int y){Point p=PointToScreen(new Point(x,y));NativePen.LogicalToPhysicalPointForPerMonitorDPI(Handle,ref p);return p;}
}
}
