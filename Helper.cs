using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Admin;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Entities;
using CounterStrikeSharp.API.Modules.Memory;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CS2_SimplerAdmin
{
	internal class Helper
	{
		private static readonly string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name ?? "";
		private static readonly string CfgPath = $"{Server.GameDirectory}/csgo/addons/counterstrikesharp/configs/plugins/{AssemblyName}/{AssemblyName}.json";

		internal static CS2_SimplerAdminConfig? Config { get; set; }

		public static bool IsDebugBuild
		{
			get
			{
				#if DEBUG
				        return true;
				#else
						return false;
				#endif
			}
		}
		
		public static List<CCSPlayerController> GetPlayerFromName(string name)
		{
			return Utilities.GetPlayers().FindAll(x => x.PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase));
		}

		public static List<CCSPlayerController> GetPlayerFromSteamid64(string steamid)
		{
			return GetValidPlayers().FindAll(x =>
				x.SteamID.ToString().Equals(steamid, StringComparison.OrdinalIgnoreCase)
			);
		}

		public static List<CCSPlayerController> GetPlayerFromIp(string ipAddress)
		{
			return GetValidPlayers().FindAll(x =>
				x.IpAddress != null &&
				x.IpAddress.Split(":")[0].Equals(ipAddress)
			);
		}

		public static List<CCSPlayerController> GetValidPlayers()
		{
			return Utilities.GetPlayers().FindAll(p => p is
					{ IsBot: false, IsHLTV: false });
		}

		public static IEnumerable<CCSPlayerController?> GetValidPlayersWithBots()
		{
			return Utilities.GetPlayers().FindAll(p =>
			 p is { IsBot: false, IsHLTV: false } or { IsBot: true, IsHLTV: false }
			);
		}

		public static bool IsValidSteamId64(string input)
		{
			const string pattern = @"^\d{17}$";
			return Regex.IsMatch(input, pattern);
		}

		public static bool IsValidIp(string input)
		{
			const string pattern = @"^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$";
			return Regex.IsMatch(input, pattern);
		}

		public static void GivePlayerFlags(SteamID? steamid, List<string>? flags = null, uint immunity = 0)
		{
			try
			{
				if (steamid == null || (flags == null && immunity == 0))
				{
					return;
				}

				if (flags == null) return;
				foreach (var flag in flags.Where(flag => !string.IsNullOrEmpty(flag)))
				{
					if (flag.StartsWith($"@"))
					{
						//Console.WriteLine($"Adding permission {flag} to SteamID {steamid}");
						AdminManager.AddPlayerPermissions(steamid, flag);
					}
					else if (flag.StartsWith($"#"))
					{
						//Console.WriteLine($"Adding SteamID {steamid} to group {flag}");
						AdminManager.AddPlayerToGroup(steamid, flag);
					}
				}

				AdminManager.SetPlayerImmunity(steamid, immunity);
			}
			catch
			{
			}
		}

		public static void KickPlayer(int userId, string? reason = null)
		{
			if (!string.IsNullOrEmpty(reason))
			{
				var escapeChars = reason.IndexOfAny([';', '|']);

				if (escapeChars != -1)
				{
					reason = reason[..escapeChars];
				}
			}

			Server.ExecuteCommand($"kickid {userId} {reason}");
		}

		public static void PrintToCenterAll(string message)
		{
			Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(controller =>
			{
				controller.PrintToCenter(message);
			});
		}

		public enum PenaltyType
		{
			Ban,
			Mute,
			Gag,
			Silence,
		}

		private static string ConvertMinutesToTime(int minutes)
		{
			var time = TimeSpan.FromMinutes(minutes);

			return time.Days > 0 ? $"{time.Days}d {time.Hours}h {time.Minutes}m" : time.Hours > 0 ? $"{time.Hours}h {time.Minutes}m" : $"{time.Minutes}m";
		}

		public static void UpdateConfig<T>(T config) where T : BasePluginConfig, new()
		{
			// get newest config version
			var newCfgVersion = new T().Version;

			// loaded config is up to date
			if (config.Version == newCfgVersion)
				return;

			// update the version
			config.Version = newCfgVersion;

			// serialize the updated config back to json
			var updatedJsonContent = JsonSerializer.Serialize(config,
				new JsonSerializerOptions
				{
					WriteIndented = true,
					Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
				});
			File.WriteAllText(CfgPath, updatedJsonContent);
		}
	}

	public static class PluginInfo
	{
		internal static async Task CheckVersion(string version, ILogger logger)
		{
			using HttpClient client = new();

			try
			{
				var response = await client.GetAsync("https://raw.githubusercontent.com/rcnoob/CS2-SimplerAdmin/main/VERSION").ConfigureAwait(false);

				if (response.IsSuccessStatusCode)
				{
					var remoteVersion = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
					remoteVersion = remoteVersion.Trim();

					var comparisonResult = string.CompareOrdinal(version, remoteVersion);

					switch (comparisonResult)
					{
						case < 0:
							logger.LogWarning("Plugin is outdated! Check https://github.com/rcnoob/CS2-SimplerAdmin");
							break;
						case > 0:
							logger.LogInformation("Probably dev version detected");
							break;
						default:
							logger.LogInformation("Plugin is up to date");
							break;
					}
				}
				else
				{
					logger.LogWarning("Failed to check version");
				}
			}
			catch (HttpRequestException ex)
			{
				logger.LogError(ex, "Failed to connect to the version server.");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "An error occurred while checking version.");
			}
		}

		internal static void ShowAd(string moduleVersion)
		{
			Console.WriteLine(" ");
			Console.WriteLine(" _______  ___   __   __  _______  ___      _______  _______  ______   __   __  ___   __    _  ");
			Console.WriteLine("|       ||   | |  |_|  ||       ||   |    |       ||   _   ||      | |  |_|  ||   | |  |  | |");
			Console.WriteLine("|  _____||   | |       ||    _  ||   |    |    ___||  |_|  ||  _    ||       ||   | |   |_| |");
			Console.WriteLine("| |_____ |   | |       ||   |_| ||   |    |   |___ |       || | |   ||       ||   | |       |");
			Console.WriteLine("|_____  ||   | |       ||    ___||   |___ |    ___||       || |_|   ||       ||   | |  _    |");
			Console.WriteLine(" _____| ||   | | ||_|| ||   |    |       ||   |___ |   _   ||       || ||_|| ||   | | | |   |");
			Console.WriteLine("|_______||___| |_|   |_||___|    |_______||_______||__| |__||______| |_|   |_||___| |_|  |__|");
			Console.WriteLine("				>> Version: " + moduleVersion);
			Console.WriteLine("		>> GitHub: https://github.com/rcnoob/CS2-SimplerAdmin");
			Console.WriteLine(" ");
		}
	}

	public class SchemaString<TSchemaClass>(TSchemaClass instance, string member)
		: NativeObject(Schema.GetSchemaValue<nint>(instance.Handle, typeof(TSchemaClass).Name, member))
		where TSchemaClass : NativeObject
	{
		public unsafe void Set(string str)
		{
			var bytes = GetStringBytes(str);

			for (var i = 0; i < bytes.Length; i++)
			{
				Unsafe.Write((void*)(Handle.ToInt64() + i), bytes[i]);
			}

			Unsafe.Write((void*)(Handle.ToInt64() + bytes.Length), 0);
		}

		private static byte[] GetStringBytes(string str)
		{
			return Encoding.UTF8.GetBytes(str);
		}
	}
}