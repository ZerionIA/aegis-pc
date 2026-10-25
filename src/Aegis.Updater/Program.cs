using System.Diagnostics;
using Aegis.Infrastructure;
using Aegis.Domain;
using System.Reflection;

string data="";
try
{
    if(args.Length!=4||!int.TryParse(args[0],out int pid))return 2;
    string version=args[1];data=Path.GetFullPath(args[2]);
    var current=Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];
    if(ReleaseVersion.Parse(version) is not {} next||ReleaseVersion.Parse(current) is not {} previous||next.CompareTo(previous)<=0)throw new InvalidDataException("Mise à jour ancienne ou de même version refusée.");
    string app=Path.GetFullPath(args[3]);
    if(!string.Equals(Path.GetFileName(app),"Aegis.exe",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Application de départ inattendue.");
    using var http=new HttpClient{Timeout=TimeSpan.FromSeconds(30)};
    var client=new ReleaseUpdateClient(http);using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(3));
    var release=await client.FindAsync(version,timeout.Token,exact:true)??throw new InvalidDataException("Release GitHub introuvable.");
    string installer=Path.Combine(data,"updates",release.FileName);
    if(!await ReleaseUpdateClient.VerifyAsync(installer,release,timeout.Token))throw new InvalidDataException("Installateur invalide.");
    try{using var parent=Process.GetProcessById(pid);if(!string.Equals(parent.MainModule?.FileName,app,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("Processus Aegis inattendu.");await parent.WaitForExitAsync(timeout.Token);}catch(ArgumentException){}
    if(!await ReleaseUpdateClient.VerifyAsync(installer,release,timeout.Token))throw new InvalidDataException("Installateur modifié pendant la fermeture.");
    var start=new ProcessStartInfo(installer){UseShellExecute=false,CreateNoWindow=true};start.ArgumentList.Add("/S");
    using var process=Process.Start(start)??throw new IOException("Installation impossible.");await process.WaitForExitAsync(timeout.Token);
    if(process.ExitCode!=0)throw new IOException("Installation interrompue : "+process.ExitCode);
    var installed=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Programs","Aegis","Aegis.exe");
    var relaunch=new ProcessStartInfo(installed){UseShellExecute=false};relaunch.ArgumentList.Add("--data-dir");relaunch.ArgumentList.Add(data);Process.Start(relaunch);return 0;
}
catch(Exception e)
{
    if(data.Length>0){try{File.WriteAllText(Path.Combine(data,"update-error.txt"),e.Message);}catch(IOException){}}
    return 1;
}
