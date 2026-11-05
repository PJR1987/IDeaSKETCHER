using System;using System.Drawing;using System.Linq;using System.Collections.Generic;using System.Windows.Forms;using System.IO;
namespace IDSketcher {
partial class VectorShape {
 const float EllipseK=.552284749831f;
 public PointF[] EllipsePointHandles(int index){PointF c,u,v;EllipseAxes(out c,out u,out v);PointF t=index==0?v:index==1?new PointF(-v.X,-v.Y):index==2?new PointF(-u.X,-u.Y):u;t=new PointF(t.X*EllipseK,t.Y*EllipseK);return new[]{new PointF(-t.X,-t.Y),t};}
 public void MakeEllipseEditable(){if(Kind!="Ellipse")return;var grips=Grips();var offsets=new[]{EllipsePointHandles(0),EllipsePointHandles(2),EllipsePointHandles(1),EllipsePointHandles(3)};Kind="Closed curve";Points=new List<PointF>{grips[0],grips[2],grips[1],grips[3]};Handles.Clear();for(int i=0;i<4;i++)Handles[i]=offsets[i];}
}
partial class Canvas {
 static int EllipseIndex(int index){return index==1?2:index==2?1:index;}
 void ConvertSelectedEllipse(){selectedVector.MakeEllipseEditable();var indices=selectedPoints.Select(EllipseIndex).ToArray();selectedPoints.Clear();foreach(int i in indices)selectedPoints.Add(i);if(vectorHandle>=0)vectorHandle=EllipseIndex(vectorHandle);if(vectorHandle==-4)curveHandlePoint=EllipseIndex(curveHandlePoint);}
 public List<VectorShape> FillSelection(){return selectedPaths.Where(p=>p.CanFill&&Doc!=null&&Doc.Selected.Vectors.Contains(p)).ToList();}
 public void TestEllipseHandleDrag(int index){SelectVectorPoint(index,false);var start=selectedVector.HandlePosition(index,1);var end=new PointF(start.X+20,start.Y+15);if(!PickPointHandle(start))throw new Exception("Ellipse handle not selectable");VectorMove(end);VectorUp(end);if(selectedVector.Kind!="Closed curve"||StrokeSession.Distance(selectedVector.HandlePosition(EllipseIndex(index),1),end)>.01)throw new Exception("Ellipse curve handle drag");}
}
partial class Studio {
 void AddFillControls(FlowLayoutPanel panel){foreach(string name in new[]{"Fill","No fill"}){string action=name;var b=Theme.Button(name,delegate{canvas.ApplyVectorFill(new VectorFillStyle{Mode=action=="Fill"?1:0,Start=canvas.Ink});canvas.Focus();});b.Width=170;panel.Controls.Add(b);}}

}
}