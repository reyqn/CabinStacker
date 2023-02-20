using System;
using System.Linq;
using CabinStacker.ChatCommands;
using CabinStacker.Helper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Menus;
using StardewValley.Network;

namespace CabinStacker.Harmony
{
	internal class HarmonyPatcher
	{
		private static IMonitor _monitor;

		internal static void Initialize(IMonitor monitor, string id)
		{
			_monitor = monitor;
			var harmonyInstance = new HarmonyLib.Harmony(id);
			harmonyInstance.Patch(original: AccessTools.Method(typeof(GameServer), nameof(GameServer.sendAvailableFarmhands)), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(SendAvailableFarmhands_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(GameServer), nameof(GameServer.sendMessage), new[]{typeof(long), typeof(byte), typeof(Farmer), typeof(object[])}), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(SendMessage3_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(GameServer), nameof(GameServer.sendMessage), new[]{typeof(long), typeof(OutgoingMessage)}), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(SendMessage6_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(Building), nameof(Building.updateInteriorWarps)), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(UpdateInteriorWarps_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(ChatBox), nameof(ChatBox.receiveChatMessage)), postfix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(ReceiveChatMessage_Postfix)));
		}

		public static bool SendAvailableFarmhands_Prefix() {
			try {
				var emptyPlaces = Game1.getAllFarmhands().Count(o => !o.isCustomized.Value && !o.isActive());

				if (emptyPlaces == 0) 
					CabinHelper.AddNewCabin();
			}
			catch (Exception e) {
				_monitor.Log($"Failed in {nameof(SendAvailableFarmhands_Prefix)}:\n{e}", LogLevel.Error);
			}
			return true;
		}

		private static readonly Multiplayer Multiplayer = new();

		public static bool SendMessage3_Prefix(
			long peerId,
			byte messageType,
			Farmer sourceFarmer,
			params object[] data)
		{
			try
			{
				if (messageType != 3 || data.Length != 2 || data[1] is not byte[]) return true;
			
				var strData = System.Text.Encoding.Default.GetString((byte[])data[1]);
				if (!strData.Contains(@"Maps\Farm" + char.MinValue)) return true;
			
				var clone = Game1.getFarm().Root.Clone();
				var farmer = Game1.getFarmer(peerId);

				foreach (var building in ((Farm)clone.Value).buildings.Where(o => o.nameOfIndoors.Equals(farmer.homeLocation.Value) && o.tileX.Value > 1000)) {
					building.humanDoor.Value = new Point(-1006, -4);
				}

				var obj = new object[]
				{
					false,
					Multiplayer.writeObjectFullBytes(clone, peerId)
				};
				Game1.server.sendMessage(peerId, new OutgoingMessage(messageType, sourceFarmer, obj));
				return false;
			}
			catch (Exception e)
			{
				_monitor.Log($"Failed in {nameof(SendMessage3_Prefix)}:\n{e}", LogLevel.Error);
				return true;
			}
		}

		public static bool SendMessage6_Prefix(
			long peerId,
			OutgoingMessage message)
		{
			try
			{
				if (ModEntry.MovingFarmer?.UniqueMultiplayerID != peerId || message.MessageType != 6 || message.Data.Count != 3 || message.Data[1].ToString() != "Farm") return true;

				var data = (byte[])message.Data[2];
				var strData = Convert.ToHexString(data);
				if (!strData.EndsWith("11FCFFFFFCFFFFFF")) return true;
				
				ModEntry.MovingFarmer = null;

				data[^32] = 64;
				data[^31] = 0;
				data[^30] = 0;
				data[^29] = 0;

				data[^8] = 18;
				data[^7] = 252;
				data[^6] = 255;
				data[^5] = 255;

				var obj = new[]
				{
					message.Data[0],
					message.Data[1],
					data
				};

				Game1.server.sendMessage(peerId, new OutgoingMessage(message.MessageType, message.FarmerID, obj));
				return false;
			}
			catch (Exception e)
			{
				_monitor.Log($"Failed in {nameof(SendMessage6_Prefix)}:\n{e}", LogLevel.Error);
				return true;
			}
		}

		public static bool UpdateInteriorWarps_Prefix(Building __instance, GameLocation interior = null)
		{
			try
			{
				var isStackedCabin = __instance.tileX.Value > 1000;
				interior ??= __instance.indoors.Value;
				if (interior == null)
					return false;
				foreach (var warp in interior.warps)
				{
					warp.TargetX = isStackedCabin ? 64 : __instance.humanDoor.X +  __instance.tileX.Value;
					warp.TargetY = isStackedCabin ? 15 : __instance.humanDoor.Y +  __instance.tileY.Value + 1;
				}
				return false;
			}
			catch (Exception e)
			{
				_monitor.Log($"Failed in {nameof(UpdateInteriorWarps_Prefix)}:\n{e}", LogLevel.Error);
				return true;
			}
		}

        internal static void ReceiveChatMessage_Postfix(long sourceFarmer, string message)
        {
			try
			{
				switch (message.Split(' ').FirstOrDefault()) {
					case "!move":
						MoveCommandListener.ChatReceived(sourceFarmer);
						break;
					case "!warp":
						WarpCommandListener.ChatReceived(sourceFarmer, message);
						break;
				}
			}
			catch (Exception e)
			{
				_monitor.Log($"Failed in {nameof(ReceiveChatMessage_Postfix)}:\n{e}", LogLevel.Error);
			}
        }
    }
}
