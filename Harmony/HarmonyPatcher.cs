using System;
using System.Collections.Generic;
using System.Linq;
using CabinStacker.ChatCommands;
using CabinStacker.Helper;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
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
		private static IModHelper _helper;

		internal static void Initialize(IMonitor monitor, IModHelper modHelper, string id)
		{
			_monitor = monitor;
			_helper = modHelper;
			var harmonyInstance = new HarmonyLib.Harmony(id);
			harmonyInstance.Patch(original: AccessTools.Method(typeof(GameServer), nameof(GameServer.sendAvailableFarmhands)), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(SendAvailableFarmhands_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(GameServer), nameof(GameServer.sendMessage), new[]{typeof(long), typeof(byte), typeof(Farmer), typeof(object[])}), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(SendMessage_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(Multiplayer), nameof(StardewValley.Multiplayer.broadcastLocationDelta)), prefix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(BroadcastLocationDelta_Prefix)));
			harmonyInstance.Patch(original: AccessTools.Method(typeof(Multiplayer), nameof(StardewValley.Multiplayer.processIncomingMessage)), postfix: new HarmonyMethod(typeof(HarmonyPatcher), nameof(ProcessIncomingMessage_Postfix)));
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
				_monitor.Log($"Failed in {nameof(SendMessage_Prefix)}:\n{e}", LogLevel.Error);
				return true;
			}
		}

		public static bool BroadcastLocationDelta_Prefix(GameLocation loc)
		{
			try {
				if (loc is not Farm farm)
					return true;

				if (loc.Root is null || !loc.Root.Dirty)
					return false;
				
				var dirtyDoors = farm.buildings.Where(o => o.isCabin && _helper.Reflection.GetField<List<INetSerializable>>(o.NetFields, "fields").GetValue()[10].Dirty).Select(o => o.nameOfIndoors).ToArray();
				if (dirtyDoors.Length != 1) 
					return true;
				
				var data = Multiplayer.writeObjectDeltaBytes(loc.Root);
				var message = new OutgoingMessage(6, Game1.player, false, loc.Name, data);
				void Action(Farmer f)
				{
					if (f == Game1.player) return;
					Game1.server.sendMessage(f.UniqueMultiplayerID, message);
				}

				var strData = Convert.ToHexString(data);
				var movingFarmer = strData.Contains("11FCFFFFFCFFFFFF") ? Game1.getAllFarmhands().Where(o => o.homeLocation.Value.Equals(dirtyDoors.FirstOrDefault())).ToArray() : Array.Empty<Farmer>();
				foreach (var farmer in Game1.otherFarmers.Values.Except(movingFarmer))
					Action(farmer);

				if (movingFarmer.Length != 1) return false;
				
				var doorIndex = strData.IndexOf("11FCFFFFFCFFFFFF", StringComparison.Ordinal)/2;
				data[doorIndex] = 18;
				data[doorIndex+1] = 252;
				data[doorIndex+2] = 255;
				data[doorIndex+3] = 255;	
				message = new OutgoingMessage(6, Game1.player, false, loc.Name, data);
				Action(movingFarmer.First());

				return false;
			}
			catch (Exception e) {
				_monitor.Log($"Failed in {nameof(BroadcastLocationDelta_Prefix)}:\n{e}", LogLevel.Error);
				return true;
			}
		}

		private static void ProcessIncomingMessage_Postfix(IncomingMessage msg)
        {
            try
            {
				if (msg.MessageType != 6) return;
				foreach(var cabin in Game1.getFarm().buildings.Where(o => o.isCabin)) {
					cabin.updateInteriorWarps();
				}
            }
			catch (Exception e)
			{
				_monitor.Log($"Failed in {nameof(ProcessIncomingMessage_Postfix)}:\n{e}", LogLevel.Error);
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
