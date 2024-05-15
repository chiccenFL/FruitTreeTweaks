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
 * Fix draw so you can see more than 3 fruit
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

        private static int attempts = 0;
		

        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            Config = Helper.ReadConfig<ModConfig>();

            I18n.Init(helper.Translation);

            context = this;

            SMonitor = Monitor;
            SHelper = helper;

            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;
            helper.Events.GameLoop.SaveLoaded += GameLoop_SaveLoaded;

            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

        }

        /// <summary>
		///     Small method that handles Debug mode to make SMAPI logs a bit easier to read.
		/// </summary>
        /// <remarks>
        ///     Allows basic Log functions to upgrade Logs to <see cref="LogLevel.Debug"/> when debugging for ease of reading.<br/>
        ///     For <b>Debug Only</b> Logs -- use <c>debugOnly: true</c> and omit <see cref="LogLevel"/><br/>
        ///     For Debug Logs that <b>always</b> show -- use <see cref="LogLevel"/> and omit <c>debugOnly</c>.
        /// </remarks>
		/// <param name="message"></param>
		/// <param name="level"></param>

        public static void Log(string message, LogLevel level = LogLevel.Trace, bool debugOnly = false)
		{
			level = Config.Debug ? LogLevel.Debug : level;
            if (!debugOnly) SMonitor.Log(message, level); 
            else if (debugOnly && Config.Debug) SMonitor.Log(message, level);
            else return;
		}

        /// <summary>
		///     Small method that handles Debug mode to make SMAPI logs a bit easier to read.
		/// </summary>
        /// <remarks>
        ///     Allows basic Log functions to upgrade Logs to <see cref="LogLevel.Debug"/> when debugging for ease of reading.<br/>
        ///     For <b>Debug Only</b> Logs -- use <c>debugOnly: true</c> and omit <see cref="LogLevel"/><br/>
        ///     For Debug Logs that <b>always</b> show -- use <see cref="LogLevel"/> and omit <c>debugOnly</c>.
        /// </remarks>
		/// <param name="message"></param>
		/// <param name="level"></param>
		public static void LogOnce(string message, LogLevel level = LogLevel.Trace, bool debugOnly = false)
		{
            level = Config.Debug ? LogLevel.Debug : level;
            if (!debugOnly) SMonitor.LogOnce(message, level);
            if (debugOnly && Config.Debug) SMonitor.LogOnce(message, level);
            else return;
        }

		private void GameLoop_GameLaunched(object sender, GameLaunchedEventArgs e)
        {

            Log("Fruit Tree Tweaks launching with Debug enabled.", debugOnly: true);

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
                name: () => I18n.EnableMod(),
                getValue: () => Config.EnableMod,
                setValue: value => Config.EnableMod = value
            );
            Log($"Mod enabled: {Config.EnableMod}", debugOnly: true);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.CropsBlock(),
				tooltip: () => I18n.CropsBlock_1(),
                getValue: () => Config.CropsBlock,
                setValue: value => Config.CropsBlock = value
            );
            Log($"Crops block: {Config.CropsBlock}", debugOnly: true);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.TreesBlock(),
				tooltip: () => I18n.TreesBlock_1(),
                getValue: () => Config.TreesBlock,
                setValue: value => Config.TreesBlock = value
            );
            Log($"Trees block: {Config.TreesBlock}", debugOnly: true);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.ObjectsBlock(),
				tooltip: () => I18n.ObjectsBlock_1(),
                getValue: () => Config.ObjectsBlock,
                setValue: value => Config.ObjectsBlock = value
            );
            Log($"Objects block: {Config.ObjectsBlock}", debugOnly: true);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.PlantAnywhere(),
                tooltip: () => I18n.PlantAnywhere_1(),
                getValue: () => Config.PlantAnywhere,
                setValue: value => Config.PlantAnywhere = value
            );
            Log($"Plant Anywhere: {Config.PlantAnywhere}", debugOnly: true);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.PlantOnPaths(),
                tooltip: () => I18n.PlantOnPaths_1(),
                getValue: () => Config.PlantOnPaths,
                setValue: value => Config.PlantOnPaths = value
            );
            Log($"Plant On Paths: {Config.PlantOnPaths}", debugOnly: true);

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.FruitAllSeasons(),
                tooltip: () => I18n.FruitAllSeasons_1(),
                getValue: () => Config.FruitAllSeasons,
                setValue: value => Config.FruitAllSeasons = value
            );
            Log($"Fruit All Seasons: {Config.FruitAllSeasons}", debugOnly: true); // plant on paths currently not working, so comment this out if you can't get it working

            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.FruitInWinter(),
                tooltip: () => I18n.FruitInWinter_1(),
                getValue: () => Config.FruitInWinter,
                setValue: value => Config.FruitInWinter = value
            );
            Log($"Fruit In Winter: {Config.FruitInWinter}", debugOnly: true);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.MaxFruitTree(),
                getValue: () => Config.MaxFruitPerTree,
                setValue: value => Config.MaxFruitPerTree = value
            );
            Log($"Max Fruit / Tree: {Config.MaxFruitPerTree}", debugOnly: true);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.DaysUntilMature(),
                getValue: () => Config.DaysUntilMature,
                setValue: value => Config.DaysUntilMature = value
            );
            Log($"Days to Mature: {Config.DaysUntilMature}", debugOnly: true);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.MinFruitDay(),
                tooltip: () => I18n.MinFruitDay_1(),
                getValue: () => Config.MinFruitPerDay,
                setValue: value => Config.MinFruitPerDay = value
            );
            Log($"Min Fruit / Day: {Config.MinFruitPerDay}", debugOnly: true);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.MaxFruitDay(),
                tooltip: () => I18n.MaxFruitDay_1(),
                getValue: () => Config.MaxFruitPerDay,
                setValue: value => Config.MaxFruitPerDay = value
            );
            Log($"Max Fruit / Day: {Config.MaxFruitPerDay}", debugOnly: true);
            
            configMenu.AddNumberOption( // just setting this comment here in case i need to comment this all out again and need to find where the first draw() option was lol
                mod: ModManifest,
                name: () => "Color Variation",
                tooltip: () => "0 - 255, applied randomly to R, B, and G for each fruit, only applied cosmetically while on tree",
                getValue: () => Config.ColorVariation,
                setValue: value => Config.ColorVariation = value
            );
            Log($"Color Variation: {Config.EnableMod}", debugOnly: true);
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
            ); //these last 4 are just out because FruitTree.draw_Patch isn't working right now anyway, so it's a bit silly being there when it does nothing atm. */

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.DaysUntilSilver(),
                tooltip: () => I18n.DaysUntilTip(),
                getValue: () => Config.DaysUntilSilverFruit,
                setValue: value => Config.DaysUntilSilverFruit = value
            );
            Log($"Days until Silver: {Config.DaysUntilSilverFruit}", debugOnly: true);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.DaysUntilGold(),
                tooltip: () => I18n.DaysUntilTip(),
                getValue: () => Config.DaysUntilGoldFruit,
                setValue: value => Config.DaysUntilGoldFruit = value
            );
            Log($"Days until Gold: {Config.DaysUntilGoldFruit}", debugOnly: true);

            configMenu.AddNumberOption(
                mod: ModManifest,
                name: () => I18n.DaysUntilIridium(),
                tooltip: () => I18n.DaysUntilTip(),
                getValue: () => Config.DaysUntilIridiumFruit,
                setValue: value => Config.DaysUntilIridiumFruit = value
            );
            Log($"Days until Iridium: {Config.DaysUntilIridiumFruit}", debugOnly: true);

            configMenu.AddBoolOption(
				mod: ModManifest,
				name: () => I18n.Debug(),
				tooltip: () => I18n.Debug_1(),
				getValue: () => Config.Debug,
				setValue: value => Config.Debug = value
			);
            Log($"Debug: Well you're reading this, aren't you?", debugOnly: true); // xaxaxa
            /* future feature
            configMenu.AddBoolOption(
                mod: ModManifest,
                name: () => I18n.GodMode(),
                tooltip: () => I18n.GodMode_1(),
                getValue: () => Config.GodMode,
                setValue: value => Config.GodMode = value
            );
            Log($"God Mode: {Config.GodMode}", debugOnly: true);
            */
        }

        private void GameLoop_SaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            fruitToday = GetFruitPerDay(); // this breaks if it is anywhere else so dont move it
        }
    }
}