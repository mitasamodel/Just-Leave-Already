using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;

namespace LeaveTheMap
{
	/// <summary>
	/// This saves and loads settings
	/// </summary>
	public class LeaveTheMapSettings : ModSettings
	{
		const int ExitGridSizeDefault = 10;

		public bool AllowLeaveAtHome = true;
		public bool AllowLeaveAtCamp = true;
		public bool AllowLeaveAtSites = true;
		public bool AllowLeaveAtIncident_Always = false;
		public bool AllowLeaveAtIncident_AtWon = true;
		public bool AllowLeaveAtIncident_AtTimePassed = true;
		public int AllowLeaveAtIncident_After = 180;        //seconds
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

		public LeaveTheMapMod(ModContentPack content) : base(content)
		{
			settings = GetSettings<LeaveTheMapSettings>();
		}

		public override void DoSettingsWindowContents(Rect inRect)
		{
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
				// Buffer to hold manual input text
				incidentSecondsInputBuffer ??= settings.AllowLeaveAtIncident_After.ToString();
				// First row: label + text field (side by side)
				Rect row = listingStandard.GetRect(Text.LineHeight); // 1 line tall
																	 // Split space: label (left), text field (right)
				float labelWidth = row.width * 0.4f;
				float fieldWidth = row.width * 0.2f;
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

			// Add a small gap first
			listingStandard.GapLine();
			// Get a button-sized rect
			Rect buttonRect = listingStandard.GetRect(30f);
			// Draw the button
			if (Widgets.ButtonText(buttonRect, "Restore Defaults"))
			{
				settings.ResetToDefaults();
				incidentSecondsInputBuffer = settings.AllowLeaveAtIncident_After.ToString();
				LoadedModManager.GetMod<LeaveTheMapMod>().WriteSettings();
			}

			listingStandard.End();
		}

		public override string SettingsCategory()
		{
			return "Just Leave Already!";
		}
	}
}
