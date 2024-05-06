﻿
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewModdingAPI;
using StardewValley;
using StardewValley.GameData.Buildings;
using StardewValley.Internal;
using StardewValley.ItemTypeDefinitions;
using StardewValley.TerrainFeatures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

using LogLevel = StardewModdingAPI.LogLevel;
using Object = StardewValley.Object;

namespace FruitTreeTweaks
{
    public partial class ModEntry
    {
        [HarmonyPatch(typeof(FruitTree), new Type[] { typeof(string), typeof(int) })] // aedenthorn
        [HarmonyPatch(MethodType.Constructor)]
        public class FruitTree__Patch1
        {
            public static void Postfix(FruitTree __instance)
            {
                if (!Config.EnableMod)
                    return;
                __instance.daysUntilMature.Value = Config.DaysUntilMature;
                SMonitor.Log($"New fruit tree: set days until mature to {Config.DaysUntilMature}");
            }
        }
        [HarmonyPatch(typeof(FruitTree), new Type[] { typeof(string), typeof(int) })] // aedenthorn
        [HarmonyPatch(MethodType.Constructor)]
        public class FruitTree__Patch2
        {
            public static void Postfix(FruitTree __instance)
            {
                if (!Config.EnableMod)
                    return;
                __instance.daysUntilMature.Value = Config.DaysUntilMature;
                SMonitor.Log($"New fruit tree: set days until mature to {Config.DaysUntilMature}");
            }
        }

        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.IsInSeasonHere))] // aedenthorn
        public class FruitTree_IsInSeasonHere_Patch
        {
            public static bool Prefix(ref bool __result)
            {
				/*
				if (!Config.EnableMod || !Config.FruitAllSeasons)
                    return true;
                __result = !Game1.IsWinter;
                return false;
				*/
				__result = (Config.EnableMod && Config.FruitAllSeasons && (!Game1.IsWinter || Config.FruitInWinter));
				return !__result;
            }
        }
        
        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.IsGrowthBlocked))] // aedenthorn
        public class FruitTree_IsGrowthBlocked_Patch
        {
            public static bool Prefix(FruitTree __instance, Vector2 tileLocation, GameLocation environment, ref bool __result)
            {
                if (!Config.EnableMod)
                    return true;
                foreach (Vector2 v in Utility.getSurroundingTileLocationsArray(tileLocation))
                {
                    if (Config.CropsBlock && environment.terrainFeatures.TryGetValue(v, out TerrainFeature feature) && feature is HoeDirt && (feature as HoeDirt).crop != null)
                    {
                        __result = true;
                        return false;
                    }

                    if (Config.ObjectsBlock && environment.IsTileOccupiedBy(v, CollisionMask.All, CollisionMask.None))
                    {
                        Object o = environment.getObjectAtTile((int)v.X, (int)v.Y);
                        if (o == null || !Utility.IsNormalObjectAtParentSheetIndex(o, "590"))
                        {
                            __result = true;
                            return false;
                        }
                    }
					if (Config.TreesBlock && environment.terrainFeatures.TryGetValue(v, out TerrainFeature feature2) && feature2 is Tree)
					{
						__result = true;
						return false;
					}
                }
                __result = false;
                return false;
            }
        }



        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.draw), new Type[] { typeof(SpriteBatch) })] // aedenthorn
        public class FruitTree_draw_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                SMonitor.Log($"Transpiling FruitTree.draw");
                var codes = new List<CodeInstruction>(instructions);
                bool found1 = false;
                int which = 0;
                for (int i = 0; i < codes.Count; i++)
                {
                    if (!found1 && i < codes.Count - 2 && codes[i].opcode == OpCodes.Ldc_I4_1 && codes[i + 1].opcode == OpCodes.Ldc_R4 && (float)codes[i + 1].operand == 1E-07f)
                    {
                        SMonitor.Log("shifting bottom of tree draw layer offset");
                        codes[i + 1].opcode = OpCodes.Ldarg_0;
                        codes.Insert(i + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.GetTreeBottomOffset))));
                        found1 = true;
                    }
                    else if (i > 0 && i < codes.Count - 18 && codes[i].opcode == OpCodes.Ldsfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(Game1), nameof(ItemRegistry.GetMetadata) + "." + nameof(ItemMetadata.GetParsedData) + "." + nameof(ParsedItemData.GetTexture)) && codes[i + 15].opcode == OpCodes.Call && (MethodInfo)codes[i + 15].operand == AccessTools.PropertyGetter(typeof(Color), nameof(Color.White)))
                    {
                        SMonitor.Log("modifying fruit scale");
                        codes[i + 18].opcode = OpCodes.Ldarg_0;
                        codes[i + 18].operand = null;
                        codes.Insert(i + 19, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.GetFruitScale))));
                        codes.Insert(i + 19, new CodeInstruction(OpCodes.Ldc_I4, which));
                        SMonitor.Log("modifying fruit color");
                        codes[i + 15].opcode = OpCodes.Ldarg_0;
                        codes[i + 15].operand = null;
                        codes.Insert(i + 16, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.GetFruitColor))));
                        codes.Insert(i + 16, new CodeInstruction(OpCodes.Ldc_I4, which));
                        which++;
                    }
                    if (found1 && which >= 2)
                        break;
                }

                return codes.AsEnumerable();
            }
            public static void Postfix(FruitTree __instance, SpriteBatch spriteBatch)
            {
                if (!Config.EnableMod || __instance.fruit.Count <= 3 || __instance.growthStage.Value < 4 || !fruitColors.TryGetValue(Game1.currentLocation, out Dictionary<Vector2, List<Color>> colors))
                    return;
                for (int i = 3; i < __instance.fruit.Count; i++)
                {
                    Vector2 offset = GetFruitOffset(__instance, i);
                    Color color = colors[Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == __instance).Key][i];


                    spriteBatch.Draw(Game1.objectSpriteSheet, Game1.GlobalToLocal(Game1.viewport, Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == __instance).Key * 64 - new Vector2(16, 80) * 4 + offset), new Rectangle?(Game1.getSourceRectForStandardTileSheet(Game1.objectSpriteSheet, (__instance.struckByLightningCountdown.Value > 0) ? 383 : ItemRegistry.GetDataOrErrorItem(__instance.fruit[0].ItemId).SpriteIndex, 16, 16)), color, 0f, Vector2.Zero, GetFruitScale(__instance, i), SpriteEffects.None, (float)__instance.getBoundingBox().Bottom / 10000f + 0.002f - Game1.getFarm().terrainFeatures.Pairs.FirstOrDefault(pair => pair.Value == __instance).Key.X / 1000000f + i / 100000f);
                }
            }
        }

        [HarmonyPatch(typeof(FruitTree), nameof(FruitTree.shake))] // aedenthorn
        public class FruitTree_shake_Patch
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                Log($"Transpiling FruitTree.shake", LogLevel.Debug);
                var codes = new List<CodeInstruction>(instructions);
				for (int i = 0; i < codes.Count; i++)
                {
					if (i < codes.Count - 4 && codes[i].opcode == OpCodes.Ldloca_S && codes[i + 1].opcode == OpCodes.Ldc_R4 && codes[i + 2].opcode == OpCodes.Ldc_R4 && (float)codes[i + 1].operand == 0 && (float)codes[i + 2].operand == 0 && codes[i + 3].opcode == OpCodes.Call && (ConstructorInfo)codes[i + 3].operand == AccessTools.Constructor(typeof(Vector2), new Type[] { typeof(float), typeof(float) }) && codes[i + 4].opcode == OpCodes.Ldloc_S && codes[i + 4].operand == codes[1 + 4].operand)
                    { // im getting index out of range on above if statement after changing Ldloc_3 => Ldloc_S
                        Log("replacing default fruit offset with method", LogLevel.Debug);
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Stloc_S, 4));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.GetFruitOffsetForShake))));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldloc_S));
                        codes.Insert(i + 4, new CodeInstruction(OpCodes.Ldarg_0));
                        i += 8;
                    }
                }

                return codes.AsEnumerable();
            }
        }

		[HarmonyPatch(typeof(FruitTree), nameof(FruitTree.GetQuality))] // chiccen
		public class FruitTree_GetQuality_Patch
		{
			/* Holding on to this for future use. It's easy to write but I don't want to memorize those damn op codes again.
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				Log("Transpiling FruitTree.GetQuality", LogLevel.Debug);
				var codes = new List<CodeInstruction>(instructions);
				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldc_I4_S && (sbyte)codes[i].operand == -112)
					{
						Log("replacing GetQuality with method");
						codes.RemoveRange(i, codes.Count - i);

						//codes.Insert(i, new CodeInstruction(OpCodes.Ldarg_0));
						codes.Insert(i, new CodeInstruction(OpCodes.Ret));
						codes.Insert(i, new CodeInstruction(OpCodes.Ldloc_0));
						codes.Insert(i, new CodeInstruction(OpCodes.Stloc_0));
						codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.QualityPatch))));

						break;
					}
				}


				return codes.AsEnumerable();
			}
			*/
			public static bool Prefix(FruitTree __instance, ref int __result)
			{
				// __instance.stump.Value was used to remove quality from matured sapling, however I just found out this is not a bug but a new 1.6 feature.
				if (!Config.EnableMod) return true;

				int days = __instance.daysUntilMature.Value;
				if (__instance.struckByLightningCountdown.Value > 0 || days >= 0)
				{
					__result = 0;
					return false;
				}
				if (days > -Config.DaysUntilIridiumFruit) // 0 = base, 1 = silver, 2 = gold, 3 = shadow realm, 4 = iridium
				{
					if (days > -Config.DaysUntilGoldFruit)
					{
						if (days > -Config.DaysUntilSilverFruit)
						{
							Log($"{days} is not old enough for Silver. Returning Base.", debugOnly: true);
							__result = 0;
						}
						else
						{
							Log($"{days} is older than -{Config.DaysUntilSilverFruit}! Returning 2", debugOnly: true);
							__result = 1;
						}
					}
					else
					{
						Log($"{days} is older than -{Config.DaysUntilGoldFruit}! Returning 3", debugOnly: true);
						__result = 2;
					}
				}
				else
				{
					Log($"{days} is older than -{Config.DaysUntilIridiumFruit}! Returning 4", debugOnly: true);
					__result = 4;
				}

				return false;
			}
		}

		
        [HarmonyPatch(typeof(Object), nameof(Object.placementAction))]
        public class Object_placementAction_Patch
        {
            public static bool Prefix(GameLocation location, int x, int y, ref bool __result, Farmer who = null)
            {

				if (who?.CurrentItem?.GetItemTypeId() is not "(O)") return true;

				Object obj = who?.CurrentItem as Object ?? null;
				if (Config.EnableMod && obj.IsFruitTreeSapling())
				{

					Vector2 placementTile = new Vector2(x / 64, y / 64);

					LogOnce($"Too Close To Another Tree: {FruitTree.IsTooCloseToAnotherTree(new Vector2(x / 64, y / 64), location)}", debugOnly: true);
					LogOnce($"Is Growth Blocked: {FruitTree.IsGrowthBlocked(new Vector2(x / 64, y / 64), location)}", debugOnly: true);
					
					if (location.terrainFeatures.TryGetValue(placementTile, out var terrainFeature2))
					{
						if (!(terrainFeature2 is HoeDirt { crop: null })) { return true; }
					}
					location.terrainFeatures.Remove(placementTile);
					string tileType = location.doesTileHaveProperty((int)placementTile.X, (int)placementTile.Y, "Type", "back");
					if ((location is Farm && location.CanItemBePlacedHere(placementTile)) || CanPlantAnywhere())
					{
						location.playSound("dirtyHit");
						DelayedAction.playSoundAfterDelay("coin", 100);
						FruitTree fruitTree = new FruitTree(obj.ItemId)
						{
							GreenHouseTileTree = (location.IsGreenhouse && tileType == "Stone")

						};
						location.terrainFeatures.Add(placementTile, fruitTree);
						__result = true;
						LogOnce($"{obj?.DisplayName} made it through all checks and should be good to place!");
						return false;
					}
				}
				LogOnce($"placementAction handling for {obj?.DisplayName} passed to original method.", debugOnly: true);
				return true;
            }
        }

		[HarmonyPatch(typeof(Object), nameof(Object.canBePlacedHere))] // chiccen
		public class Object_canBePlacedHere_Patch
		{

			public static bool Prefix(GameLocation l, Vector2 tile, ref bool __result)
			{
				CollisionMask mask = CollisionMask.All;
				Farmer who = Game1.player;
				Object tree = who?.ActiveObject ?? null;

				if (tree is null || !Config.EnableMod) return true;

				if (tree.IsFruitTreeSapling())
				{
					LogOnce($"{tree.DisplayName} too close: {FruitTree.IsTooCloseToAnotherTree(tile, l, false)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} growth blocked: {FruitTree.IsGrowthBlocked(tile, l)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} CantPlantTreesHere: {l.CanPlantTreesHere(tree.ItemId, (int)tile.X, (int)tile.Y, out var deniedMessage2)}", debugOnly: true);

					if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, mask))
					{
						return true;
					}

					__result = true;
					return false;
				}
				LogOnce($"canBePlacedHere handling for {tree?.DisplayName} passed to original method.", debugOnly: true);
				return true;
			}

			public static bool Prefix(GameLocation l, Vector2 tile, bool showError, ref bool __result)
			{
				CollisionMask mask = CollisionMask.All;
				Farmer who = Game1.player;
				Object tree = who?.ActiveObject ?? null;

                if (tree is null || !Config.EnableMod) return true;

                if (tree.IsFruitTreeSapling())
				{
					LogOnce($"{tree.DisplayName} too close: {FruitTree.IsTooCloseToAnotherTree(tile, l, false)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} growth blocked: {FruitTree.IsGrowthBlocked(tile, l)}", debugOnly:true);
					LogOnce($"{tree.DisplayName} CantPlantTreesHere: {l.CanPlantTreesHere(tree.ItemId, (int)tile.X, (int)tile.Y, out var deniedMessage2)}", debugOnly: true);

					if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, mask))
					{
						return true;
					}

					__result = true;
					return false;
				}
				LogOnce($"canBePlacedHere handling for {tree?.DisplayName} passed to original method.", debugOnly: true);
				return true;
			}

			public static bool Prefix(GameLocation l, Vector2 tile, CollisionMask collisionMask, ref bool __result)
			{
				Farmer who = Game1.player;
				Object tree = who?.ActiveObject ?? null;

                if (tree is null || !Config.EnableMod) return true;

                Object obj = l.getObjectAtTile((int)tile.X, (int)tile.Y);
				LogOnce($"Object at {(int)tile.X}, {(int)tile.Y} is {obj?.DisplayName}", debugOnly: true);

				if (tree.IsFruitTreeSapling())
				{
					LogOnce($"{tree.DisplayName} too close: {FruitTree.IsTooCloseToAnotherTree(tile, l, false)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} growth blocked: {FruitTree.IsGrowthBlocked(tile, l)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} CantPlantTreesHere: {l.CanPlantTreesHere(tree.ItemId, (int)tile.X, (int)tile.Y, out var deniedMessage2)}", debugOnly: true);

					if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask))
					{
						return true;
					}

					__result = true;
					return false;
				}
				LogOnce($"canBePlacedHere handling for {tree?.DisplayName} passed to original method.", debugOnly: true);
				return true;
			}

			public static bool Prefix(GameLocation l, Vector2 tile, CollisionMask collisionMask, bool showError, ref bool __result)
			{
				Farmer who = Game1.player;
				Object tree = who?.ActiveObject ?? null;

                if (tree is null || !Config.EnableMod) return true;

                Object obj = l.getObjectAtTile((int)tile.X, (int)tile.Y);
				LogOnce($"Object at {(int)tile.X}, {(int)tile.Y} is {obj?.DisplayName}", debugOnly: true);

				if (tree.IsFruitTreeSapling())
				{
					LogOnce($"{tree.DisplayName} too close: {FruitTree.IsTooCloseToAnotherTree(tile, l, false)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} growth blocked: {FruitTree.IsGrowthBlocked(tile, l)}", debugOnly: true);
					LogOnce($"{tree.DisplayName} CantPlantTreesHere: {l.CanPlantTreesHere(tree.ItemId, (int)tile.X, (int)tile.Y, out var deniedMessage2)}", debugOnly: true);

					if (!l.CanItemBePlacedHere(tile, itemIsPassable: true, collisionMask))
					{
						return true;
					}

					__result = true;
					return false;
				}
				LogOnce($"canBePlacedHere handling for {tree?.DisplayName} passed to original method.", debugOnly: true);
				return true;
			}
		}


		[HarmonyPatch(typeof(FruitTree), nameof(FruitTree.dayUpdate))] // aedenthorn
        public class FruitTree_dayUpdate_Patch
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                SMonitor.Log($"Transpiling FruitTree.dayUpdate");
                var codes = new List<CodeInstruction>(instructions);
                for (int i = 0; i < codes.Count; i++)
                {
                    if (codes[i].opcode == OpCodes.Call && (MethodInfo)codes[i].operand == AccessTools.Method(typeof(FruitTree), nameof(FruitTree.TryAddFruit)))
                    {
                        Log("Replacing fruit per day with methods");
                        //codes.RemoveAt(i); BROKEN -- CRASHES GAME. COME BACK TO THIS LATER THIS SHIT SUCKS.
						//codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.TryAddMoreFruit))));
                    }
                    if (i < codes.Count - 3 && codes[i].opcode == OpCodes.Ldfld && (FieldInfo)codes[i].operand == AccessTools.Field(typeof(FruitTree), nameof(FruitTree.daysUntilMature)) && codes[i + 3].opcode == OpCodes.Bgt_S)
                    {
                        Log("replacing daysUntilMature value with method", LogLevel.Trace);
                        codes.Insert(i + 3, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.ChangeDaysToMatureCheck))));
                    }
                }

                return codes.AsEnumerable();
            }
        }

		[HarmonyPatch(typeof(FruitTree), nameof(FruitTree.TryAddFruit))] // chiccen
		public class FruitTree_TryAddFruit_Patch
		{
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				if (!Config.EnableMod) return instructions;

				Log("Transpiling FruitTree.TryAddFruit", LogLevel.Debug);
				
				var codes= new List<CodeInstruction>(instructions);
				for (int i = 0; i < codes.Count; i++)
				{
					if (codes[i].opcode == OpCodes.Ldc_I4_3)
					{
						Log("replacing TryAddFruit with method", LogLevel.Trace);
						codes.Insert(i, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(ModEntry), nameof(ModEntry.GetMaxFruit))));
						codes.RemoveAt(i + 1);
					}
				}

				return codes.AsEnumerable();
			}

			public static void Postfix(ref bool __result)
			{
				if (!__result) { Log("TryAddFruit() failed to add fruit! \nPlease navigate to https://smapi.io/log/ to acquire your SMAPI log and post a bug report on Nexus with a link to the log.", LogLevel.Error); }
			}
		}
    }
}