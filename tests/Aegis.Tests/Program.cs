using Aegis.Domain;
using Aegis.Application;
using Aegis.Infrastructure;
using System.Text.Json;
using System.Diagnostics;
using System.IO.Pipes;


int passed=0;
void Check(bool ok,string name){if(!ok)throw new Exception("FAIL: "+name);passed++;Console.WriteLine("PASS: "+name);}
Metric Cpu(double? value)=>new("cpu.load","CPU","CPU","%",value,"test",value.HasValue?MetricState.Available:MetricState.Missing);
Snapshot Sample(double time,double? cpu,double? ram=40)=>new(1,DateTimeOffset.UtcNow,time,[Cpu(cpu),new("ram.load","RAM","Mémoire","%",ram,"test",ram.HasValue?MetricState.Available:MetricState.Missing)]);
Session Session(params Snapshot[] samples)=>new(Guid.NewGuid(),"Unicode é & <script>",DateTimeOffset.UtcNow,DateTimeOffset.UtcNow,"<b>conditions</b>",samples,[]);
Check(Metrics.CpuPercent(20,60,40)==80,"CPU kernel includes idle");
Check(Metrics.CpuPercent(0,0,0) is null,"CPU warming absent");
Check(Metrics.CpuPercent(120,50,50) is null,"Invalid CPU counter rejected");
Check(Metrics.Rate(10,20,1) is null,"Network counter reset absent");
Check(Metrics.Rate(30,20,2)==5,"Network counter interval");
Check(Metrics.Rate(30,20,0) is null,"Network zero interval");
var a=SessionAnalysis.Summarize(Session(Sample(1,null),Sample(2,0),Sample(3,80)));
Check(a.CpuAverage==40,"Missing samples excluded, real zero included");
Check(a.DurationSeconds==2,"Monotonic session duration");
Check(a.MissingValues==1,"Missing values counted");
var empty=SessionAnalysis.Summarize(Session());Check(empty.CpuAverage is null&&empty.CpuPeak is null,"No fabricated empty average");
var high=SessionAnalysis.Summarize(Session(Sample(1,99,95)));Check(high.Findings.Any(f=>f.Id=="ram-high")&&high.Findings.Any(f=>f.Id=="cpu-high"),"Threshold observations backed by measurements");
var frames=FrameAnalysis.Calculate([10,10,20,double.NaN,-1,0]);Check(frames is {Frames:3}&&Math.Abs(frames.AverageFps-75)<.001&&frames.OnePercentLowFps==50,"Frame statistics filter invalid values and average durations");
Check(FrameAnalysis.Calculate([]) is null,"Absent frames remain absent");
var recorder=new SessionRecorder();recorder.Start("Game","Scene");recorder.Add(Sample(100,10));recorder.Mark("Lag");recorder.Add(Sample(105,20));var stopped=recorder.Stop();Check(stopped.Markers[0].Seconds==0&&stopped.Samples.Count==2,"Marker relative to session");recorder.Add(Sample(106,30));Check(recorder.Count==2,"Stopped recorder does not append");
recorder.Start("Long","Test");for(int i=0;i<28801;i++)recorder.Add(Sample(i,1));Check(recorder.LimitReached&&!recorder.Active&&recorder.Count==28800,"Session bound prevents unbounded memory");
var html=Reports.Html(Session(Sample(1,10)));Check(!html.Contains("<script>")&&html.Contains("&lt;script&gt;")&&!html.Contains("<b>conditions</b>"),"Report escapes annotations and titles");
var unsafeSession=Session(new Snapshot(1,DateTimeOffset.UtcNow,0,[new("x","=SUM(1)","malicious","%",10,"test",MetricState.Available)]));Check(Reports.Csv(unsafeSession).Contains("'=SUM(1)"),"CSV formula injection neutralized");
var root=Path.GetFullPath(args.FirstOrDefault()??"work/test-data");Directory.CreateDirectory(root);var store=new SessionStore(root);var original=Session(Sample(1,10),Sample(2,null));await store.SaveAsync(original);var loaded=await store.LoadAsync(original.Id);Check(loaded is not null&&loaded.Name==original.Name&&loaded.Samples.Count==2&&loaded.Samples[1].Metrics[0].Value is null,"SQLite round trip preserves unicode and missing measurements");await store.SaveAsync(original);Check((await store.ListAsync()).Count(s=>s.Id==original.Id)==1,"SQLite save is idempotent");
string frameFile=Path.Combine(root,"frames-fixture.csv");await File.WriteAllTextAsync(frameFile,"Application,SwapChainAddress,MsBetweenPresents\n\"game,one.exe\",0x1,10\n\"game,one.exe\",0x1,20\nother.exe,0x2,1\nother.exe,0x1,NA\n");var frameCapture=await PresentMonCapture.ImportAsync(frameFile,"game");Check(frameCapture.SwapChain=="0x1"&&frameCapture.Stats is {Frames:2}&&Math.Abs(frameCapture.Stats.AverageFps-1000/15d)<.001,"PresentMon parses quoted CSV and isolates dominant swapchain");
bool unsupported=false;try{await store.SaveAsync(original with{Schema=2});}catch(InvalidDataException){unsupported=true;}Check(unsupported,"Unsupported session schema rejected");
string corruptDir = Path.Combine(root, "corrupt-test-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(corruptDir);
await File.WriteAllTextAsync(Path.Combine(corruptDir, "sessions.db"), "NOT A SQLITE DATABASE GARBAGE DATA");
var recoveredStore = new SessionStore(corruptDir);
Check(recoveredStore.QuarantinedDatabase is not null && File.Exists(recoveredStore.QuarantinedDatabase), "Corrupt SQLite database quarantined without loss");
await recoveredStore.SaveAsync(original);
Check((await recoveredStore.ListAsync()).Count == 1, "Session store heals with new database after quarantine");
string etwDenied = PresentMonCapture.EtwMessage("Error: access denied to ETW trace session");
Check(etwDenied.Contains("ETW") && etwDenied.Contains("administrateur") && etwDenied.Contains("Aegis ne modifie pas"), "ETW access restriction provides clear diagnostic without modifying system");
if(args.Length>1)
{
    string name="aegis-"+Guid.NewGuid().ToString("N");await using var pipe=new NamedPipeServerStream(name,PipeDirection.In,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous|PipeOptions.CurrentUserOnly);
    var info=new ProcessStartInfo(args[1]){UseShellExecute=false,CreateNoWindow=true};info.ArgumentList.Add("--pipe");info.ArgumentList.Add(name);
    using var process=Process.Start(info)!;using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
    try{await pipe.WaitForConnectionAsync(timeout.Token);using var reader=new StreamReader(pipe);for(int i=0;i<3;i++){var text=await reader.ReadLineAsync(timeout.Token);var s=JsonSerializer.Deserialize<Snapshot>(text!);Check(s is not null&&s.Protocol==1&&s.Metrics.Count>0&&s.Metrics.All(m=>m.Value is null||double.IsFinite(m.Value.Value)),"Real collector snapshot "+i);if(i>0)Check(Metrics.Latest(s,"cpu.load") is >=0 and <=100,"Real CPU bounded "+i);}}
    finally{if(!process.HasExited){process.Kill(true);await process.WaitForExitAsync();}}
}
var threshold=new ThresholdMonitor();
Check(!threshold.Observe(96,95,1)&&!threshold.Observe(96,95,2)&&threshold.Observe(96,95,3),"Threshold requires three consecutive samples");
Check(!threshold.Observe(99,95,4)&&!threshold.Observe(96,95,5),"Threshold does not repeat while elevated");
threshold.Observe(null,95,6);
Check(!threshold.Observe(99,95,7)&&!threshold.Observe(99,95,8)&&threshold.Observe(99,95,9),"Missing readings reset consecutive threshold count");
threshold.Observe(20,95,10);
Check(!threshold.Observe(99,95,11)&&!threshold.Observe(99,95,20)&&!threshold.Observe(99,95,21)&&threshold.Observe(99,95,22),"Discontinuous monitoring cannot trigger a stale sustained alert");
Check(!threshold.Observe(double.NaN,95,23)&&!threshold.Observe(100,double.NaN,24),"Invalid threshold values ignored");
string readOnlyDir=Path.Combine(root,"readonly-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(readOnlyDir);
string readOnlyDb=Path.Combine(readOnlyDir,"sessions.db");
using(var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={readOnlyDb};Pooling=False"))
{c.Open();using var cmd=c.CreateCommand();cmd.CommandText="CREATE TABLE sessions(id TEXT PRIMARY KEY,name TEXT NOT NULL,started TEXT NOT NULL,data TEXT NOT NULL);INSERT INTO sessions VALUES('original','Preserved','2026-10-24','{}');";cmd.ExecuteNonQuery();}
File.SetAttributes(readOnlyDb,FileAttributes.ReadOnly);
try
{
    bool refused=false;try{_=new SessionStore(readOnlyDir);}catch(Microsoft.Data.Sqlite.SqliteException e){refused=e.SqliteErrorCode==8;}
    Check(refused&&File.Exists(readOnlyDb)&&!Directory.EnumerateFiles(readOnlyDir,"*.corrupt-*").Any(),"Healthy readonly database is never quarantined");
}
finally{File.SetAttributes(readOnlyDb,FileAttributes.Normal);}
using(var c=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={readOnlyDb};Pooling=False"))
{c.Open();using var cmd=c.CreateCommand();cmd.CommandText="SELECT count(*) FROM sessions WHERE id='original'";Check(Convert.ToInt64(cmd.ExecuteScalar())==1,"Readonly startup failure preserves original records");}
await UpdateTests.RunAsync(Check,root);
await DailyTests.RunAsync(Check,root);
GameTests.Run(Check);
Console.WriteLine($"{passed} checks passed.");
