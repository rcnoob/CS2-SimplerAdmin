using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Entities.Constants;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Utils;
using System.Text;

namespace CS2_SimplerAdmin
{
	public partial class CS2_SimplerAdmin
	{

		[ConsoleCommand("css_give")]
		[RequiresPermissions("@css/cheats")]
		[CommandHelper(minArgs: 2, usage: "<#userid or name> <weapon>", whoCanExecute: CommandUsage.CLIENT_AND_SERVER)]
		public void OnGiveCommand(CCSPlayerController? caller, CommandInfo command)
		{
			var callerName = caller == null ? "Console" : caller.PlayerName;
			var targets = GetTarget(command);
			if (targets == null) return;

			var playersToTarget = targets.Players.Where(player => player.IsValid && player is { PawnIsAlive: true, IsHLTV: false }).ToList();
			var weaponName = command.GetArg(2);

			// check if item is typed
			if (weaponName.Length < 5)
			{
				command.ReplyToCommand($"No weapon typed.");
				return;
			}

			// check if item is valid
			if (!weaponName.Contains("weapon_") && !weaponName.Contains("item_"))
			{
				command.ReplyToCommand($"{weaponName} is not a valid item.");
				return;
			}

			// check if weapon is knife
			if (weaponName.Contains("_knife") || weaponName.Contains("bayonet"))
			{
				if (CoreConfig.FollowCS2ServerGuidelines)
				{
					command.ReplyToCommand($"Cannot Give {weaponName} because it's illegal to be given.");
					return;
				}
			}

			playersToTarget.ForEach(player =>
			{
				if (!player.IsBot && player.SteamID.ToString().Length != 17)
					return;

				GiveWeapon(caller, player, weaponName, callerName, command);
			});
		}

		public void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, CsItem weapon, string? callerName = null)
		{
			player.GiveNamedItem(weapon);
			SubGiveWeapon(caller, player, weapon.ToString(), callerName);
		}

		private void GiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null, CommandInfo? command = null)
		{
			player.GiveNamedItem(weaponName);
			SubGiveWeapon(caller, player, weaponName, callerName);
		}

		private void SubGiveWeapon(CCSPlayerController? caller, CCSPlayerController player, string weaponName, string? callerName = null)
		{
			callerName ??= caller == null ? "Console" : caller.PlayerName;

			if (caller != null && (SilentPlayers.Contains(caller.Slot))) return;
			foreach (var controller in Helper.GetValidPlayers())
			{
				using (new WithTemporaryCulture(controller.GetLanguage()))
				{
					StringBuilder sb = new(_localizer!["sa_prefix"]);
					sb.Append(_localizer["sa_admin_give_message", callerName, player.PlayerName, weaponName]);
					controller.PrintToChat(sb.ToString());
				}
			}
		}
	}
}