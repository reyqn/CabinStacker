using System.Linq;
using CabinStacker.Helper;
using Microsoft.Xna.Framework;
using StardewValley;

namespace CabinStacker.ChatCommands
{
    internal abstract class MoveCommandListener
    {
        public static void ChatReceived(long sender)
        {
            var farmer = Game1.getFarmer(sender);
            if (!Game1.getFarm().Equals(farmer.currentLocation)) {
                CabinHelper.SendMessageToFarmer(farmer, "You must be on the farm to move your cabin");
                return;
            }
            var cabin = Game1.getFarm().buildings.FirstOrDefault(o => o.nameOfIndoors.Equals(farmer.homeLocation.Value));
            if (cabin == null) {
                CabinHelper.SendMessageToFarmer(farmer, "You cannot move the FarmHouse");
                return;
            }
            var isStackedCabin = cabin.tileX.Value > 1000;
            cabin.humanDoor.Value = isStackedCabin ? new Point(2, 1) : new Point(-1006, -4);
            cabin.tilesWide.Value = isStackedCabin ? 5 : -1001;
            cabin.tilesHigh.Value = isStackedCabin ? 3 : -1;
            Game1.getFarm().buildStructure(cabin, new Vector2(isStackedCabin ? farmer.getTileX()-2 : 1070, isStackedCabin ? farmer.getTileY()-2 : 18), farmer, true);
        }


    }
}