using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
using NAudio.Wave;
namespace Aegis.Desktop;
public sealed class VoiceService : IDisposable
{
    private WaveInEvent? input;
    private WaveFileWriter? writer;
    private SpeechSynthesizer? speech;
    private readonly string tempFile;
    private readonly object audioLock=new();
    public bool Recording=>input is not null;
    public VoiceService(string dataDirectory) {tempFile=Path.Combine(dataDirectory,"microphone-temp.wav");}
    public void Start()
    {
        StopSpeaking();
        if(input is not null)return;
        if(WaveIn.DeviceCount==0)throw new InvalidOperationException("Aucun microphone Windows disponible.");
        var next=new WaveInEvent { WaveFormat=new WaveFormat(16000,1), BufferMilliseconds=100 };
        writer=new WaveFileWriter(tempFile,next.WaveFormat);
        next.DataAvailable+=(_,e)=>{lock(audioLock){writer?.Write(e.Buffer,0,e.BytesRecorded);}};
        input=next;
        try{next.StartRecording();}catch{next.Dispose();input=null;writer.Dispose();writer=null;throw;}
    }
    public async Task<string> StopAndTranscribeAsync(string cli,string model,CancellationToken token)
    {
        if(input is null)throw new InvalidOperationException("Microphone inactif.");
        var finished=new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var current=input;
        current.RecordingStopped+=(_,e)=>{lock(audioLock){writer?.Dispose();writer=null;} if(e.Exception is not null)finished.TrySetException(e.Exception);else finished.TrySetResult();};
        try{current.StopRecording();await finished.Task.WaitAsync(TimeSpan.FromSeconds(5),token);}
        catch{CancelRecording();throw;}
        finally{current.Dispose();input=null;}
        try
        {
            if(!File.Exists(cli)||!File.Exists(model))throw new FileNotFoundException("Configurez whisper-cli.exe et un modèle multilingue dans Réglages.");
            var start=new ProcessStartInfo(cli) {UseShellExecute=false,CreateNoWindow=true,WindowStyle=ProcessWindowStyle.Hidden,RedirectStandardError=true,RedirectStandardOutput=true};
            foreach(var a in new[]{"-m",model,"-f",tempFile,"-l","fr","-nt"})start.ArgumentList.Add(a);
            using var p=Process.Start(start)??throw new IOException("Transcription impossible.");
            using var registration=token.Register(()=>{try{if(!p.HasExited)p.Kill(true);}catch(InvalidOperationException){}});
            var output=p.StandardOutput.ReadToEndAsync(token);var error=p.StandardError.ReadToEndAsync(token);
            await p.WaitForExitAsync(token); var text=await output;var err=await error;
            if(p.ExitCode!=0)throw new IOException("Transcription échouée : "+err[..Math.Min(err.Length,400)]);
            return text.Trim();
        }
        finally {if(File.Exists(tempFile))File.Delete(tempFile);}
    }
    public void Speak(string text)
    {
        speech??=new SpeechSynthesizer();speech.SpeakAsyncCancelAll();
        var french=speech.GetInstalledVoices().FirstOrDefault(v=>v.Enabled&&v.VoiceInfo.Culture.TwoLetterISOLanguageName=="fr");
        if(french is not null)speech.SelectVoice(french.VoiceInfo.Name);
        speech.SpeakAsync(text[..Math.Min(text.Length,2200)]);
    }
    public void StopSpeaking()=>speech?.SpeakAsyncCancelAll();
    public void CancelRecording()
    {
        var current=input;input=null;
        try{current?.StopRecording();}finally{current?.Dispose();lock(audioLock){writer?.Dispose();writer=null;}try{if(File.Exists(tempFile))File.Delete(tempFile);}catch(IOException){}}
    }
    public void Dispose(){CancelRecording();speech?.Dispose();}
}
