using Aegis.Domain;
internal static class GameTests
{
    public static void Run(Action<bool,string> check)
    {
        check(!Metrics.IsTemperatureReading(new("cpu.distance","CPU","CPU Core Distance to TjMax","°C",90,"test",MetricState.Available)),"Thermal headroom never reported as component temperature");
        check(Metrics.IsTemperatureReading(new("cpu.package","CPU","CPU Package","°C",45,"test",MetricState.Available)),"Real package temperature retained");
        check(GameCatalog.Title("LeagueClientUx.exe") is null,"LoL launcher never starts a game session");
        check(GameCatalog.Title("League of Legends.exe")=="League of Legends","LoL game executable recognized");
        check(GameCatalog.Title("STEAM.exe") is null,"Store launcher not classified as game");
        check(GameCatalog.Title("mygame","mygame.exe; other.exe")=="mygame","Custom executable exact match");
        check(GameCatalog.Title("mygamehelper","mygame.exe") is null,"Custom game name cannot match helper prefix");
        check(GameCatalog.Title("mygame","C:/games/mygame.exe") is null,"Custom full path rejected rather than guessed");
        var game=new GameProcess(42,100,"League of Legends","League of Legends");
        var planner=new GameSessionPlanner();
        check(planner.Observe([game],true) is null,"Game start debounced");
        check(planner.Observe([game],true) is {Started:true},"Stable game starts session once");
        check(planner.Observe([game],false) is null,"Active game never duplicates recording");
        check(planner.Observe([],false) is null&&planner.Observe([game],false) is null,"Short game disappearance tolerated");
        planner.SuppressActive();
        check(planner.Observe([game],true) is null&&planner.Observe([game],true) is null,"Manual stop suppresses same running game");
        planner.Observe([],true);planner.Observe([game],true);
        check(planner.Observe([game],true) is {Started:true},"Game can record after a new launch");
        check(planner.Observe([],false) is null&&planner.Observe([],false) is null&&planner.Observe([],false) is {Started:false},"End waits for three missing observations");
        var reused=game with{Started=200};planner.Observe([game],true);planner.Observe([game],true);planner.SuppressActive();planner.Observe([reused],true);
        check(planner.Observe([reused],true) is {Started:true},"PID reuse cannot inherit suppressed identity");
        var blocked=new GameSessionPlanner();
        check(blocked.Observe([game],false) is null&&blocked.Observe([game],false) is null,"Manual recording prevents auto session start");
    }
}
