using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using System.Text;
using Vector = CounterStrikeSharp.API.Modules.Utils.Vector;

namespace CS2_SimplerAdmin;

public static class PlayerExtensions
{
	public static bool CanTarget(this CCSPlayerController? controller, CCSPlayerController? target)
	{
		if (target != null && target.IsBot) return true;
		if (controller is null) return true;

		return target != null && (AdminManager.CanPlayerTarget(controller, target) ||
		                          AdminManager.CanPlayerTarget(new SteamID(controller.SteamID),
			                          new SteamID(target.SteamID)));
	}
}