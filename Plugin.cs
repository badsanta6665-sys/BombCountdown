
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

public class RoundEndSoundConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("Sounds")]
    public SoundConfig Sounds { get; set; } = new();
    
    [JsonPropertyName("Volume")]
    public float Volume { get; set; } = 1.0f;
    
    [JsonPropertyName("DelayBeforeSound")]
    public float DelayBeforeSound { get; set; } = 1.0f;
    
    [JsonPropertyName("RandomSounds")]
    public bool RandomSounds { get; set; } = false;
    
    [JsonPropertyName("PlayForWinningTeamOnly")]
    public bool PlayForWinningTeamOnly { get; set; } = false;
    
    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = false;
}

public class SoundConfig
{
    [JsonPropertyName("CTWin")]
    public string CTWin { get; set; } = "sounds/roundend/ct_win.vsnd_c";
    
    [JsonPropertyName("TWin")]
    public string TWin { get; set; } = "sounds/roundend/t_win.vsnd_c";
    
    [JsonPropertyName("Draw")]
    public string Draw { get; set; } = "sounds/roundend/draw.vsnd_c";
    
    [JsonPropertyName("RoundEnd")]
    public string RoundEnd { get; set; } = "sounds/roundend/round_end.vsnd_c";
    
    [JsonPropertyName("CTWinSounds")]
    public List<string> CTWinSounds { get; set; } = new();
    
    [JsonPropertyName("TWinSounds")]
    public List<string> TWinSounds { get; set; } = new();
    
    [JsonPropertyName("RoundEndSounds")]
    public List<string> RoundEndSounds { get; set; } = new();
}

[MinimumApiVersion(80)]
public class RoundEndSoundPlugin : BasePlugin, IPluginConfig<RoundEndSoundConfig>
{
    public override string ModuleName => "Round End Sound";
    public override string ModuleVersion => "1.0.0";
    public override string ModuleAuthor => "Assistant";
    public override string ModuleDescription => "Plays vsnd_c sounds at round end in CS2";
    
    public RoundEndSoundConfig Config { get; set; } = new();
    private readonly Random _random = new();

    public void OnConfigParsed(RoundEndSoundConfig config)
    {
        Config = config;
        
        if (Config.Debug)
        {
            Console.WriteLine("[RoundEndSound] Configuration loaded successfully!");
        }
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
        Console.WriteLine("[RoundEndSound] Plugin loaded successfully!");
    }

    private HookResult OnRoundEnd(EventRoundEnd eventObj, GameEventInfo info)
    {
        if (!Config.Enabled) return HookResult.Continue;

        try
        {
            var winner = (CsTeam)eventObj.Winner;
            var reason = eventObj.Reason;
            
            if (Config.Debug)
            {
                Console.WriteLine($"[RoundEndSound] Round ended. Winner: {winner}, Reason: {reason}");
            }
            
            // Запускаем звуки с задержкой
            AddTimer(Config.DelayBeforeSound, () => PlayRoundEndSounds(winner));
        }
        catch (Exception ex)
        {
            if (Config.Debug)
            {
                Console.WriteLine($"[RoundEndSound] Error: {ex.Message}");
            }
        }
        
        return HookResult.Continue;
    }

    private void PlayRoundEndSounds(CsTeam winner)
    {
        // Проигрываем звук конца раунда
        PlayRoundEndSound();
        
        // Проигрываем звук победителя с небольшой задержкой
        AddTimer(0.5f, () => PlayTeamWinSound(winner));
    }

    private void PlayRoundEndSound()
    {
        string soundPath = GetRoundEndSound();
        
        if (Config.PlayForWinningTeamOnly)
        {
            // Проигрываем только определенным игрокам
            PlaySoundToAll(soundPath);
        }
        else
        {
            // Проигрываем всем
            PlaySoundToAll(soundPath);
        }
        
        if (Config.Debug)
        {
            Console.WriteLine($"[RoundEndSound] Playing round end sound: {soundPath}");
        }
    }

    private void PlayTeamWinSound(CsTeam winner)
    {
        string soundPath = GetTeamWinSound(winner);
        
        if (Config.PlayForWinningTeamOnly && winner != CsTeam.None)
        {
            // Проигрываем только победившей команде
            PlaySoundToTeam(winner, soundPath);
        }
        else
        {
            // Проигрываем всем
            PlaySoundToAll(soundPath);
        }
        
        if (Config.Debug)
        {
            Console.WriteLine($"[RoundEndSound] Playing team sound: {soundPath} for {winner}");
        }
    }

    private string GetRoundEndSound()
    {
        if (Config.RandomSounds && Config.Sounds.RoundEndSounds.Count > 0)
        {
            return Config.Sounds.RoundEndSounds[_random.Next(Config.Sounds.RoundEndSounds.Count)];
        }
        return Config.Sounds.RoundEnd;
    }

    private string GetTeamWinSound(CsTeam winner)
    {
        return winner switch
        {
            CsTeam.CounterTerrorist => GetCTWinSound(),
            CsTeam.Terrorist => GetTWinSound(),
            _ => Config.Sounds.Draw
        };
    }

    private string GetCTWinSound()
    {
        if (Config.RandomSounds && Config.Sounds.CTWinSounds.Count > 0)
        {
            return Config.Sounds.CTWinSounds[_random.Next(Config.Sounds.CTWinSounds.Count)];
        }
        return Config.Sounds.CTWin;
    }

    private string GetTWinSound()
    {
        if (Config.RandomSounds && Config.Sounds.TWinSounds.Count > 0)
        {
            return Config.Sounds.TWinSounds[_random.Next(Config.Sounds.TWinSounds.Count)];
        }
        return Config.Sounds.TWin;
    }

    private void PlaySoundToAll(string soundPath)
    {
        if (string.IsNullOrEmpty(soundPath)) return;
        
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player?.IsValid == true && !player.IsBot)
            {
                PlaySoundToPlayer(player, soundPath);
            }
        }
    }

    private void PlaySoundToTeam(CsTeam team, string soundPath)
    {
        if (string.IsNullOrEmpty(soundPath)) return;
        
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player?.IsValid == true && !player.IsBot && player.Team == team)
            {
                PlaySoundToPlayer(player, soundPath);
            }
        }
    }

    private void PlaySoundToPlayer(CCSPlayerController player, string soundPath)
    {
        if (string.IsNullOrEmpty(soundPath) || player?.IsValid != true) return;
        
        try
        {
            // Правильное воспроизведение vsnd_c файлов в CS2
            player.ExecuteClientCommand($"play {soundPath}");
            
            // Альтернативный способ через эмиттер звуков
            // player.EmitSound(soundPath);
        }
        catch (Exception ex)
        {
            if (Config.Debug)
            {
                Console.WriteLine($"[RoundEndSound] Error playing sound to {player.PlayerName}: {ex.Message}");
            }
        }
    }

    private HookResult OnRoundStart(EventRoundStart eventObj, GameEventInfo info)
    {
        // Очистка или подготовка к новому раунду
        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        Console.WriteLine("[RoundEndSound] Plugin unloaded");
        base.Unload(hotReload);
    }
}
