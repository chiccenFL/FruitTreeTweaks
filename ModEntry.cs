using HarmonyLib;
using StardewValley;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using System;
using System.Collections;
using System.Security.Cryptography.X509Certificates;
using System.Linq;
using StardewValley.ItemTypeDefinitions;

/***
 * TO DO:
 * Fix min/max Fruit Per Day, and Fruit Per Day in general
 * Fix bug that causes Fruit Per Day values to multiply how fast a tree ages (e.g. min/max = 4 made tree age -3 the day after it was planted)
 * Fix draw so you can see more than 3 fruit
 * Fix chopped down fruit tree producing sapling of equal quality to the fruit that tree had produced.
 * Change how debug Logging works so toggling Debug in Config makes all Trace logs Debug logs instead.
 * For fun: see if you can change it so trees can be placed on top of paths
***/

namespace FruitTreeTweaks
{
    /// <summary>The mod entry point.</summary>
    public partial class ModEntry : Mod
    {

        public static IMonitor SMonitor;
        public static IModHelper SHelper;
        public static ModConfig Config;
        public static ModEntry context;
		

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            context = this;

            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

        }

		/// <summary>
		/// Small method that checks if Debug mode is enabled before Logging messages. A bit easier than writing an if/else every time.
		/// </summary>
		/// <param name="message"></param>
		/// <param name="level"></param>

		public static void Log(string message, LogLevel level = LogLevel.Trace)
		{
			if (Config.Debug) SMonitor.Log(message, level);
		}

		public static void LogOnce(string message, LogLevel level = LogLevel.Trace)
		{
			if (Config.Debug) SMonitor.LogOnce(message, level);
		}

		private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {

            // get Generic Mod Config Menu's API (if it's installed)
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu is null)
                return;

            // register mod
            configMenu.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => Helper.WriteConfig(Config)
            );

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Mod Enabled",
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Crops Block",
				tooltip: () => "Set true if you want crops to prevent growth",
                getValue: () => Config.CropsBlock,
                setValue: value => Config.CropsBlock = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Trees Block",
				tooltip: () => "Set true if you want other trees to prevent growth",
                getValue: () => Config.TreesBlock,
                setValue: value => Config.TreesBlock = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Objects Block",
				tooltip: () => "Set true if you want objects to prevent growth",
                getValue: () => Config.ObjectsBlock,
                setValue: value => Config.ObjectsBlock = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Plant Anywhere",
                tooltip: () => "Remove tile and map restrictions",
                getValue: () => Config.PlantAnywhere,
                setValue: value => Config.PlantAnywhere = value
            );
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => "Fruit All Seasons",
                tooltip: () => "Except winter, duh",
                getValue: () => Config.FruitAllSeasons,
                setValue: value => Config.FruitAllSeasons = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Max Fruit / Tree",
                getValue: () => Config.MaxFruitPerTree,
                setValue: value => Config.MaxFruitPerTree = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Days to Mature",
                getValue: () => Config.DaysUntilMature,
                setValue: value => Config.DaysUntilMature = value
            );
            /* edge-case bug where if a user changes these numbers, it causes the age to increase by odd factors. I had set min/max both to 4 and the planted a tree, and that tree was instantly 3 days old somehow.
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Min Fruit / Day",
                getValue: () => Config.MinFruitPerDay,
                setValue: value => Config.MinFruitPerDay = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Max Fruit / Day",
                getValue: () => Config.MaxFruitPerDay,
                setValue: value => Config.MaxFruitPerDay = value
            ); so yeah, keep this commented out until that bug is patched so users don't accidentally break their shit over a feature that doesn't even work right now anyway */
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Color Variation",
                tooltip: () => "0 - 255, applied randomly to R, B, and G for each fruit, only applied cosmetically while on tree",
                getValue: () => Config.ColorVariation,
                setValue: value => Config.ColorVariation = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Size Variation %",
                tooltip: () => "0 - 99, applied randomly for each fruit, only applied cosmetically while on tree",
                getValue: () => Config.SizeVariation,
                setValue: value => Config.SizeVariation = value,
                min:0,
                max:99
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Fruit Buffer X",
                tooltip: () => "Left and right border on the canopy to limit fruit spawn locations",
                getValue: () => Config.FruitSpawnBufferX,
                setValue: value => Config.FruitSpawnBufferX = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Fruit Buffer Y",
                tooltip: () => "Top and bottom border on the canopy to limit fruit spawn locations",
                getValue: () => Config.FruitSpawnBufferY,
                setValue: value => Config.FruitSpawnBufferY = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Days Until Silver",
                tooltip: () => "After fully mature",
                getValue: () => Config.DaysUntilSilverFruit,
                setValue: value => Config.DaysUntilSilverFruit = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Days Until Gold",
                tooltip: () => "After fully mature",
                getValue: () => Config.DaysUntilGoldFruit,
                setValue: value => Config.DaysUntilGoldFruit = value
            );
            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => "Days Until Iridium",
                tooltip: () => "After fully mature",
                getValue: () => Config.DaysUntilIridiumFruit,
                setValue: value => Config.DaysUntilIridiumFruit = value
            );
			configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => "Debug Outputs",
				tooltip: () => "Enable this for debugging or if generating SMAPI log for mod help or troubleshooting",
				getValue: () => Config.Debug,
				setValue: value => Config.Debug = value
			);
		}
    }
}