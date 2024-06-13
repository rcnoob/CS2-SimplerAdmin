using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace CS2_SimplerAdmin
{
	public partial class CS2_SimplerAdmin
	{
		[ConsoleCommand("css_adminhelp")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/generic")]
		public void OnAdminHelpCommand(CCSPlayerController? caller, CommandInfo command)
		{
			//if (caller == null ||!caller.IsValid) return;

			/*
			using (new WithTemporaryCulture(caller.GetLanguage()))
			{
				var splitMessage = _localizer!["sa_adminhelp"].ToString().Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

				foreach (var line in splitMessage)
				{
					caller.PrintToChat(Helper.ReplaceTags($" {line}"));
				}
			} */

			var lines = File.ReadAllLines(ModuleDirectory + "/admin_help.txt");

			foreach (var line in lines)
			{
				command.ReplyToCommand(string.IsNullOrWhiteSpace(line) ? " " : line.ReplaceColorTags());
			}
		}

		[ConsoleCommand("css_addadmin")]
		[CommandHelper(minArgs: 4, usage: "<steamid> <name> <flags/groups> <immunity> <duration>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnAddAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var steamid = steamId.SteamId64.ToString();

			if (command.GetArg(2).Length <= 0)
			{
				command.ReplyToCommand($"Invalid player name.");
				return;
			}
			if (!command.GetArg(3).Contains('@') && !command.GetArg(3).Contains('#'))
			{
				command.ReplyToCommand($"Invalid flag or group.");
				return;
			}

			var name = command.GetArg(2);
			var flags = command.GetArg(3);
			var globalAdmin = command.GetArg(4).ToLower().Equals("-g") || command.GetArg(5).ToLower().Equals("-g") ||
					  command.GetArg(6).ToLower().Equals("-g");
			int.TryParse(command.GetArg(4), out var immunity);
			int.TryParse(command.GetArg(5), out var time);

			AddAdmin(caller, steamid, name, flags, immunity, time, globalAdmin, command);
		}

		public static void AddAdmin(CCSPlayerController? caller, string steamid, string name, string flags, int immunity, int time = 0, bool globalAdmin = false, CommandInfo? command = null)
		{
			if (_database == null) return;
			PermissionManager adminManager = new(_database);

			var flagsList = flags.Split(',').Select(flag => flag.Trim()).ToList();
			_ = adminManager.AddAdminBySteamId(steamid, name, flagsList, immunity, time, globalAdmin);

			var msg = $"Added '{flags}' flags to '{name}' ({steamid})";
			if (command != null)
				command.ReplyToCommand(msg);
			else if (caller != null && caller.IsValid)
				caller.PrintToChat(msg);
			else
				Server.PrintToConsole(msg);
		}

		[ConsoleCommand("css_deladmin")]
		[CommandHelper(minArgs: 1, usage: "<steamid>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnDelAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			if (!Helper.ValidateSteamId(command.GetArg(1), out var steamId) || steamId == null)
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var globalDelete = command.GetArg(2).ToLower().Equals("-g");

			RemoveAdmin(caller, steamId.SteamId64.ToString(), globalDelete, command);
		}

		public void RemoveAdmin(CCSPlayerController? caller, string steamid, bool globalDelete = false, CommandInfo? command = null)
		{
			if (_database == null) return;
			PermissionManager adminManager = new(_database);
			_ = adminManager.DeleteAdminBySteamId(steamid, globalDelete);

			AddTimer(2, () =>
			{
				if (string.IsNullOrEmpty(steamid) || !SteamID.TryParse(steamid, out var steamId) ||
					steamId == null) return;
				if (PermissionManager.AdminCache.ContainsKey(steamId))
				{
					PermissionManager.AdminCache.TryRemove(steamId, out _);
				}

				AdminManager.ClearPlayerPermissions(steamId);
				AdminManager.RemovePlayerAdminData(steamId);
			}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			var msg = $"Removed flags from '{steamid}'";
			if (command != null)
				command.ReplyToCommand(msg);
			else if (caller != null && caller.IsValid)
				caller.PrintToChat(msg);
			else
				Server.PrintToConsole(msg);
		}



		[ConsoleCommand("css_reloadadmins")]
		[CommandHelper(whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		[RequiresPermissions("@css/root")]
		public void OnRelAdminCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			ReloadAdmins(caller);

			command.ReplyToCommand("Reloaded sql admins and groups");
		}

		public void ReloadAdmins(CCSPlayerController? caller)
		{
			if (_database == null) return;

			for (var index = 0; index < PermissionManager.AdminCache.Keys.ToList().Count; index++)
			{
				var steamId = PermissionManager.AdminCache.Keys.ToList()[index];
				if (!PermissionManager.AdminCache.TryRemove(steamId, out _)) continue;

				AdminManager.ClearPlayerPermissions(steamId);
				AdminManager.RemovePlayerAdminData(steamId);
			}

			PermissionManager adminManager = new(_database);

			Task.Run(async () =>
			{
				await adminManager.CrateGroupsJsonFile();
				await adminManager.CreateAdminsJsonFile();

				await Server.NextFrameAsync(() =>
				{
					AdminManager.LoadAdminData(ModuleDirectory + "/data/admins.json");
					AddTimer(1.0f, () => AdminManager.LoadAdminGroups(ModuleDirectory + "/data/groups.json"));
				});
			});

			//_ = _adminManager.GiveAllGroupsFlags();
			//_ = _adminManager.GiveAllFlags();
		}

		[ConsoleCommand("css_kick")]
		[RequiresPermissions("@css/kick")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnKickCommand(CCSPlayerController? caller, CommandInfo command)
		{
			
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			var targets = GetTarget(command);

			if (targets == null)
				return;

			var playersToTarget = targets.Players
				.Where(player => player is { IsValid: true, IsHLTV: false }).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			if (command.ArgCount >= 2 && command.GetArg(2).Length > 0)
				reason = command.GetArg(2);

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				if (caller!.CanTarget(player))
				{
					Kick(caller, player, reason, callerName, command);
				}
			});
		}

		public void Kick(CCSPlayerController? caller, CCSPlayerController? player, string? reason = "Unknown", string? callerName = null, CommandInfo? command = null)
		{
			if (player == null || !player.IsValid) return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			reason ??= _localizer?["sa_unknown"] ?? "Unknown";

			if (string.IsNullOrEmpty(reason) == false)
			{
				if (player != null && !player.IsBot)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_kick_message", reason, caller == null ? "Console" : caller.PlayerName]);
					}
				if (player != null && player.UserId.HasValue)
					AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value, reason),
						CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}
			else
			{
				if (player != null && player.UserId.HasValue)
					AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value),
						CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
			}

			if (caller != null && (caller.UserId == null || SilentPlayers.Contains(caller.Slot))) return;
			foreach (var controller in Helper.GetValidPlayers())
			{

				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_kick_message", callerName, player?.PlayerName ?? string.Empty, reason]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}

		[ConsoleCommand("css_rcon", "Run a server console command.")]
		[CommandHelper(1, "<command>")]
		[RequiresPermissions("@css/rcon")]
		public void OnRconCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;

			Server.ExecuteCommand(command.ArgString);
			command.ReplyToCommand($"{callerName} executed command {command.ArgString}.");
			Logger.LogInformation($"{callerName} executed command ({command.ArgString}).");
		}
	}
}