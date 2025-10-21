using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Modules.Timers;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text.Json.Serialization;

public class BombCountdownConfig : BasePluginConfig
{
    [JsonPropertyName("Enabled")]
    public bool Enabled { get; set; } = true;
    
    [JsonPropertyName("StartFromSecond")]
    public int StartFromSecond { get; set; } = 40;
    
    [JsonPropertyName("AnnounceSeconds")]
    public List<int> AnnounceSeconds { get; set; } = new();
    
    [JsonPropertyName("SoundPath")]
    public string SoundPath { get; set; } = "sounds/bombcountdown";
    
    [JsonPropertyName("ShowTextCountdown")]
    public bool ShowTextCountdown { get; set; } = true;
    
    [JsonPropertyName("PlaySoundCountdown")]
    public bool PlaySoundCountdown { get; set; } = true;
    
    [JsonPropertyName("TextColor")]
    public string TextColor { get; set; } = "{RED}";
    
    [JsonPropertyName("Debug")]
    public bool Debug { get; set; } = false;
}

[MinimumApiVersion(80)]
public class BombCountdownPlugin : BasePlugin, IPluginConfig<BombCountdownConfig>
{
    public override string ModuleName => "Bomb Countdown";
    public override string ModuleVersion => "1.2.0";
    public override string ModuleAuthor => "Assistant";
    public override string ModuleDescription => "Bomb countdown with sound from 40 seconds to 1 second";
    
    public BombCountdownConfig Config { get; set; } = new();
    
    private Timer? _countdownTimer;
    private int _currentSecond;
    private bool _isBombPlanted = false;
    private bool _isCountdownActive = false;

    public void OnConfigParsed(BombCountdownConfig config)
    {
        if (config.AnnounceSeconds.Count == 0)
        {
            config.AnnounceSeconds = new List<int> { 40, 35, 30, 25, 20, 15, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1 };
        }
        
        Config = config;
        
        if (Config.Debug)
        {
            Console.WriteLine("[BombCountdown] Configuration loaded successfully!");
        }
    }

    public override void Load(bool hotReload)
    {
        RegisterEventHandler<EventBombPlanted>(OnBombPlanted);
        RegisterEventHandler<EventBombExploded>(OnBombExploded);
        RegisterEventHandler<EventBombDefused>(OnBombDefused);
        RegisterEventHandler<EventRoundEnd>(OnRoundEnd);
        RegisterEventHandler<EventRoundStart>(OnRoundStart);
        
        Console.WriteLine("[BombCountdown] Plugin loaded successfully!");
    }

    private HookResult OnBombPlanted(EventBombPlanted eventObj, GameEventInfo info)
    {
        if (!Config.Enabled) return HookResult.Continue;
        
        StartCountdown();
        return HookResult.Continue;
    }

    private void StartCountdown()
    {
        _isBombPlanted = true;
        _isCountdownActive = true;
        _currentSecond = Config.StartFromSecond;
        
        _countdownTimer?.Kill();
        _countdownTimer = AddTimer(1.0f, OnCountdownTick, TimerFlags.REPEAT);
    }

    private void OnCountdownTick()
    {
        if (!_isBombPlanted || !_isCountdownActive)
        {
            _countdownTimer?.Kill();
            return;
        }
        
        if (Config.AnnounceSeconds.Contains(_currentSecond))
        {
            AnnounceCountdown(_currentSecond);
        }
        
        _currentSecond--;
        
        if (_currentSecond <= 0)
        {
            _countdownTimer?.Kill();
            _isCountdownActive = false;
        }
    }

    private void AnnounceCountdown(int seconds)
    {
        if (Config.ShowTextCountdown)
        {
            var message = $"{Config.TextColor}⚡ БОМБА: {seconds} секунд{GetRussianEnding(seconds)}!";
            Server.PrintToChatAll(message);
        }
        
        if (Config.PlaySoundCountdown)
        {
            var soundPath = $"{Config.SoundPath}/{seconds}.vsnd";
            PlaySoundToAll(soundPath);
        }
    }

    private void PlaySoundToAll(string soundPath)
    {
        var players = Utilities.GetPlayers();
        foreach (var player in players)
        {
            if (player?.IsValid == true && !player.IsBot)
            {
                player.ExecuteClientCommand($"play {soundPath}");
            }
        }
    }

    private string GetRussianEnding(int number)
    {
        if (number == 1) return "а";
        if (number >= 2 && number <= 4) return "ы";
        return "";
    }

    private void StopCountdown()
    {
        _isBombPlanted = false;
        _isCountdownActive = false;
        _countdownTimer?.Kill();
        _countdownTimer = null;
    }

    private HookResult OnBombExploded(EventBombExploded eventObj, GameEventInfo info)
    {
        StopCountdown();
        return HookResult.Continue;
    }

    private HookResult OnBombDefused(EventBombDefused eventObj, GameEventInfo info)
    {
        StopCountdown();
        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd eventObj, GameEventInfo info)
    {
        StopCountdown();
        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart eventObj, GameEventInfo info)
    {
        StopCountdown();
        _isBombPlanted = false;
        return HookResult.Continue;
    }

    public override void Unload(bool hotReload)
    {
        StopCountdown();
        base.Unload(hotReload);
    }
}
