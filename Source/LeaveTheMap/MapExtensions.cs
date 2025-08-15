using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LeaveTheMap
{
	public static class MapExtensions
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

			map.exitMapGrid?.Notify_LOSBlockerSpawned();

			//var method = typeof(ExitMapGrid).GetMethod("Rebuild", BindingFlags.Instance | BindingFlags.NonPublic);
			//method?.Invoke(map.exitMapGrid, null);

			//map.exitMapGrid.Drawer?.SetDirty();               // Force redraw
			//map.exitMapGrid.Drawer?.CellBoolDrawerUpdate();   // Apply redraw immediately
		}
	}
}
