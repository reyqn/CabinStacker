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
            HarmonyPatcher.Initialize(Monitor, helper, ModManifest.UniqueID);
            helper.Events.GameLoop.UpdateTicked += OnUpdate;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private static void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            //Those values aren't loaded from the save for some reason
            foreach (var cabin in Game1.getFarm().buildings.Where(o => o.tileX.Value > 1000)) {
                cabin.humanDoor.Value = new Point(-1007, -4);
            }
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