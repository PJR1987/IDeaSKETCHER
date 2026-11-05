using System;
using System.Drawing;
using System.Windows.Forms;

namespace IDSketcher {
class BrushPopup : ToolStripDropDown {
    public readonly NumericUpDown SizeValue=new NumericUpDown(),OpacityValue=new NumericUpDown(),FlowValue=new NumericUpDown(),HardnessValue=new NumericUpDown();
    public readonly TrackBar SizeSlider=new TrackBar(),OpacitySlider=new TrackBar(),FlowSlider=new TrackBar(),HardnessSlider=new TrackBar();
    public readonly Panel Content=new Panel();
    public Action Changed;
    readonly ToolTip tips=new ToolTip();
    public void AddFavourites(System.Collections.Generic.List<Favourite> favourites,Action<int> pin,Action<Favourite> choose,Action<Favourite> remove){
        Content.Height=451;((ToolStripControlHost)Items[0]).Size=Content.Size;
        var title=new Label{Text="FAVOURITES · 24 slots",Left=10,Top=252,Width=220,Height=23};Content.Controls.Add(title);
        var add=Theme.Button("Pin current tool",delegate{pin(-1);});add.SetBounds(357,246,172,28);Content.Controls.Add(add);
        Action refresh=null;var buttons=new FavouriteButton[24];
        refresh=delegate{for(int i=0;i<24;i++){int slot=i;var f=favourites.Find(x=>x.Slot==slot);buttons[i].Favourite=f;tips.SetToolTip(buttons[i],f==null?"Pin current tool in slot "+(i+1):f.Preset.Tool+" "+(i+1)+" · "+f.Preset.Size+" px · opacity "+f.Preset.Opacity+"% · flow "+f.Preset.Flow+"% · hardness "+f.Preset.Hardness+"%\nClick to use · right-click to remove");buttons[i].Invalidate();}add.Enabled=favourites.Count<24;};
        for(int i=0;i<24;i++){int slot=i;var b=new FavouriteButton{Slot=i,FlatStyle=FlatStyle.Flat};b.SetBounds(10+i%8*65,281+i/8*47,60,42);b.Click+=delegate{if(b.Favourite==null){pin(slot);refresh();}else{var f=b.Favourite;Close();choose(f);}};b.MouseUp+=delegate(object sender,MouseEventArgs e){if(e.Button==MouseButtons.Right&&b.Favourite!=null){var menu=new UppercaseContextMenu{Font=new System.Drawing.Font("Segoe UI",9,System.Drawing.FontStyle.Bold)};menu.Items.Add("Remove favourite "+(slot+1),null,delegate{remove(b.Favourite);refresh();});menu.Closed+=delegate{menu.Dispose();};menu.Show(b,e.Location);}};Content.Controls.Add(b);buttons[i]=b;}
        add.Click+=delegate{refresh();};Theme.Apply(Content);refresh();
    }
    protected override void Dispose(bool disposing){if(disposing)tips.Dispose();base.Dispose(disposing);}
    public BrushPopup(int size,int opacity,int flow,int hardness,int minSize=1,int maxSize=500){
        AutoClose=true;Padding=new Padding(6);Content.Size=new Size(540,246);Content.Font=new Font("Segoe UI",9);
        AddSlider("SIZE · PX",SizeValue,SizeSlider,minSize,maxSize,size,8);
        AddSlider("OPACITY · %",OpacityValue,OpacitySlider,0,100,opacity,110);
        AddSlider("FLOW · %",FlowValue,FlowSlider,0,100,flow,161);
        AddSlider("HARDNESS · %",HardnessValue,HardnessSlider,0,100,hardness,59);
        
        Theme.Apply(Content);BackColor=Theme.Panel;
        ToolStripControlHost host=new ToolStripControlHost(Content){AutoSize=false,Size=Content.Size,Margin=Padding.Empty,Padding=Padding.Empty};Items.Add(host);
    }
    void AddSlider(string title,NumericUpDown number,TrackBar slider,int min,int max,int current,int y){
        var label=new Label{Text=title+" · "+current,Left=10,Top=y,Width=520,Height=20};Content.Controls.Add(label);
        slider.Orientation=Orientation.Horizontal;slider.AutoSize=false;slider.SetBounds(10,y+20,520,28);slider.TickStyle=TickStyle.None;slider.Minimum=min;slider.Maximum=max;slider.Value=current;Content.Controls.Add(slider);
        number.SetBounds(415,y+18,115,28);number.Minimum=min;number.Maximum=max;number.Value=current;
        number.ValueChanged+=delegate{label.Text=title+" · "+number.Value;slider.Value=(int)number.Value;if(Changed!=null)Changed();};slider.ValueChanged+=delegate{number.Value=slider.Value;};
    }
}
}
