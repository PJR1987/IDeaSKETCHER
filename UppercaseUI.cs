using System;using System.Drawing;using System.Windows.Forms;using System.Collections.Generic;
namespace IDSketcher{
class UppercaseContextMenu:ContextMenuStrip{public UppercaseContextMenu(){UppercaseUI.Apply(this);}}
static class UppercaseUI{
 static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Control,object> watched=new System.Runtime.CompilerServices.ConditionalWeakTable<Control,object>();
 public static void Apply(Control c){
  if(c is TextBoxBase||c is NumericUpDown||c is Canvas)return;
  object marker;if(!watched.TryGetValue(c,out marker)){watched.Add(c,new object());
   var combo=c as ComboBox;if(combo!=null&&combo.DropDownStyle==ComboBoxStyle.DropDownList){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.DrawItem+=delegate(object sender,DrawItemEventArgs e){if(e.Index<0)return;e.DrawBackground();TextRenderer.DrawText(e.Graphics,Caption(Convert.ToString(combo.Items[e.Index])),combo.Font,e.Bounds,e.ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter);e.DrawFocusRectangle();};}
   else if(!(c is ListControl))c.TextChanged+=delegate{UpperText(c);};
   c.ControlAdded+=delegate(object sender,ControlEventArgs e){Apply(e.Control);};
   var strip=c as ToolStrip;if(strip!=null){foreach(ToolStripItem item in strip.Items)UpperItem(item);strip.ItemAdded+=delegate(object sender,ToolStripItemEventArgs e){UpperItem(e.Item);};}
  }
  if(!(c is ListControl))UpperText(c);
 }
 public static string Caption(string value){string text=value.ToUpperInvariant().Replace("PAINTBRUSH","BRUSH").Replace("VISIBLE FILL","VISIBLE").Replace("INK PEN","INK");return text.Replace("CURVATURE PEN","CURVE PEN").Replace("VECTOR PEN","PEN").Replace("SELECT / EDIT","SELECT").Replace("SELECT/EDIT","SELECT");}static void UpperText(Control c){if(string.Equals(c.Text,"IDeaSKETCHER",StringComparison.OrdinalIgnoreCase)){if(c.Text!="IDeaSKETCHER")c.Text="IDeaSKETCHER";return;}if(c is Studio){string title=c.Text;if(title.StartsWith("IDeaSKETCHER",StringComparison.OrdinalIgnoreCase)){string display="IDeaSKETCHER"+title.Substring(12).ToUpperInvariant();if(c.Text!=display)c.Text=display;}return;}string text=Caption(c.Text);if(c.Text!=text)c.Text=text;}
 static void UpperItem(ToolStripItem item){item.Text=Caption(item.Text);item.TextChanged+=delegate{string text=Caption(item.Text);if(item.Text!=text)item.Text=text;};var menu=item as ToolStripDropDownItem;if(menu!=null){foreach(ToolStripItem child in menu.DropDownItems)UpperItem(child);menu.DropDown.ItemAdded+=delegate(object sender,ToolStripItemEventArgs e){UpperItem(e.Item);};}}
}
partial class Studio{
 public void TestUppercaseUI(){
  previewing=true;Show();SetDocument(new Document("A4",100),null);if(canvas.Doc.Selected.Name!="LAYER 1")throw new Exception("Default layer uppercase");AddTypedLayer(true);if(!canvas.Doc.Selected.Name.StartsWith("VECTOR "))throw new Exception("Default vector uppercase");
  if(verticalMirror.Text!="VERTICAL"||horizontalMirror.Text!="HORIZONTAL")throw new Exception("Mirror captions");
  if(toolButtons["Select"].Text!="SELECT")throw new Exception("Tool uppercase");if(mode.DrawMode!=DrawMode.OwnerDrawFixed)throw new Exception("Dropdown display uppercase");dirty=false;
 }
}
}
