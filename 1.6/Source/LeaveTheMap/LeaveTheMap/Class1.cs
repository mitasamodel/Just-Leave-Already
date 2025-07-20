using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.Noise;

namespace LeaveTheMap
{
	[StaticConstructorOnStartup]
	public static class Class1
	{
		static Class1()
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
			if (___map.IsPlayerHome)
				__result = true;

			//Caravan camp
			if (___map.IsCaravanCamp())
				__result = true;

			//Sites: quests, ancient complexes
			if (___map.IsSite())
				__result = true;

			//Caravan incidents - only for test
			//if (___map.IsCaravanIncident())
			//	__result = true;
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
			return map?.Parent?.GetType().Name == "Camp";
		}

		/// <summary>
		/// Check if the current map is a caravan incident
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool IsCaravanIncident(this Map map)
		{
			return map?.Parent?.GetType().Name == "CaravansBattlefield";
		}

		/// <summary>
		/// Check if the current map is a Site (quest)
		/// </summary>
		/// <param name="map"></param>
		/// <returns></returns>
		public static bool IsSite(this Map map)
		{
			return map?.Parent?.GetType().Name == "Site";
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
			Log.Message($"[MY] Map: {map}, Type: {map.GetType().Name}, Parent: {map.Parent?.GetType().Name}");
		}
	}
#endif
}
