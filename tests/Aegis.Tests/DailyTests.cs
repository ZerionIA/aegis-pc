using Aegis.Domain;
using Aegis.Infrastructure;
using System.Text.Json;

public static class DailyTests
{
    public static async Task RunAsync(Action<bool,string> check,string root)
    {
        string command=StartupRegistration.Command(@"C:\Program Files\Aegis\Aegis.exe");
        check(command=="\"C:\\Program Files\\Aegis\\Aegis.exe\" --background","Startup command quotes executable paths with spaces");
        bool rejected=false;try{StartupRegistration.Command("Aegis.exe");}catch(ArgumentException){rejected=true;}
        check(rejected,"Relative startup executable rejected");
        rejected=false;try{StartupRegistration.Command("C:\\bad\"name.exe");}catch(ArgumentException){rejected=true;}
        check(rejected,"Quoted startup path cannot inject arguments");
        var privateSnapshot=new Snapshot(1,DateTimeOffset.UtcNow,1,[new("PRIVATE-ID","PRIVATE-DEVICE","PRIVATE-NAME","%",12345,"PRIVATE-PATH",MetricState.Available)]);
        string report=SupportReport.Create("0.4.0-beta.1",privateSnapshot);
        check(!report.Contains("PRIVATE-")&&!report.Contains("12345"),"Support report excludes sensor identifiers, names, paths and readings");
        using(var json=JsonDocument.Parse(report))check(json.RootElement.GetProperty("sensorCount").GetInt32()==1&&json.RootElement.GetProperty("available").GetInt32()==1,"Support report retains useful aggregate availability");
        string directory=Path.Combine(root,"daily-"+Guid.NewGuid().ToString("N"));var store=new SessionStore(directory);
        var interrupted=new Session(Guid.NewGuid(),"Interrupted",DateTimeOffset.UtcNow,null,"",[privateSnapshot],[]);
        await store.SaveAsync(interrupted);var completed=interrupted with{Id=Guid.NewGuid(),Name="Complete",Ended=DateTimeOffset.UtcNow};await store.SaveAsync(completed);
        var list=await store.ListAsync();
        check(list.Single(s=>s.Id==interrupted.Id).Ended is null,"Library preserves incomplete checkpoint state");
        check(list.Single(s=>s.Id==completed.Id).Ended is not null,"Completed sessions are not mislabeled as interrupted");
        check((await store.LoadAsync(interrupted.Id))?.Samples.Count==1,"Interrupted checkpoint can be reloaded with measurements");
    }
}
