using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Timers;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using static CounterStrikeSharp.API.Core.Listeners;

namespace CS2_AutoExec
{
    public class AutoExec : BasePlugin
	{
		static ILogger? g_Logger;
		ConfigJSON? cfg = new();
		ConfigJSON? cfgPrefix = new();
		ConfigJSON? cfgMap = new();
		static bool bHalfTime = false;
		public override string ModuleName => "Auto Exec";
		public override string ModuleDescription => "Automatically executes commands after events";
		public override string ModuleAuthor => "DarkerZ [RUS]";
		public override string ModuleVersion => "1.DZ.3";
		public override void Load(bool hotReload)
		{
			g_Logger = Logger;
			LoadCFG();
			RegisterListener<OnMapStart>(OnMapStart_Listener);
			RegisterListener<OnMapEnd>(OnMapEnd_Listener);
			RegisterEventHandler<EventRoundStart>(OnEventRoundStart);
			RegisterEventHandler<EventRoundEnd>(OnEventRoundEnd);
		}
		public override void Unload(bool hotReload)
		{
			RemoveListener<OnMapStart>(OnMapStart_Listener);
			RemoveListener<OnMapEnd>(OnMapEnd_Listener);
			DeregisterEventHandler<EventRoundStart>(OnEventRoundStart);
			DeregisterEventHandler<EventRoundEnd>(OnEventRoundEnd);
			RemoveCommand("css_ae_reload", OnReload);
			cfg?.StopTimers();
			cfgPrefix?.StopTimers();
			cfgMap?.StopTimers();
		}
		void OnMapStart_Listener(string sMapName)
		{
			cfg?.OnMapSpawnHandler();
			LoadCFGPrefix();
			cfgPrefix?.OnMapSpawnHandler();
			LoadCFGMap();
			cfgMap?.OnMapSpawnHandler();
		}
		void OnMapEnd_Listener()
		{
			cfg?.OnMapEndHandler();
			cfgPrefix?.OnMapEndHandler();
			cfgPrefix?.StopTimers();
			cfgPrefix = null;
			cfgMap?.OnMapEndHandler();
			cfgMap?.StopTimers();
			cfgMap = null;
		}
		[GameEventHandler]
		private HookResult OnEventRoundStart(EventRoundStart @event, GameEventInfo info)
		{
			cfg?.OnRoundStartHandler();
			cfgPrefix?.OnRoundStartHandler();
			cfgMap?.OnRoundStartHandler();
			return HookResult.Continue;
		}
		[GameEventHandler]
		private HookResult OnEventRoundEnd(EventRoundEnd @event, GameEventInfo info)
		{
			cfg?.OnRoundEndHandler();
			cfgPrefix?.OnRoundEndHandler();
			cfgMap?.OnRoundEndHandler();
			return HookResult.Continue;
		}

		[ConsoleCommand("css_ae_reload", "Reload config file of AutoExec")]
		[RequiresPermissions("@css/root")]
		public void OnReload(CCSPlayerController? player, CommandInfo command)
		{
			if (player != null && !player.IsValid) return;
			cfg?.StopTimers();
			cfgPrefix?.StopTimers();
			cfgMap?.StopTimers();
			cfg = null;
			cfgPrefix = null;
			cfgMap = null;
			LoadCFG();
			LoadCFGPrefix();
			LoadCFGMap();
			if (cfg != null)
			{
				if (player != null)
				{
					command.ReplyToCommand(" \x0B[\x04 AutoExec \x0B]\x01 Global ConfigFile reloaded!");
					PrintToConsole($"Global ConfigFile reloaded by {player.PlayerName} ({player.SteamID})");
				}
				else PrintToConsole($"Global ConfigFile reloaded!");
			}
			if (cfgPrefix != null)
			{
				int iIndex = Server.MapName.ToLower().IndexOf('_');
				if (iIndex > 0)
				{
					string sPrefix = Server.MapName.ToLower()[..iIndex];
					if (player != null)
					{
						command.ReplyToCommand($" \x0B[\x04 AutoExec \x0B]\x01 ConfigFile for prefix {sPrefix} reloaded!");
						PrintToConsole($"ConfigFile for prefix {sPrefix} reloaded by {player.PlayerName} ({player.SteamID})");
					}
					else PrintToConsole($"ConfigFile for prefix {sPrefix} reloaded!");
				}
			}
			if (cfgMap != null)
			{
				if (player != null)
				{
					command.ReplyToCommand($" \x0B[\x04 AutoExec \x0B]\x01 ConfigFile for map {Server.MapName.ToLower()} reloaded!");
					PrintToConsole($"ConfigFile for map {Server.MapName.ToLower()} reloaded by {player.PlayerName} ({player.SteamID})");
				}
				else PrintToConsole($"ConfigFile for map {Server.MapName.ToLower()} reloaded!");
			}
			if (cfg == null && cfgMap == null && cfgPrefix == null)
			{
				if (player != null)
					command.ReplyToCommand(" \x0B[\x04 AutoExec \x0B]\x01 Bad ConfigFiles!");
				PrintToConsole($"Bad ConfigFiles!");
			}
		}
		void LoadCFG()
		{
			string sConfig = $"{Path.Join(ModuleDirectory, "config.json")}";
			string sData;
			if (File.Exists(sConfig))
			{
				try
				{
					sData = File.ReadAllText(sConfig);
					cfg = JsonSerializer.Deserialize<ConfigJSON>(sData);
				}
				catch
				{
					cfg = null;
					PrintToConsole($"Bad Config file ({sConfig})");
				}
			}
			else
			{
				cfg = null;
				PrintToConsole($"Config file ({sConfig}) not found");
			}
		}
		void LoadCFGMap()
		{
			string sConfig = $"{Path.Join(ModuleDirectory, $"/maps/{Server.MapName.ToLower()}.json")}";
			string sData;
			if (File.Exists(sConfig))
			{
				try
				{
					sData = File.ReadAllText(sConfig);
					cfgMap = JsonSerializer.Deserialize<ConfigJSON>(sData);
				}
				catch
				{
					cfgMap = null;
					PrintToConsole($"Bad Config file ({sConfig})");
				}
			}
			else
			{
				cfgMap = null;
				PrintToConsole($"Config file ({sConfig}) not found");
			}
		}
		void LoadCFGPrefix()
		{
			int iIndex = Server.MapName.ToLower().IndexOf('_');
			if (iIndex <= 0)
			{
				cfgPrefix = null;
				return;
			}
			string sPrefix = Server.MapName.ToLower()[..iIndex];
			string sConfig = $"{Path.Join(ModuleDirectory, $"/prefix/{sPrefix}.json")}";
			string sData;
			if (File.Exists(sConfig))
			{
				try
				{
					sData = File.ReadAllText(sConfig);
					cfgPrefix = JsonSerializer.Deserialize<ConfigJSON>(sData);
				}
				catch
				{
					cfgPrefix = null;
					PrintToConsole($"Bad Config file ({sConfig})");
				}
			}
			else
			{
				cfgPrefix = null;
				PrintToConsole($"Config file ({sConfig}) not found");
			}
		}

		class ConfigJSON
		{
			public List<EventInfo> OnMapSpawn { get; set; }
			public List<EventInfo> OnMapEnd { get; set; }
			public List<EventInfo> OnRoundStartAlways { get; set; }
			public List<EventInfo> OnRoundStartWarmUp { get; set; }
			public List<EventInfo> OnRoundStartAfterWarmUp { get; set; }
			public List<EventInfo> OnRoundEndAlways { get; set; }
			public List<EventInfo> OnRoundEndWarmUp { get; set; }
			public List<EventInfo> OnRoundEndAfterWarmUp { get; set; }
			public List<EventInfo> OnHalfTime { get; set; }
			public List<EventInfo> OnHalfTimeEnd { get; set; }
			public void OnMapSpawnHandler()
			{
				StopTimers();
				foreach (EventInfo e in OnMapSpawn) e.EventInfoHandler();
			}
			public void OnMapEndHandler()
			{
				KillAllTimers(OnMapSpawn);
				KillAllTimers(OnRoundStartAlways);
				KillAllTimers(OnRoundStartWarmUp);
				KillAllTimers(OnRoundStartAfterWarmUp);
				KillAllTimers(OnHalfTime);

				foreach (EventInfo e in OnMapEnd) e.EventInfoHandler();
			}
			public void OnRoundStartHandler()
			{
				KillAllTimers(OnRoundEndAlways);
				KillAllTimers(OnRoundEndWarmUp);
				KillAllTimers(OnRoundEndAfterWarmUp);
				KillAllTimers(OnHalfTimeEnd);

				foreach (EventInfo e in OnRoundStartAlways) e.EventInfoHandler();

				if (IsWarmup()) foreach (EventInfo e in OnRoundStartWarmUp) e.EventInfoHandler();
				else foreach (EventInfo e in OnRoundStartAfterWarmUp) e.EventInfoHandler();

				bHalfTime = IsHalfTime();
				if (bHalfTime) foreach (EventInfo e in OnHalfTime) e.EventInfoHandler();
			}
			public void OnRoundEndHandler()
			{
				KillAllTimers(OnRoundStartAlways);
				KillAllTimers(OnRoundStartWarmUp);
				KillAllTimers(OnRoundStartAfterWarmUp);
				KillAllTimers(OnHalfTime);

				foreach (EventInfo e in OnRoundEndAlways) e.EventInfoHandler();

				if (IsWarmup()) foreach (EventInfo e in OnRoundEndWarmUp) e.EventInfoHandler();
				else foreach (EventInfo e in OnRoundEndAfterWarmUp) e.EventInfoHandler();

				if (bHalfTime) foreach (EventInfo e in OnHalfTimeEnd) e.EventInfoHandler();
				bHalfTime = false;
			}
			public void StopTimers()
			{
				KillAllTimers(OnMapSpawn);
				KillAllTimers(OnMapEnd);
				KillAllTimers(OnRoundStartAlways);
				KillAllTimers(OnRoundStartWarmUp);
				KillAllTimers(OnRoundStartAfterWarmUp);
				KillAllTimers(OnRoundEndAlways);
				KillAllTimers(OnRoundEndWarmUp);
				KillAllTimers(OnRoundEndAfterWarmUp);
				KillAllTimers(OnHalfTime);
				KillAllTimers(OnHalfTimeEnd);
			}
			static void KillAllTimers(List<EventInfo> ListEventInfo)
			{
				foreach (EventInfo e in ListEventInfo) e.KillTimer();
			}
			public ConfigJSON()
			{
				OnMapSpawn = [];
				OnMapEnd = [];
				OnRoundStartAlways = [];
				OnRoundStartWarmUp = [];
				OnRoundStartAfterWarmUp = [];
				OnRoundEndAlways = [];
				OnRoundEndWarmUp = [];
				OnRoundEndAfterWarmUp = [];
				OnHalfTime = [];
				OnHalfTimeEnd = [];
			}
			~ConfigJSON()
			{
				OnMapSpawn.Clear();
				OnMapEnd.Clear();
				OnRoundStartAlways.Clear();
				OnRoundStartWarmUp.Clear();
				OnRoundStartAfterWarmUp.Clear();
				OnRoundEndAlways.Clear();
				OnRoundEndWarmUp.Clear();
				OnRoundEndAfterWarmUp.Clear();
				OnHalfTime.Clear();
				OnHalfTimeEnd.Clear();
			}
		}
		class EventInfo
		{
			CounterStrikeSharp.API.Modules.Timers.Timer? Timer;
			public bool ShowInConsole { get; set; }
			public bool EntryInLog { get; set; }
			public float Delay { get; set; }
			public List<string> Commands { get; set; }
			public EventInfo()
			{
				ShowInConsole = false;
				EntryInLog = false;
				Timer = null;
				Delay = 0.0f;
				Commands = [];
			}
			~EventInfo()
			{
				KillTimer();
			}
			public void EventInfoHandler()
			{
				if (Delay <= 0.0f) ExecCommands();
				else CreateTimer();
			}
			void ExecCommands()
			{
				Server.NextFrame(() =>
				{
					if (IsValid())
					{
						foreach (string sCommand in Commands)
						{
							Server.ExecuteCommand(sCommand);
							if (ShowInConsole) PrintToConsole($"Exec: {sCommand}");
							if (EntryInLog) g_Logger?.LogInformation($"Exec: {sCommand}");
						}
					}
				});
			}
			void CreateTimer()
			{
				KillTimer();
				Timer = new CounterStrikeSharp.API.Modules.Timers.Timer(Delay, () =>
				{
					ExecCommands();
					KillTimer();
				}, TimerFlags.STOP_ON_MAPCHANGE);
			}
			public void KillTimer()
			{
				if (Timer != null)
				{
					Timer.Kill();
					Timer = null;
				}
			}
			bool IsValid()
			{
				if (Commands != null && Commands.Count > 0) return true;
				return false;
			}
		}
		static CCSGameRules? GetGameRules()
		{
			try
			{
				var gameRulesEntities = Utilities.FindAllEntitiesByDesignerName<CCSGameRulesProxy>("cs_gamerules");
				return gameRulesEntities.First().GameRules;
			}
			catch (Exception)
			{
				return null;
			}
		}
		static bool IsWarmup()
		{
			return GetGameRules()?.WarmupPeriod ?? false;
		}
		static bool IsHalfTime()
		{
			var gamerules = GetGameRules();
			var halftime = ConVar.Find("mp_halftime")!.GetPrimitiveValue<bool>();
			var maxrounds = ConVar.Find("mp_maxrounds")!.GetPrimitiveValue<int>();

			if (gamerules == null || maxrounds <= 0) return false;
			return gamerules.TotalRoundsPlayed == 0 || (halftime && maxrounds / 2 == gamerules.TotalRoundsPlayed) || gamerules.GameRestart;
		}
		static void PrintToConsole(string sValue)
		{
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("[");
			Console.ForegroundColor = (ConsoleColor)6;
			Console.Write("AutoExec");
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("] ");
			Console.ForegroundColor = (ConsoleColor)3;
			Console.WriteLine(sValue);
			Console.ResetColor();
		}
	}
}
