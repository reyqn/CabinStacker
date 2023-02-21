using System;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Buildings;

namespace CabinStacker.Helper
{
	internal static class CabinHelper {
		
        public static Building[] CabinsToMove;

        public static void Warp(Farmer farmer, GameLocation target)
		{
			if (farmer.Equals(Game1.player)) {
				Game1.warpFarmer(target.NameOrUniqueName, target.warps.First().X, target.warps.First().Y-1, false);
			}
			else {
				var message = new object[]
				{
					target.NameOrUniqueName,
					target.warps.First().X,
					target.warps.First().Y-1,
					true
				};
				Game1.server.sendMessage(farmer.UniqueMultiplayerID, 29, Game1.player, message.ToArray());
			}
		}

		public static void SendMessageToFarmer(Farmer farmer, string message) {
            if (farmer.Equals(Game1.player)) {
				Game1.chatBox.addInfoMessage(message);
			}
			else {
				Game1.chatBox.textBoxEnter("/message " + farmer.Name + " " + message);
			}
        }

        public static void AddNewCabin()
		{
			var types = new[] {"Stone Cabin", "Plank Cabin", "Log Cabin"};
			var blueprint = new BluePrint(types[new Random().Next(0, 3)]);

			var building = new Building(blueprint, new Vector2(1070, 18))
			{
				tilesWide =
				{
					Value = -1001
				},
				tilesHigh =
				{
					Value = -1
				},
				humanDoor =
				{
					Value = new Point(-1007, -4)
				}
			};
			Game1.getFarm().buildings.Add(building);
			building.load();
		}

		public static void MoveCabinsForWarpingEvent()
		{
			var currentEvent = Game1.CurrentEvent;
			var isWarpingEvent = currentEvent?.isFestival == true || currentEvent?.isWedding == true;
			if (CabinsToMove == null && isWarpingEvent) {
                CabinsToMove = Game1.getFarm().buildings.Where(o => o.tileX.Value > 1000).ToArray();
                foreach(var cabin in CabinsToMove) {
                    cabin.tileX.Value = 63;
                    cabin.tileY.Value = 17;
                }
            }
            else if (CabinsToMove != null && !isWarpingEvent)
            {
                foreach(var cabin in CabinsToMove) {
                    cabin.tileX.Value = 1070;
                    cabin.tileY.Value = 18;
                }
                CabinsToMove = null;
            }
		}
    }
}