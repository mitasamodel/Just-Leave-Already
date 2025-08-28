using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace LeaveTheMap
{
	/// <summary>
	/// This saves and loads settings
	/// </summary>
	public class LeaveTheMapSettings : ModSettings
	{
		const bool ExitGridSizeEnabledDefault = false;
		const int ExitGridSizeDefault = 4;
		public const int ExitGridSizeGameDefault = 2;

		public bool AllowLeaveAtHome = true;
		public bool AllowLeaveAtCamp = true;
		public bool AllowLeaveAtSites = true;
		public bool AllowLeaveAtIncident_Always = false;
		public bool AllowLeaveAtIncident_AtWon = true;
		public bool AllowLeaveAtIncident_AtTimePassed = true;
		public int AllowLeaveAtIncident_After = 180;        //seconds
		public bool ExitGridSizeEnabled = ExitGridSizeEnabledDefault;
		public int ExitGridSize = ExitGridSizeDefault;

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref AllowLeaveAtHome, "AllowLeaveAtHome", true);
			Scribe_Values.Look(ref AllowLeaveAtCamp, "AllowLeaveAtCamp", true);
			Scribe_Values.Look(ref AllowLeaveAtSites, "AllowLeaveAtSites", true);
			Scribe_Values.Look(ref AllowLeaveAtIncident_Always, "AllowLeaveAtIncident_Always", false);
			Scribe_Values.Look(ref AllowLeaveAtIncident_AtWon, "AllowLeaveAtIncident_AtWon", true);
			Scribe_Values.Look(ref AllowLeaveAtIncident_AtTimePassed, "AllowLeaveAtIncident_AtWon", true);
			Scribe_Values.Look(ref AllowLeaveAtIncident_After, "AllowLeaveAtIncident_After", 180);
			Scribe_Values.Look(ref ExitGridSizeEnabled, "ExitGridSizeEnabled", ExitGridSizeEnabledDefault);
			Scribe_Values.Look(ref ExitGridSize, "ExitGridSize", ExitGridSizeDefault);
		}

		public void ResetToDefaults()
		{
			AllowLeaveAtHome = true;
			AllowLeaveAtCamp = true;
			AllowLeaveAtSites = true;
			AllowLeaveAtIncident_Always = false;
			AllowLeaveAtIncident_AtWon = true;
			AllowLeaveAtIncident_AtTimePassed = true;
			AllowLeaveAtIncident_After = 180;
			ExitGridSizeEnabled = ExitGridSizeEnabledDefault;
			ExitGridSize = ExitGridSizeDefault;
		}
	}

	/// <summary>
	/// This stores settings in memory and makes them accessible from other classes
	/// </summary>
	public class LeaveTheMapMod : Mod
	{
		public static LeaveTheMapSettings settings;
		private string incidentSecondsInputBuffer;
		private string exitGridInputBuffer;

		public LeaveTheMapMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<LeaveTheMapSettings>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
			int originalGridSize = settings.ExitGridSize;

			Listing_Standard listingStandard = new Listing_Standard();
			listingStandard.Begin(inRect);

			listingStandard.Label("Allow to leave the map...");
			//Checkboxes
			listingStandard.CheckboxLabeled("...on home map", ref settings.AllowLeaveAtHome, "Recommended: enabled");
			listingStandard.CheckboxLabeled("...on caravan camping map", ref settings.AllowLeaveAtCamp, "Recommended: enabled");
			listingStandard.CheckboxLabeled("...on quests (sites) map", ref settings.AllowLeaveAtSites,
				"Recommended: enabled\n\nNote: this setting doesn't disable the exit grid if it exists on original quest's map");
			listingStandard.CheckboxLabeled("...always on caravan incident map", ref settings.AllowLeaveAtIncident_Always, "Recommended: disabled");
			//Ignore if always enabled
			GUI.enabled = !settings.AllowLeaveAtIncident_Always;
			listingStandard.CheckboxLabeled("...on caravan incident map when won the battle", ref settings.AllowLeaveAtIncident_AtWon, "Recommended: enabled");
			listingStandard.CheckboxLabeled("...on caravan incident map after time passed", ref settings.AllowLeaveAtIncident_AtTimePassed, "Recommended: enabled, 180 seconds");

			if (settings.AllowLeaveAtIncident_AtTimePassed)
			{
				// First row: label + text field (side by side)
				Rect row = listingStandard.GetRect(Text.LineHeight); // 1 line tall
																	 // Split space: label (left), text field (right)
				float labelWidth = row.width * 0.4f;
				float fieldWidth = row.width * 0.2f;
				// Buffer to hold manual input text
				incidentSecondsInputBuffer ??= settings.AllowLeaveAtIncident_After.ToString();

				// Label
				Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "Seconds should pass (180 recommended):");
				// Text field
				incidentSecondsInputBuffer = Widgets.TextField(
					new Rect(row.x + labelWidth, row.y, fieldWidth, row.height),
					incidentSecondsInputBuffer
				);
				if (int.TryParse(incidentSecondsInputBuffer, out int parsed))
					settings.AllowLeaveAtIncident_After = Mathf.Clamp(parsed, 0, 1000);
				settings.AllowLeaveAtIncident_After = (int)listingStandard.Slider(settings.AllowLeaveAtIncident_After, 0, 1000);
				incidentSecondsInputBuffer = settings.AllowLeaveAtIncident_After.ToString();
			}
			GUI.enabled = true;

			listingStandard.GapLine();
			listingStandard.CheckboxLabeled("Modify exit grid size", ref settings.ExitGridSizeEnabled,
				"Some items (e.g. vehicles from VF) can be too big and cannot achieve the edge of the map.");
			if (settings.ExitGridSizeEnabled)
			{
				Rect row = listingStandard.GetRect(Text.LineHeight); // 1 line tall
				float labelWidth = row.width * 0.4f;
				float fieldWidth = row.width * 0.2f;
				Widgets.Label(new Rect(row.x, row.y, labelWidth, row.height), "Grid size:");
				exitGridInputBuffer = Widgets.TextField(
					new Rect(row.x + labelWidth, row.y, fieldWidth, row.height),
					exitGridInputBuffer
					);
				if (int.TryParse(exitGridInputBuffer, out int parsed))
					settings.ExitGridSize = Mathf.Clamp(parsed, 2, 50);
				settings.ExitGridSize = (int)listingStandard.Slider(settings.ExitGridSize, 2, 50);
				exitGridInputBuffer = settings.ExitGridSize.ToString();
			}
			else
				settings.ExitGridSize = LeaveTheMapSettings.ExitGridSizeGameDefault;  //game's default

			// Compatibility with WalkTheWorld - it clears the color and it must be forcefully updated
			if (ModsConfig.IsActive("addvans.WalkTheWorld"))
			{
				Rect rect = listingStandard.GetRect(30f);
				TooltipHandler.TipRegion(rect, "Walk The World mod changes the color of the exit grid if it is disabled on all maps.\n\n" +
					"Click this button to re-draw the exit grids after you disabled that setting.\n\n" +
					"Note: If grid disabled globally in Walk The World mod, then that mod (Just leave already) is basically not used.");
				if (Widgets.ButtonText(rect, "Force update exit grid (run the game for 1 sec after click)"))
				{
					ExitGridUpdateManager.RequestRebuildDelayed(delaySeconds: 1);
					SoundDefOf.Click.PlayOneShotOnCamera();
					Messages.Message("Run the game for 1 sec.", MessageTypeDefOf.TaskCompletion, historical: false);
				}
			}

			if (originalGridSize != settings.ExitGridSize)
			{
				ExitGridUpdateManager.RequestRebuildDelayed(delaySeconds: 1);   //delayed update
			}

			// Add a small gap first
			listingStandard.GapLine();
			// Get a button-sized rect
			Rect buttonRect = listingStandard.GetRect(30f);
			// Draw the button
			if (Widgets.ButtonText(buttonRect, "Restore Defaults"))
			{
				settings.ResetToDefaults();
				incidentSecondsInputBuffer = settings.AllowLeaveAtIncident_After.ToString();
				exitGridInputBuffer = settings.ExitGridSize.ToString();
				LoadedModManager.GetMod<LeaveTheMapMod>().WriteSettings();
				ExitGridUpdateManager.RequestRebuildDelayed();
			}

			listingStandard.End();
		}

		public override string SettingsCategory()
		{
			return "Just Leave Already!";
		}
	}
}
