using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
class Launcher : Form {
    readonly string root=AppDomain.CurrentDomain.BaseDirectory;
    readonly ComboBox version=new ComboBox {DropDownStyle=ComboBoxStyle.DropDownList};
    readonly Label status=new Label {AutoSize=true,ForeColor=Color.DimGray};
    Launcher() {
        Text="AutoHotInterceptor";ClientSize=new Size(620,380);MinimumSize=new Size(630,410);
        Font=new Font("Segoe UI",10);BackColor=Color.White;StartPosition=FormStartPosition.CenterScreen;
        var layout=new FlowLayoutPanel {Dock=DockStyle.Fill,Padding=new Padding(24),FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true};Controls.Add(layout);
        layout.Controls.Add(new Label {Text="AutoHotInterceptor",Font=new Font("Segoe UI",22,FontStyle.Bold),AutoSize=true,ForeColor=Color.FromArgb(25,75,160)});
        layout.Controls.Add(new Label {Text="AutoHotkey scripts with per-device keyboard and mouse input.",AutoSize=true,Margin=new Padding(0,8,0,18)});
        version.Items.AddRange(new object[]{"AutoHotkey v2 (recommended)","AutoHotkey v1"});version.SelectedIndex=0;version.Width=550;layout.Controls.Add(version);
        Button(layout,"Install / repair driver",()=>Run("driver/InterceptorSetup.exe","",true));
        Button(layout,"Open input monitor",()=>Script("Monitor.ahk"));
        Button(layout,"Run a script...",()=>{using(var dialog=new OpenFileDialog {Filter="AutoHotkey scripts|*.ahk",InitialDirectory=Path.Combine(root,Folder())})if(dialog.ShowDialog()==DialogResult.OK)Script(dialog.FileName);});
        Button(layout,"Examples and library",()=>Process.Start(new ProcessStartInfo(Path.Combine(root,Folder())){UseShellExecute=true}));
        Button(layout,"Uninstall driver",()=>Run("driver/InterceptorSetupConsole.exe","/uninstall",true));
        layout.Controls.Add(status);status.Text="Windows 10/11 x64. Demo: one hour per kernel boot.";
    }
    string Folder(){return version.SelectedIndex==0?"AHK v2":"AHK v1";}
    void Button(FlowLayoutPanel panel,string text,Action action){var b=new Button {Text=text,Width=550,Height=34,FlatStyle=FlatStyle.Flat,Cursor=Cursors.Hand,BackColor=Color.FromArgb(240,245,255),Margin=new Padding(0,6,0,0)};b.Click+=(s,e)=>{try{action();}catch(Exception error){MessageBox.Show(this,error.Message,"AutoHotInterceptor",MessageBoxButtons.OK,MessageBoxIcon.Error);}};panel.Controls.Add(b);}
    void Script(string path){if(!Path.IsPathRooted(path))path=Path.Combine(root,Folder(),path);PrepareLibraries();Run(version.SelectedIndex==0?"Runtime/v2/AutoHotkey64.exe":"Runtime/v1/AutoHotkeyU64.exe","\""+path+"\"",true,Path.GetDirectoryName(path));}
    [System.Runtime.InteropServices.DllImport("kernel32.dll",CharSet=System.Runtime.InteropServices.CharSet.Unicode)]static extern bool DeleteFile(string path);
    void PrepareLibraries(){foreach(var version in new[]{"AHK v1","AHK v2"})foreach(var relative in new[]{"Lib/AutoHotInterception.dll","Lib/x64/InterceptorBridge.dll","Lib/x86/InterceptorBridge.dll"})DeleteFile(Path.Combine(root,version,relative)+":Zone.Identifier");}
    void Run(string relative,string arguments,bool admin,string workingDirectory=null){var file=Path.Combine(root,relative);if(!File.Exists(file))throw new FileNotFoundException("Extract the complete release ZIP before starting.",file);Process.Start(new ProcessStartInfo(file,arguments){UseShellExecute=true,Verb=admin?"runas":"",WorkingDirectory=workingDirectory??Path.GetDirectoryName(file)});}
    [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new Launcher());}
}
