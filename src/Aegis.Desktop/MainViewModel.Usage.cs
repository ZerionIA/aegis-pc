using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Threading;
using Aegis.Infrastructure;
namespace Aegis.Desktop;
public sealed partial class MainViewModel
{
    private UsageReporting usage=null!;
    private readonly HttpClient usageHttp=new();
    private readonly DispatcherTimer usageTimer=new(){Interval=UsageReporting.Interval};
    public bool UsageConsentPending=>!usage.IsInternal&&usage.Consent is null;
    public string UsageState=>L.Current[usage.IsInternal?"usage.internal":usage.Consent==true?"usage.enabled":usage.DeletionPending?"usage.deleting":"usage.disabled"];
    public AsyncCommand AllowUsageCommand{get;private set;}=null!;
    public AsyncCommand DeclineUsageCommand{get;private set;}=null!;
    public Command FeedbackCommand{get;private set;}=null!;
    public Command UsagePrivacyCommand{get;private set;}=null!;
    private void InitializeUsage()
    {
        var args=Environment.GetCommandLineArgs();
        bool test=!File.Exists(Path.Combine(AppContext.BaseDirectory,"Uninstall.exe"))||args.Any(a=>a.StartsWith("--smoke",StringComparison.Ordinal)||a=="--data-dir"||a=="--no-usage-reporting")
            ||Environment.GetEnvironmentVariable("AEGIS_DISABLE_USAGE_REPORTING")=="1";
#if DEBUG
        test=true;
#endif
        usage=new(data,VersionText,usageHttp,test);
        async Task Choose(bool enabled)
        {
            await usage.SetConsentAsync(enabled);
            Changed(nameof(UsageConsentPending));Changed(nameof(UsageState));
        }
        AllowUsageCommand=new(()=>Choose(true),Error);
        DeclineUsageCommand=new(()=>Choose(false),Error);
        FeedbackCommand=new(()=>Process.Start(new ProcessStartInfo("https://zerionia.com/apps/aegis#feedback"){UseShellExecute=true}));
        UsagePrivacyCommand=new(()=>Process.Start(new ProcessStartInfo("https://zerionia.com/apps/aegis#usage-privacy"){UseShellExecute=true}));
        usageTimer.Tick+=async(_,_)=>{await usage.TickAsync();Changed(nameof(UsageState));};
        usageTimer.Start();
    }
}
