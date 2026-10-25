using System.Windows.Input;
namespace Aegis.Desktop;
public sealed class Command(Action execute, Func<bool>? allowed=null) : ICommand
{
    public bool CanExecute(object? p)=>allowed?.Invoke()??true;
    public void Execute(object? p)=>execute();
    public event EventHandler? CanExecuteChanged;
    public void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}
public sealed class AsyncCommand(Func<Task> execute,Action<Exception> onError,Func<bool>? allowed=null) : ICommand
{
    private bool busy;
    public bool CanExecute(object? p)=>!busy&&(allowed?.Invoke()??true);
    public async void Execute(object? p) { if(!CanExecute(p))return; busy=true; Refresh(); try { await execute(); }catch(Exception e){onError(e);}finally{busy=false;Refresh();} }
    public event EventHandler? CanExecuteChanged;
    public void Refresh()=>CanExecuteChanged?.Invoke(this,EventArgs.Empty);
}

public sealed class ParameterCommand(Action<object?> execute) : ICommand { public bool CanExecute(object? value)=>true;public void Execute(object? value)=>execute(value);public event EventHandler? CanExecuteChanged{add{}remove{}} }
