using System;
using System.Linq;
using CabinStacker.ChatCommands;
using CabinStacker.Helper;
using HarmonyLib;
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
			harmonyInstance.Patch(original: AccessTools.Method(typeof(GameServer), nameof(GameServer.sendMessage), new[]{typeof(long), typeof(byte), typeof(Farmer), typeof(object[])}), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(SendMessage_Prefix)));
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

		public static bool SendMessage_Prefix(
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
				var farmerHouseLocation = Game1.getLocationFromName(farmer.homeLocation.Value);

				using var enumerator = clone.Value.warps.Where(o => o.TargetName.Equals("FarmHouse")).GetEnumerator();
				while (enumerator.MoveNext())
				{
					clone.Value.warps.Remove(enumerator.Current);
				}
				var newWarp = new Warp(64, 14, farmer.homeLocation.Value, farmerHouseLocation.warps.First().X, farmerHouseLocation.warps.First().Y - 1, false);
				clone.Value.warps.Add(newWarp);

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
				_monitor.Log($"Failed in {nameof(SendMessage_Prefix)}:\n{e}", LogLevel.Error);
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
				switch (message.Split('!').LastOrDefault()?.Split(' ').FirstOrDefault()) {
					case "move":
						MoveCommandListener.ChatReceived(sourceFarmer);
						break;
					case "warp":
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
