using HarmonyLib;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace LeaveTheMap
{
	[StaticConstructorOnStartup]
	public static class LeaveTheMap
	{
		static LeaveTheMap()
		{
			new Harmony("LeaveTheMap.Mod").PatchAll();
		}
	}

	[HarmonyPatch(typeof(ExitMapGrid), "get_MapUsesExitGrid")]
	public static class ExitMapGrid_MapUsesExitGrid_Patch
	{
		public static void Postfix(Map ___map, ref bool __result)
		{
			//Player home
			if (LeaveTheMapMod.settings?.AllowLeaveAtHome == true && ___map.IsPlayerHome)
				__result = true;

			//Caravan camp
			if (LeaveTheMapMod.settings?.AllowLeaveAtCamp == true && ___map.IsCaravanCamp())
				__result = true;

			//Sites: quests, ancient complexes
			if (LeaveTheMapMod.settings?.AllowLeaveAtSites == true && ___map.IsSite())
				__result = true;

			//Caravan incidents
			if (___map.IsCaravanIncident())
			{
				if (LeaveTheMapMod.settings?.AllowLeaveAtIncident_Always == true)
					__result = true;
				else
				{
					if (LeaveTheMapMod.settings?.AllowLeaveAtIncident_AtWon == true && ___map.IsBattleWon())
						__result = true;
					if (LeaveTheMapMod.settings?.AllowLeaveAtIncident_AtTimePassed == true &&
						___map.TimePassedSeconds() > LeaveTheMapMod.settings.AllowLeaveAtIncident_After)
						__result = true;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ExitMapGrid), "IsGoodExitCell")]
	public static class ExitMapGrid_IsGoodExitCell_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var code in instructions)
			{
				if (code.opcode == OpCodes.Ldc_R4 && (float)code.operand == 2f)
				{
					var labels = code.labels; // Preserve any attached labels

					yield return new CodeInstruction(OpCodes.Ldsfld,
						AccessTools.Field(typeof(LeaveTheMapMod), nameof(LeaveTheMapMod.settings)))
						.WithLabels(labels); // Attach labels to first new instruction

					yield return new CodeInstruction(OpCodes.Ldfld,
						AccessTools.Field(typeof(LeaveTheMapSettings), nameof(LeaveTheMapSettings.ExitGridSize)));

					yield return new CodeInstruction(OpCodes.Conv_R4); // Convert int to float
				}
				else
				{
					yield return code;
				}
			}
		}
	}

	[HarmonyPatch(typeof(ExitMapGrid), "Rebuild")]
	public static class ExitMapGrid_Rebuild_Patch
	{
		public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			foreach (var code in instructions)
			{
				// Replace hardcoded constant 2 (MaxDistToEdge) with mod setting
				if ((code.opcode == OpCodes.Ldc_I4_2) ||
					(code.opcode == OpCodes.Ldc_I4_S && (sbyte)code.operand == 2))
				{
					// Load static field: LeaveTheMapMod.settings
					yield return new CodeInstruction(OpCodes.Ldsfld,
						AccessTools.Field(typeof(LeaveTheMapMod), nameof(LeaveTheMapMod.settings)))
						.WithLabels(code.labels);

					// Load instance field: ExitGridSize
					yield return new CodeInstruction(OpCodes.Ldfld,
						AccessTools.Field(typeof(LeaveTheMapSettings), nameof(LeaveTheMapSettings.ExitGridSize)));
				}
				else
				{
					yield return code;
				}
			}
		}
	}

	public static class CurrentMapInfo
	{
		/// <summary>
		/// Check if the current map is a caravan camp
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool IsCaravanCamp(this Map map)
		{
			return map?.Parent is RimWorld.Planet.Camp;
		}

		/// <summary>
		/// Check if the current map is a caravan incident
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool IsCaravanIncident(this Map map)
		{
			return map?.Parent is RimWorld.Planet.CaravansBattlefield;
		}

		/// <summary>
		/// Check if the current map is a Site (quest)
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool IsSite(this Map map)
		{
			return map?.Parent is RimWorld.Planet.Site;
		}

		/// <summary>
		/// Check if the Battle for the caravan has been won
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool IsBattleWon(this Map map)
		{
			return (map.Parent as CaravansBattlefield)?.WonBattle ?? false;
		}

		/// <summary>
		/// How many seconds passed from the creation of the map
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static float TimePassedSeconds(this Map map)
		{
			return GenTicks.TicksToSeconds(GenTicks.TicksGame - map.generationTick);
		}

		/// <summary>
		/// Force rebuild exit grid
		/// </summary>
		/// <param name="map"></param>
		public static void ForceExitGridRebuild(Map map)
		{
			if (map == null) return;

			var method = typeof(ExitMapGrid).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic);
			method?.Invoke(map.exitMapGrid, null);

			map.exitMapGrid.Drawer?.SetDirty();               // Force redraw
			map.exitMapGrid.Drawer?.CellBoolDrawerUpdate();   // Apply redraw immediately
		}
	}

#if DEBUG
	public class DummyTestGameComponent : GameComponent
	{
		private int tickCounter;

		public DummyTestGameComponent(Game game) { }

		public override void GameComponentTick()
		{
			tickCounter++;

			if (tickCounter % 60 == 0)
			{
				DoSomethingEverySecond();
			}
		}

		private void DoSomethingEverySecond()
		{
			//foreach (var map in Find.Maps)
			//{
			//	Log.Message($"[MY] Map: {map}, Type: {map.GetType().Name}, Parent: {map.Parent?.GetType().Name}");
			//}
			Map map = Find.CurrentMap;
			//Log.Message($"[MAP] Map: {map}, Type: {map.GetType().Name}, Parent: {map.Parent?.GetType().Name}, Fullname: {map.Parent?.GetType().FullName}, " +
			//	$"Time: {GenTicks.TicksToSeconds(GenTicks.TicksGame - map.generationTick)}");

			Log.Message("[MAP] Try rebuild");
			CurrentMapInfo.ForceExitGridRebuild(map);
			//map?.exitMapGrid.Drawer?.CellBoolDrawerUpdate();
		}
	}
#endif
}
