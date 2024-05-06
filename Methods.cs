﻿using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FruitTreeTweaks
{
    public partial class ModEntry
    {
        private static Dictionary<GameLocation, Dictionary<Vector2, List<Vector2>>> fruitOffsets = new Dictionary<GameLocation, Dictionary<Vector2, List<Vector2>>>();
        private static Dictionary<GameLocation, Dictionary<Vector2, List<Color>>> fruitColors = new Dictionary<GameLocation, Dictionary<Vector2, List<Color>>>();
        private static Dictionary<GameLocation, Dictionary<Vector2, List<float>>> fruitSizes = new Dictionary<GameLocation, Dictionary<Vector2, List<float>>>();

        private static float GetTreeBottomOffset(FruitTree tree)
        {
            if (!Config.EnableMod)
                return 1E-07f;
            return 1E-07f + Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key.X / 100000f;
        }
        private static bool TreesBlock()
        {
            return Config.TreesBlock;
        }
        private static bool FruitTreesBlock()
        {
            return Config.TreesBlock;
        }
        private static bool CanPlantAnywhere()
        {
            return Config.PlantAnywhere;
        }
        private static int GetMaxFruit()
        {
			return !Config.EnableMod ? 3 : Config.MaxFruitPerTree;
        }
        private static int GetFruitPerDay()
        {
            return !Config.EnableMod ? 1 : Game1.random.Next(Config.MinFruitPerDay, Math.Max(Config.MinFruitPerDay, Config.MaxFruitPerDay + 1));
        }
        private static void TryAddMoreFruit(FruitTree __instance)
        {
            int fruitDay = GetFruitPerDay();
            for (int i = 1; i < fruitDay; i++)
            {
                if (!__instance.TryAddFruit()) {
                    return;
                }
            }
        }
        private static Color GetFruitColor(FruitTree tree, int index)
        {
            if (!Config.EnableMod)
                return Color.White;
            if (!fruitColors.TryGetValue(tree.Location, out Dictionary<Vector2, List<Color>> dict) || !dict.TryGetValue(Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key, out List<Color> colors) || colors.Count < tree.fruit.Count)
                ReloadFruit(tree.Location, Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key, tree.fruit.Count);
            return fruitColors[tree.Location][Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key][index];
        }
        private static float GetFruitScale(FruitTree tree, int index)
        {
            if (!Config.EnableMod)
                return 4;
            if (!fruitSizes.TryGetValue(tree.Location, out Dictionary<Vector2, List<float>> dict) || !dict.TryGetValue(Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key, out List<float> sizes) || sizes.Count < tree.fruit.Count)
                ReloadFruit(tree.Location, Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key, tree.fruit.Count);
            return fruitSizes[tree.Location][Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key][index];
        }
        private static Vector2 GetFruitOffsetForShake(FruitTree tree, int index)
        {
            if (!Config.EnableMod || index < 2)
                return Vector2.Zero;
            return GetFruitOffset(tree, index);
        }
        private static int GetFruitQualityDays(int days)
        {
            if (!Config.EnableMod)
                return days;
            switch (days)
            {
                case -112:
                    return -Config.DaysUntilSilverFruit;
                case -224:
                    return -Config.DaysUntilGoldFruit;
                case -336:
                    return -Config.DaysUntilIridiumFruit;
            }
            return days;
        }

		public static int QualityPatch(int days)
		{
			/*
			this method is only needed if using transpiler. currently using prefix, so no need for now but i will hold on to it
			*/

			if (days > -Config.DaysUntilIridiumFruit)
			{
				if (days > -Config.DaysUntilGoldFruit)
				{
					if (days > -Config.DaysUntilSilverFruit)
					{
						Log($"{days} is not old enough for Silver. Returning Base.", debugOnly: true);
						return 0;
					}
					else {
						Log($"{days} is older than -{Config.DaysUntilSilverFruit}! Returning 2", debugOnly: true);
						return 1; 
					}
				}
				else {
					Log($"{days} is older than -{Config.DaysUntilGoldFruit}! Returning 3", debugOnly: true);
					return 2; 
				}
			}
			else {
				Log($"{days} is older than -{Config.DaysUntilIridiumFruit}! Returning 4", debugOnly: true);
				return 4;
			}
		}

        private static Vector2 GetFruitOffset(FruitTree tree, int index)
        {
            if (!fruitOffsets.TryGetValue(tree.Location, out Dictionary<Vector2, List<Vector2>> dict) || !dict.TryGetValue(Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key, out List<Vector2> offsets) || offsets.Count < tree.fruit.Count)
                ReloadFruit(tree.Location, Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key, tree.fruit.Count);
            return fruitOffsets[tree.Location][Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == tree).Key][index];
        }
        private static int ChangeDaysToMatureCheck(int oldValue)
        {
            if (!Config.EnableMod)
                return oldValue;
            switch (oldValue)
            {
                case 0:
                    return 0;
                case 7:
                    return Config.DaysUntilMature / 4;
                case 14:
                    return Config.DaysUntilMature / 2;
                case 21:
                    return Config.DaysUntilMature * 3 / 4;
            }
            return oldValue;
        }

        private static void ReloadFruit(GameLocation location, Vector2 tileLocation, int max)
        {
            if (!fruitOffsets.ContainsKey(location))
                fruitOffsets.Add(location, new Dictionary<Vector2, List<Vector2>>());
            if (!fruitOffsets[location].TryGetValue(tileLocation, out List<Vector2> offsets))
            {
                offsets = new List<Vector2>();
                fruitOffsets[location][tileLocation] = offsets;
            }
            if (!fruitColors.ContainsKey(location))
                fruitColors.Add(location, new Dictionary<Vector2, List<Color>>());
            if (!fruitColors[location].TryGetValue(tileLocation, out List<Color> colors))
            {
                colors = new List<Color>();
                fruitColors[location][tileLocation] = colors;
            }
            if (!fruitSizes.ContainsKey(location))
                fruitSizes.Add(location, new Dictionary<Vector2, List<float>>());
            if (!fruitSizes[location].TryGetValue(tileLocation, out List<float> sizes))
            {
                sizes = new List<float>();
                fruitSizes[location][tileLocation] = sizes;
            }
            if (offsets.Count != max)
            {
                offsets.Clear();
                colors.Clear();
                sizes.Clear();
                SMonitor.Log($"Resetting fruit offsets for {tileLocation} in {location.Name}");
                for (int i = 0; i < max; i++)
                {
                    var color = Color.White;
                    color.R -= (byte)(Game1.random.NextDouble() * Config.ColorVariation);
                    color.G -= (byte)(Game1.random.NextDouble() * Config.ColorVariation);
                    color.B -= (byte)(Game1.random.NextDouble() * Config.ColorVariation);
                    colors.Add(color);

                    sizes.Add(4 * (float)(1 + ((Game1.random.NextDouble() * 2 - 1) * Config.SizeVariation / 100)));

                    if (i < 3)
                    {
                        offsets.Add(Vector2.Zero);
                        continue;
                    }
                    bool gotSpot = false;
                    Vector2 offset;
                    while (!gotSpot)
                    {
                        double distance = 24;
                        for (int j = 0; j < 100; j++)
                        {
                            gotSpot = true;
                            offset = new Vector2(Config.FruitSpawnBufferX + Game1.random.Next(34 * 4 - Config.FruitSpawnBufferX), Config.FruitSpawnBufferY + Game1.random.Next(58 * 4 - Config.FruitSpawnBufferY));
                            for (int k = 0; k < offsets.Count; k++)
                            {
                                if (Vector2.Distance(offsets[k], offset) < distance)
                                {
                                    distance--;
                                    gotSpot = false;
                                    break;
                                }
                            }
                            if (gotSpot)
                            {
                                offsets.Add(offset);
                                break;
                            }
                        }
                    }
                }
            }
        }
    }
}