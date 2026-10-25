namespace Aegis.Domain;

public sealed record GameProcess(int Id,long Started,string Executable,string Title);
public sealed record GameTransition(GameProcess Game,bool Started);

/// <summary>Process names only: no game memory, launcher inspection or network access.</summary>
public static class GameCatalog
{
    public static readonly IReadOnlyDictionary<string,string> Known=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase)
    {
        ["League of Legends"]="League of Legends",["cs2"]="Counter-Strike 2",
        ["VALORANT-Win64-Shipping"]="VALORANT",["FortniteClient-Win64-Shipping"]="Fortnite",
        ["RocketLeague"]="Rocket League",["r5apex"]="Apex Legends",["Overwatch"]="Overwatch 2",
        ["Minecraft.Windows"]="Minecraft",["GTA5"]="Grand Theft Auto V",["GTA5_Enhanced"]="Grand Theft Auto V Enhanced",
        ["dota2"]="Dota 2",["RainbowSix"]="Rainbow Six Siege",["eldenring"]="Elden Ring",
        ["Cyberpunk2077"]="Cyberpunk 2077",["bg3"]="Baldur’s Gate 3",["bg3_dx11"]="Baldur’s Gate 3"
    };
    public static string Normalize(string executable)=>executable.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)?executable[..^4]:executable;
    public static string? Title(string executable,string custom="")
    {
        string name=Normalize(executable);
        if(Known.TryGetValue(name,out var title))return title;
        return custom.Split(';',StringSplitOptions.TrimEntries|StringSplitOptions.RemoveEmptyEntries)
            .Select(Normalize).Any(x=>!x.Contains('/')&&!x.Contains('\\')&&string.Equals(x,name,StringComparison.OrdinalIgnoreCase))?name:null;
    }
}

/// <summary>Debounce start, tolerate short disappearance, never restart a manually stopped game.</summary>
public sealed class GameSessionPlanner
{
    private readonly HashSet<(int,long)> suppressed=[];
    private GameProcess? candidate;
    private int seen,missing;
    public GameProcess? Active{get;private set;}
    public void SuppressActive(){if(Active is {} game)suppressed.Add((game.Id,game.Started));Active=null;candidate=null;seen=missing=0;}
    public GameTransition? Observe(IReadOnlyList<GameProcess> games,bool mayStart)
    {
        suppressed.RemoveWhere(id=>!games.Any(g=>(g.Id,g.Started)==id));
        if(Active is {} active)
        {
            if(games.Any(g=>(g.Id,g.Started)==(active.Id,active.Started))){missing=0;return null;}
            if(++missing<3)return null;
            Active=null;candidate=null;seen=missing=0;return new(active,false);
        }
        if(!mayStart){candidate=null;seen=0;return null;}
        var next=games.FirstOrDefault(g=>!suppressed.Contains((g.Id,g.Started)));
        if(next is null){candidate=null;seen=0;return null;}
        if(candidate is null||(candidate.Id,candidate.Started)!=(next.Id,next.Started)){candidate=next;seen=1;return null;}
        if(++seen<2)return null;
        Active=next;missing=0;return new(next,true);
    }
}
