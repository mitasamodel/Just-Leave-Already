using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

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
}
