using System;using System.Drawing;using System.Runtime.InteropServices;using System.Runtime.CompilerServices;using System.Windows.Forms;
namespace IDSketcher{
static class SharpDpi{
 class State:NativeWindow{
  public Form Form;public float Factor=1;public bool Scaling,RefreshQueued;
  public void ScaleTo(float factor){if(Scaling||Math.Abs(Factor-factor)<.001)return;float change=factor/Factor;Factor=factor;Scaling=true;Form.SuspendLayout();try{Form.Scale(new SizeF(change,change));SetLayerRows(Form,factor);}finally{Form.ResumeLayout(true);Scaling=false;}Complete();QueueComplete();}
  public void Complete(){var studio=Form as Studio;if(studio!=null&&!Form.IsDisposed)studio.CompleteDpiLayout();}
  public void QueueComplete(){if(RefreshQueued||!Form.IsHandleCreated||Form.IsDisposed)return;RefreshQueued=true;Form.BeginInvoke((Action)delegate{RefreshQueued=false;Complete();});}
  protected override void WndProc(ref Message m){if(m.Msg==0x02E0){float factor=(m.WParam.ToInt64()&65535)/96f;ScaleTo(factor);}base.WndProc(ref m);}
 }
 static void SetLayerRows(Control c,float factor){var list=c as LayerList;if(list!=null)list.ItemHeight=(int)Math.Round(40*factor);foreach(Control child in c.Controls)SetLayerRows(child,factor);}static readonly ConditionalWeakTable<Form,State> states=new ConditionalWeakTable<Form,State>();
 public static void Configure(Form f){var state=new State{Form=f};states.Add(f,state);f.HandleCreated+=delegate{state.AssignHandle(f.Handle);};f.HandleDestroyed+=delegate{state.ReleaseHandle();};f.Load+=delegate{using(var g=f.CreateGraphics())state.ScaleTo(g.DpiX/96f);state.Complete();};f.Shown+=delegate{state.QueueComplete();};}
 public static bool IsScaling(Control c){Form f=c.FindForm();State state;return f!=null&&states.TryGetValue(f,out state)&&state.Scaling;}
 public static void TestScale(Form f,float factor){State state;if(states.TryGetValue(f,out state))state.ScaleTo(factor);}
 public static float Factor(Control c){Form f=c.FindForm();State state;return f!=null&&states.TryGetValue(f,out state)?state.Factor:1;}
 public static int Px(Control c,int value){return (int)Math.Round(value*Factor(c));}
 public static void Bounds(Control c,int x,int y,int w,int h){c.SetBounds(Px(c,x),Px(c,y),Px(c,w),Px(c,h));}
}
}
