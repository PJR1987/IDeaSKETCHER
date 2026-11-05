using System;using System.IO;using System.ComponentModel;using System.Drawing;using System.Windows.Forms;
namespace IDSketcher {
static class GlbImport {
 internal static GlbModel Load(string path,Func<bool> cancelled){
  var file=new FileInfo(path);if(!file.Exists||file.Length>200000000)throw new InvalidDataException("Choose a model smaller than 200 MB.");
  if(!string.Equals(Path.GetExtension(path),".glb",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Choose a GLB model.");
  if(cancelled())throw new OperationCanceledException();var model=GlbModel.Load(File.ReadAllBytes(path));if(cancelled())throw new OperationCanceledException();return model;
 }
}
class ModelImportDialog:Form {
 readonly BackgroundWorker worker=new BackgroundWorker{WorkerSupportsCancellation=true};readonly Label message=new Label();readonly Button cancel;bool finished;public GlbModel Model;public Exception Error;
 public ModelImportDialog(string path){Text="Import 3D model";Theme.Form(this);ClientSize=new Size(480,145);FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=MinimizeBox=false;StartPosition=FormStartPosition.CenterParent;
  message.Text="Opening model...";message.SetBounds(18,16,444,42);Controls.Add(message);var bar=new ProgressBar{Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=30};bar.SetBounds(18,63,444,18);Controls.Add(bar);

  cancel=Theme.Button("Cancel",delegate{RequestCancel();});cancel.SetBounds(352,95,110,32);Controls.Add(cancel);Theme.Apply(this);
  worker.DoWork+=delegate(object sender,DoWorkEventArgs e){try{e.Result=GlbImport.Load(path,()=>worker.CancellationPending);}catch(OperationCanceledException){e.Cancel=true;}};
  worker.RunWorkerCompleted+=delegate(object sender,RunWorkerCompletedEventArgs e){finished=true;Error=e.Error;if(!e.Cancelled&&e.Error==null&&!worker.CancellationPending)Model=(GlbModel)e.Result;DialogResult=Model!=null?DialogResult.OK:DialogResult.Cancel;Close();};
  Shown+=delegate{worker.RunWorkerAsync();};FormClosing+=delegate(object sender,FormClosingEventArgs e){if(!finished){e.Cancel=true;RequestCancel();}};
 }
 void RequestCancel(){worker.CancelAsync();cancel.Enabled=false;message.Text="Cancelling import...";}
 protected override void Dispose(bool disposing){if(disposing)worker.Dispose();base.Dispose(disposing);}
}
partial class Studio {
 void ImportGLB(){if(canvas.Doc==null)return;if(canvas.Doc.Layers.Count>=32){MessageBox.Show(this,"This version supports up to 32 layers.");return;}canvas.EndStroke();
  using(var dialog=new OpenFileDialog{Title="Import GLB",Filter="Binary glTF (*.glb)|*.glb",CheckFileExists=true})if(dialog.ShowDialog(this)==DialogResult.OK){using(var importing=new ModelImportDialog(dialog.FileName)){if(importing.ShowDialog(this)==DialogResult.OK)AddThreeDModel(importing.Model,Path.GetFileNameWithoutExtension(dialog.FileName));else if(importing.Error!=null)MessageBox.Show(this,"The model could not be imported.\n\n"+importing.Error.Message,"Import 3D model",MessageBoxButtons.OK,MessageBoxIcon.Information);}}
 }
}
}
