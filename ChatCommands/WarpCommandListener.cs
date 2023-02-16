using System;
using System.Linq;
using CabinStacker.Helper;
using StardewValley;

namespace CabinStacker.ChatCommands
{
    internal abstract class WarpCommandListener
    {
        public static void ChatReceived(long sender, string message)
        {
            var tokens = message.ToLower().Split(' ');
            if (tokens.Length == 0)
            {
                return;
            }
            var farmer = Game1.getFarmer(sender);
            if (tokens.Length != 2)
            {
                CabinHelper.SendMessageToFarmer(farmer, "Usage: !warp [player_name]");
                return;
            }
            var farmerName = tokens[1];
            var targetFarmer = Game1.getAllFarmers().FirstOrDefault(o => string.Equals(o.Name, farmerName, StringComparison.OrdinalIgnoreCase));
            if (targetFarmer == null) {
                CabinHelper.SendMessageToFarmer(farmer, "You must enter an existing farmer's name");
                return;
            }
            if (farmer.currentLocation.Name != "Cabin" && farmer.currentLocation.Name != "FarmHouse") {
                CabinHelper.SendMessageToFarmer(farmer, "You must be home to warp");
                return;
            }

            CabinHelper.Warp(farmer, Game1.getLocationFromName(targetFarmer.homeLocation.Value));
        }
    }
}