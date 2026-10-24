using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Aegis.Infrastructure;
namespace Aegis.Desktop;
/// <summary>Ligne d'interface pour un composant facultatif : taille, progression, annulation, erreurs lisibles.</summary>
public sealed class ComponentRow : INotifyPropertyChanged
{
    private bool installed,busy;
    private double progress;
    private string statusText="Non installé",error="";
    private CancellationTokenSource? cancellation;
    public ComponentDefinition Definition{get;}
    public event PropertyChangedEventHandler? PropertyChanged;
    public Command CancelCommand{get;}
    public AsyncCommand InstallCommand{get;}
    public ComponentRow(ComponentDefinition definition,Func<ComponentRow,Task> install,Action<Exception> onError)
    {
        Definition=definition;
        InstallCommand=new(()=>install(this),onError,()=>!installed&&!busy);
        CancelCommand=new(()=>cancellation?.Cancel(),()=>busy);
    }
    private void Changed([CallerMemberName]string? name=null)=>PropertyChanged?.Invoke(this,new(name));
    public string Title=>Definition.Title;
    public string Description=>Definition.Description;
    public string Details=>$"Téléchargement : {Definition.SizeText} · version {Definition.Version} · licence {Definition.License}";
    public bool IsInstalled{get=>installed;private set{installed=value;Changed();Changed(nameof(CanInstall));InstallCommand.Refresh();}}
    public bool IsBusy{get=>busy;private set{busy=value;Changed();Changed(nameof(CanInstall));InstallCommand.Refresh();CancelCommand.Refresh();}}
    public bool CanInstall=>!installed&&!busy;
    public double Progress{get=>progress;private set{progress=value;Changed();}}
    public string StatusText{get=>statusText;private set{statusText=value;Changed();}}
    public string Error{get=>error;private set{error=value;Changed();Changed(nameof(HasError));}}
    public bool HasError=>error.Length>0;
    public void SetInstalled(bool value)
    {
        IsInstalled=value;Error="";Progress=value?100:0;
        StatusText=value?"Installé":"Non installé";
    }
    /// <summary>Exécute l'installation avec progression et annulation ; ne laisse jamais une exception remonter à l'interface.</summary>
    public async Task RunAsync(CancellationToken lifetime,Func<IProgress<ComponentProgress>,CancellationToken,Task> work)
    {
        if(busy)return;
        using var source=CancellationTokenSource.CreateLinkedTokenSource(lifetime);cancellation=source;
        IsBusy=true;Error="";Progress=0;StatusText="Préparation…";
        var reporter=new Progress<ComponentProgress>(p=>
        {
            if(p.Fraction is double f)Progress=f*100;
            StatusText=p.Fraction is double g?$"{p.Phase} · {g:P0} ({ComponentDefinition.FormatSize(p.Done)} / {ComponentDefinition.FormatSize(p.Total)})":p.Phase;
        });
        try
        {
            await work(reporter,source.Token);
            SetInstalled(true);
        }
        catch(OperationCanceledException){Progress=0;StatusText="Annulé. Rien n'a été installé ; vous pouvez relancer.";}
        catch(ComponentException e){Progress=0;StatusText="Échec";Error=e.Message;}
        catch(Exception e) when(e is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {Progress=0;StatusText="Échec";Error=e.Message;}
        finally{IsBusy=false;cancellation=null;}
    }
}