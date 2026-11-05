using System;using System.Drawing;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.IO;using System.Linq;using System.Collections.Generic;
namespace IDSketcher {
class VectorFillStyle {
 public int Mode;public Color Start=Color.SteelBlue;
 public VectorFillStyle Copy(){return new VectorFillStyle{Mode=Mode,Start=Start};}
 public Brush Brush(GraphicsPath path,float opacity){return new SolidBrush(Color.FromArgb((int)(Start.A*opacity),Start));}
 // Retain the existing file record size so older projects remain readable.
 public void Write(BinaryWriter w){w.Write(Mode);w.Write(Start.ToArgb());w.Write(Color.White.ToArgb());w.Write(0f);}
 public static VectorFillStyle Read(BinaryReader r){int mode=r.ReadInt32();Color colour=Color.FromArgb(r.ReadInt32());r.ReadInt32();float reserved=r.ReadSingle();if(mode<0||mode>3||float.IsNaN(reserved)||reserved<0||reserved>360)throw new InvalidDataException("Invalid vector fill");return new VectorFillStyle{Mode=mode==0?0:1,Start=colour};}
}
partial class VectorShape {
 public VectorFillStyle Fill=new VectorFillStyle();
 public bool CanFill{get{return Kind=="Compound"||Kind=="Ellipse"||Kind=="Polygon"||Kind=="Closed curve";}}
 public bool HasFill{get{return Filled||CanFill&&Fill.Mode!=0;}}
}
partial class Canvas {
 public VectorFillStyle VectorFill=new VectorFillStyle();
 public void ApplyVectorFill(VectorFillStyle fill){if(Doc==null||!Doc.Selected.IsVector||Doc.Selected.IsLocked)return;VectorFill=fill.Copy();var targets=selectedPaths.Where(s=>s.CanFill&&Doc.Selected.Vectors.Contains(s)).ToList();if(targets.Count==0)return;if(BeforeStroke!=null)BeforeStroke();foreach(var shape in targets){shape.Filled=false;shape.Fill=fill.Copy();}Doc.Selected.InvalidateVectors();if(AfterStroke!=null)AfterStroke();Invalidate();}
}

}