using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Forms=System.Windows.Forms;
namespace Aegis.Desktop;

public sealed class DailyCompanion:IDisposable
{
    private readonly Window window;
    private readonly MainViewModel model;
    private readonly Forms.NotifyIcon tray;
    private readonly Forms.ContextMenuStrip menu=new();
    private readonly Forms.ToolStripMenuItem record=new(),marker=new("Marquer une saccade · Ctrl+Alt+F8");
    private readonly System.Drawing.Icon icon;
    private HwndSource? source;
    private bool registered,disposed;
    private const int HotkeyId=0x4147;
    public DailyCompanion(Window window,MainViewModel model)
    {
        this.window=window;this.model=model;
        using(var resource=System.Windows.Application.GetResourceStream(new Uri("/Assets/aegis.ico",UriKind.Relative))!.Stream)
        using(var original=new System.Drawing.Icon(resource))icon=(System.Drawing.Icon)original.Clone();
        tray=new Forms.NotifyIcon{Icon=icon,Text="Aegis — Zerionia",ContextMenuStrip=menu,Visible=true};
        menu.Items.Add("Ouvrir Aegis",null,(_,_)=>Restore());
        menu.Items.Add(record);record.Click+=(_,_)=>model.RecordCommand.Execute(null);
        menu.Items.Add(marker);marker.Click+=(_,_)=>Mark();
        menu.Items.Add("Panneau compact",null,(_,_)=>model.WidgetCommand.Execute(null));
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("Quitter et conserver la session",null,(_,_)=>window.Close());
        menu.Opening+=(_,_)=>{record.Text=model.RecordLabel;record.Enabled=model.RecordCommand.CanExecute(null);marker.Enabled=model.MarkerCommand.CanExecute(null);};
        tray.DoubleClick+=(_,_)=>Restore();model.PropertyChanged+=OnChanged;
    }
    public bool TrayVisible=>tray.Visible;
    internal void SmokeDispatchMarker(){bool handled=false;Hook(source!.Handle,0x0312,(IntPtr)HotkeyId,IntPtr.Zero,ref handled);if(!handled)throw new InvalidOperationException("Hotkey route failed");}
    public void Attach()
    {
        source=HwndSource.FromHwnd(new WindowInteropHelper(window).Handle);source?.AddHook(Hook);ApplyHotkey();
    }
    public void Restore(){if(disposed)return;window.Show();window.WindowState=WindowState.Normal;window.Activate();}
    public void Hide()=>window.Hide();
    private void Mark(){if(model.MarkerCommand.CanExecute(null))model.MarkerCommand.Execute(null);}
    private void OnChanged(object? sender,PropertyChangedEventArgs e)
    {
        if(e.PropertyName==nameof(model.HotkeyEnabled))ApplyHotkey();
        if(e.PropertyName is nameof(model.Cpu) or nameof(model.Memory))
        {string text="Aegis | CPU "+model.Cpu+" | RAM "+model.Memory;tray.Text=text[..Math.Min(text.Length,63)];}
    }
    private void ApplyHotkey()
    {
        if(source is null)return;
        if(registered){UnregisterHotKey(source.Handle,HotkeyId);registered=false;}
        if(model.HotkeyEnabled)registered=RegisterHotKey(source.Handle,HotkeyId,0x4003,0x77);
        model.HotkeyStatus=!model.HotkeyEnabled?"Raccourci désactivé.":registered?"Ctrl+Alt+F8 prêt pendant les sessions.":"Ctrl+Alt+F8 est déjà utilisé ou indisponible. Les marqueurs restent accessibles dans l’app et le menu de l’icône.";
    }
    private IntPtr Hook(IntPtr hwnd,int message,IntPtr wParam,IntPtr lParam,ref bool handled)
    {
        if(message==0x0312&&wParam.ToInt32()==HotkeyId){Mark();handled=true;}
        return IntPtr.Zero;
    }
    public void Dispose()
    {
        if(disposed)return;disposed=true;model.PropertyChanged-=OnChanged;
        if(source is not null){if(registered)UnregisterHotKey(source.Handle,HotkeyId);source.RemoveHook(Hook);}
        tray.Visible=false;tray.Dispose();menu.Dispose();icon.Dispose();
    }
    [DllImport("user32.dll",SetLastError=true)] [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr window,int id,uint modifiers,uint key);
    [DllImport("user32.dll")] [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr window,int id);
}
