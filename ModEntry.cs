using System.Linq;
using CabinStacker.Harmony;
using CabinStacker.Helper;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace CabinStacker
{
    public class ModEntry : Mod
    {
        public override void Entry(IModHelper helper)
        {
            HarmonyPatcher.Initialize(Monitor, ModManifest.UniqueID);
            helper.Events.GameLoop.UpdateTicked += OnUpdate;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            //Those values aren't loaded from the save for some reason
            foreach (var cabin in Game1.getFarm().buildings.Where(o => o.tileX.Value > 1000)) {
                cabin.humanDoor.Value = new Point(-1006, -4);
            }

            var farmhouseWarp = Game1.getLocationFromName("FarmHouse").warps.First();
            Game1.getFarm().warps.Add(new Warp(64, 14, Game1.player.homeLocation.Value, farmhouseWarp.X, farmhouseWarp.Y - 1, false));
        }

        private static void OnUpdate(object sender, UpdateTickedEventArgs e)
        {
            // ignore if player hasn't loaded a save yet
            if (!Context.IsWorldReady)
                return;

            CabinHelper.MoveCabinsForWarpingEvent();
        }
    }
}