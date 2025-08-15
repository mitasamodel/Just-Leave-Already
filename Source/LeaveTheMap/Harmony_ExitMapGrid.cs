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
	public static class Harmony_ExitMapGrid
	{
		static readonly FieldInfo SettingsFI =
				AccessTools.Field(typeof(LeaveTheMapMod), nameof(LeaveTheMapMod.settings));
		static readonly FieldInfo ExitGridSizeFI =
			AccessTools.Field(typeof(LeaveTheMapSettings), nameof(LeaveTheMapSettings.ExitGridSize));

		/// <summary>
		/// Change the maps on which ExitGrid is available
		/// </summary>
		[HarmonyPatch(typeof(ExitMapGrid), nameof(ExitMapGrid.MapUsesExitGrid), MethodType.Getter)]
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

		/// <summary>
		/// Grid size: use setting instead of hardcorded "2"
		/// </summary>
		[HarmonyPatch(typeof(ExitMapGrid), "IsGoodExitCell")]
		public static class ExitMapGrid_IsGoodExitCell_Patch
		{
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var matcher = new CodeMatcher(instructions);
				matcher.MatchStartForward(
						new CodeMatch(ci => ci.opcode == OpCodes.Ldc_R4 && ci.OperandIs(2f))
					)
					.Repeat(cm =>
					{
						var labels = cm.Instruction.labels; // carry over any labels targeting the old ldc.r4 2f
						cm.RemoveInstruction()
						  .InsertAndAdvance(
							  new CodeInstruction(OpCodes.Ldsfld, SettingsFI).WithLabels(labels),   // Static field: LeaveTheMapMod.settings
							  new CodeInstruction(OpCodes.Ldfld, ExitGridSizeFI),                   // Instance field: LeaveTheMapSettings.ExitGridSize
							  new CodeInstruction(OpCodes.Conv_R4)                                  // Convert int to float
						  );
					});
				return matcher.InstructionEnumeration();
			}
		}

		/// <summary>
		/// Grid size: replace hardocred values by setting
		/// </summary>
		[HarmonyPatch(typeof(ExitMapGrid), "Rebuild")]
		public static class ExitMapGrid_Rebuild_Patch
		{
			public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
			{
				var matcher = new CodeMatcher(instructions);

				// -------- A) Replace all hardcoded '2's with ExitGridSize
				matcher.Start();
				matcher.MatchStartForward(new CodeMatch(ci =>
							ci.opcode == OpCodes.Ldc_I4_2 ||
							(ci.opcode == OpCodes.Ldc_I4_S && Equals(ci.operand, (sbyte)2))))
					.Repeat(m =>
					{
						var labels = m.Instruction.labels;
						m.RemoveInstruction()
						 .InsertAndAdvance(
							 new CodeInstruction(OpCodes.Ldsfld, SettingsFI).WithLabels(labels),
							 new CodeInstruction(OpCodes.Ldfld, ExitGridSizeFI)
						 );
					});

				// -------- B) For patterns "... ldloc.* ; ldc.i4.1 ; ble/ble.s ..."
				// Replace the middle "1" with "(ExitGridSize - 1)"
				matcher.Start();
				matcher.MatchStartForward(
						new CodeMatch(ci => ci.opcode.Name.StartsWith("ldloc")),        // ldloc.*, any form
						new CodeMatch(ci => ci.opcode == OpCodes.Ldc_I4_1),            // the literal 1
						new CodeMatch(ci => ci.opcode == OpCodes.Ble || ci.opcode == OpCodes.Ble_S) // comparison
					)
					.Repeat(m =>
					{
						m.Advance(1);       // Move to the next instruction: ldc.i4.1 (middle of the 3-instruction window)
						var labels = m.Instruction.labels;

						m.RemoveInstruction() // remove the '1'
						 .InsertAndAdvance(
							 new CodeInstruction(OpCodes.Ldsfld, SettingsFI).WithLabels(labels),
							 new CodeInstruction(OpCodes.Ldfld, ExitGridSizeFI),
							 new CodeInstruction(OpCodes.Ldc_I4_1),
							 new CodeInstruction(OpCodes.Sub)
						 );
					});
				return matcher.InstructionEnumeration();
			}
		}
	}
}
