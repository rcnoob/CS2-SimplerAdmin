using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using System.Text;

namespace CS2_SimplerAdmin
{
	public partial class CS2_SimplerAdmin
	{
		[ConsoleCommand("css_ban")]
		[RequiresPermissions("@css/ban")]
		[CommandHelper(minArgs: 1, usage: "<#userid or name> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnBanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.ArgCount < 2)
				return;

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			var targets = GetTarget(command);
			if (targets == null) return;
			var playersToTarget = targets.Players.Where(player => player.IsValid && player.SteamID.ToString().Length == 17 && !player.IsHLTV).ToList();

			if (playersToTarget.Count > 1 && Config.DisableDangerousCommands || playersToTarget.Count == 0)
			{
				return;
			}

			Database.Database database = new(_dbConnectionString);
			BanManager banManager = new(database, Config);

			int.TryParse(command.GetArg(2), out var time);

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			playersToTarget.ForEach(player =>
			{
				if (caller!.CanTarget(player))
				{
					Ban(caller, player, time, reason, callerName, banManager, command);
				}
			});
		}

		internal void Ban(CCSPlayerController? caller, CCSPlayerController? player, int time, string reason, string? callerName = null, BanManager? banManager = null, CommandInfo? command = null)
		{
			if (_database == null || player is null || !player.IsValid) return;

			if (CheckValidBan(caller, time) == false)
				return;

			callerName ??= caller == null ? "Console" : caller.PlayerName;

			PlayerInfo playerInfo = new()
			{
				SteamId = player.SteamID.ToString(),
				Name = player.PlayerName,
				IpAddress = player.IpAddress?.Split(":")[0]
			};

			PlayerInfo adminInfo = new()
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			Task.Run(async () =>
			{
				banManager ??= new BanManager(_database, Config);
				await banManager.BanPlayer(playerInfo, adminInfo, reason, time);
			});

			if (player.UserId.HasValue)
				AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value),
					CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

			if (playerInfo.IpAddress != null && !BannedPlayers.Contains(playerInfo.IpAddress))
				BannedPlayers.Add(playerInfo.IpAddress);
			if (!BannedPlayers.Contains(player.SteamID.ToString()))
				BannedPlayers.Add(player.SteamID.ToString());

			if (time == 0)
			{
				if (!player.IsBot && !player.IsHLTV)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
					}

				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_ban_message_perm", callerName, player.PlayerName, reason]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}
			else
			{
				if (!player.IsBot && !player.IsHLTV)
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						player.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
					}
				if (caller == null || !SilentPlayers.Contains(caller.Slot))
				{
					foreach (var controller in Helper.GetValidPlayers())
					{
						using (new WithTemporaryCulture(controller.GetLanguage()))
						{
							StringBuilder sb = new(_localizer!["sa_prefix"]);
							sb.Append(_localizer["sa_admin_ban_message_time", callerName, player.PlayerName, reason, time]);
							controller.PrintToChat(sb.ToString());
						}
					}
				}
			}
		}

		[ConsoleCommand("css_addban")]
		[RequiresPermissions("@css/ban")]
		[CommandHelper(minArgs: 1, usage: "<steamid> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnAddBanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			var callerName = caller == null ? "Console" : caller.PlayerName;
			if (command.ArgCount < 2)
				return;
			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			var steamid = command.GetArg(1);

			if (!Helper.IsValidSteamId64(steamid))
			{
				command.ReplyToCommand($"Invalid SteamID64.");
				return;
			}

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			int.TryParse(command.GetArg(2), out var time);

			if (CheckValidBan(caller, time) == false)
				return;

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			var matches = Helper.GetPlayerFromSteamid64(steamid);
			if (matches.Count == 1)
			{
				var player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (!caller!.CanTarget(player))
					{
						command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
						return;
					}

					if (player.UserId.HasValue)
						AddTimer(Config.KickTime, () => Helper.KickPlayer(player.UserId.Value), CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);

					if (time == 0)
					{
						if (!player.IsBot && !player.IsHLTV)
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_perm", callerName, player.PlayerName, reason]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_time", callerName, player.PlayerName, reason, time]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
				}
			}

			Task.Run(async () =>
			{
				BanManager banManager = new(_database, Config);
				await banManager.AddBanBySteamid(steamid, adminInfo, reason, time);
			});

			command.ReplyToCommand($"Banned player with steamid {steamid}.");
		}

		[ConsoleCommand("css_banip")]
		[RequiresPermissions("@css/ban")]
		[CommandHelper(minArgs: 1, usage: "<ip> [time in minutes/0 perm] [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnBanIp(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;
			var callerName = caller == null ? "Console" : caller.PlayerName;

			if (command.ArgCount < 2)
				return;
			if (string.IsNullOrEmpty(command.GetArg(1))) return;

			var ipAddress = command.GetArg(1);

			if (!Helper.IsValidIp(ipAddress))
			{
				command.ReplyToCommand($"Invalid IP address.");
				return;
			}

			var reason = _localizer?["sa_unknown"] ?? "Unknown";

			var adminInfo = new PlayerInfo
			{
				SteamId = caller?.SteamID.ToString(),
				Name = caller?.PlayerName,
				IpAddress = caller?.IpAddress?.Split(":")[0]
			};

			int.TryParse(command.GetArg(2), out var time);
			if (CheckValidBan(caller, time) == false)
				return;

			if (command.ArgCount >= 3 && command.GetArg(3).Length > 0)
				reason = command.GetArg(3);

			var matches = Helper.GetPlayerFromIp(ipAddress);
			if (matches.Count == 1)
			{
				var player = matches.FirstOrDefault();
				if (player != null && player.IsValid)
				{
					if (!caller!.CanTarget(player))
					{
						command.ReplyToCommand($"{player.PlayerName} is more powerful than you!");
						return;
					}

					if (time == 0)
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_ban_message_perm", reason, caller == null ? "Console" : caller.PlayerName]);
							}

						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_perm", callerName, player.PlayerName, reason]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}
					else
					{
						if (player is { IsBot: false, IsHLTV: false })
							using (new WithTemporaryCulture(player.GetLanguage()))
							{
								player.PrintToCenter(_localizer!["sa_player_ban_message_time", reason, time, caller == null ? "Console" : caller.PlayerName]);
							}
						if (caller == null || !SilentPlayers.Contains(caller.Slot))
						{
							foreach (var controller in Helper.GetValidPlayers())
							{
								using (new WithTemporaryCulture(controller.GetLanguage()))
								{
									StringBuilder sb = new(_localizer!["sa_prefix"]);
									sb.Append(_localizer["sa_admin_ban_message_time", callerName, player.PlayerName, reason, time]);
									controller.PrintToChat(sb.ToString());
								}
							}
						}
					}

					if (player.UserId.HasValue)
					{
						AddTimer(Config.KickTime, () =>
						{
							Helper.KickPlayer(player.UserId.Value, "Banned");
						}, CounterStrikeSharp.API.Modules.Timers.TimerFlags.STOP_ON_MAPCHANGE);
					}
				}
			}

			Task.Run(async () =>
			{
				BanManager banManager = new(_database, Config);
				await banManager.AddBanByIp(ipAddress, adminInfo, reason, time);
			});

			command.ReplyToCommand($"Banned player with IP address {ipAddress}.");
		}

		private bool CheckValidBan(CCSPlayerController? caller, int duration)
		{
			if (caller == null) return true;

			bool canPermBan = AdminManager.PlayerHasPermissions(caller, "@css/permban");

			if (duration == 0 && canPermBan == false)
			{
				caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_perm_restricted"]}");
				return false;
			}

			if (duration > Config.MaxBanDuration && canPermBan == false)
			{
				caller.PrintToChat($"{_localizer!["sa_prefix"]} {_localizer["sa_ban_max_duration_exceeded", Config.MaxBanDuration]}");
				return false;
			}

			return true;
		}

		[ConsoleCommand("css_unban")]
		[RequiresPermissions("@css/unban")]
		[CommandHelper(minArgs: 1, usage: "<steamid or name or ip> [reason]", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnUnbanCommand(CCSPlayerController? caller, CommandInfo command)
		{
			if (_database == null) return;

			var callerSteamId = caller?.SteamID.ToString() ?? "Console";

			if (command.GetArg(1).Length <= 1)
			{
				command.ReplyToCommand($"Too short pattern to search.");
				return;
			}

			var pattern = command.GetArg(1);
			var reason = command.GetArg(2);

			BanManager banManager = new(_database, Config);
			Task.Run(async () => await banManager.UnbanPlayer(pattern, callerSteamId, reason));

			command.ReplyToCommand($"Unbanned player with pattern {pattern}.");
		}
	}
}