using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace LeaveTheMap
{
	/// <summary>
	/// Rebuild exit grid if required
	/// </summary>
	public class ExitGridUpdateManager : GameComponent
	{
		private static int ticksUntilRebuild = -1;

		public ExitGridUpdateManager(Game game) { }

		public override void GameComponentTick()
		{
			if (ticksUntilRebuild > 0)
			{
				ticksUntilRebuild--;
			}
			else if (ticksUntilRebuild == 0)
			{
				Log.Message("[JLA] Rebuild exit grid");
				ticksUntilRebuild = -1;
				foreach (Map map in Find.Maps)
				{
					if (map == null) continue;

					// Force color rebuild
					var fld = typeof(ExitMapGrid).GetField("drawerInt", BindingFlags.Instance | BindingFlags.NonPublic);
					fld?.SetValue(map.exitMapGrid, null);

					// Rebuild exit grid
					map.exitMapGrid?.Notify_LOSBlockerSpawned();
					map.exitMapGrid?.Drawer?.SetDirty();
					map.exitMapGrid?.Drawer?.CellBoolDrawerUpdate();
				}
			}
		}

		/// <summary>
		/// Request a rebuild after X seconds
		/// </summary>
		/// <param name="map">Target map</param>
		/// <param name="delaySeconds">How many seconds to wait</param>
		public static void RequestRebuildDelayed(int delaySeconds = 1)
		{
			ticksUntilRebuild = delaySeconds * 60; // convert seconds to ticks
		}
	}
}
