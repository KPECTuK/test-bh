#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace BH.Components.Editor
{
	/// <summary>
	/// This script should NOT be placed in an "Editor" folder. Ideally placed in a "Plugins" folder.
	/// https://gist.github.com/Invertex
	/// </summary>
	internal class CustomHoldingInteractionEditor : InputParameterEditor<CustomHoldingInteraction>
	{
		private static GUIContent _pressPointWarning;
		private static GUIContent _holdTimeWarning;
		private static GUIContent _pressPointLabel;
		private static GUIContent _holdTimeLabel;

		protected override void OnEnable()
		{
			_pressPointLabel = new GUIContent("Press Point",
				"The minimum amount this input's actuation value must exceed to be considered \"held\".\n" +
				"Value less-than or equal to 0 will result in the 'Default Button Press Point' value being used from your 'Project Settings > Input System'.");

			_holdTimeLabel = new GUIContent("Min Hold Time",
				"The minimum amount of realtime seconds before the input is considered \"held\".\n" +
				"Value less-than or equal to 0 will result in the 'Default Hold Time' value being used from your 'Project Settings > Input System'.");

			_pressPointWarning = EditorGUIUtility.TrTextContent("Using \"Default Button Press Point\" set in project-wide input settings.");
			_pressPointWarning = EditorGUIUtility.TrTextContent("Using \"Default Button Press Point\" set in project-wide input settings.");
			_holdTimeWarning = EditorGUIUtility.TrTextContent("Using \"Default Hold Time\" set in project-wide input settings.");
		}

		public override void OnGUI()
		{
			DrawDisableIfDefault(ref target.pressPoint, ref target.useDefaultSettingsPressPoint, _pressPointLabel, _pressPointWarning);
			DrawDisableIfDefault(ref target.duration, ref target.useDefaultSettingsDuration, _holdTimeLabel, _holdTimeWarning);
		}

		private void DrawDisableIfDefault(ref float value, ref bool useDefault, GUIContent fieldName, GUIContent warningText)
		{
			EditorGUILayout.BeginHorizontal();

			EditorGUI.BeginDisabledGroup(useDefault);
			value = EditorGUILayout.FloatField(fieldName, value);
			EditorGUI.EndDisabledGroup();
			useDefault = EditorGUILayout.ToggleLeft("Default", useDefault);
			EditorGUILayout.EndHorizontal();

			if(useDefault || value <= 0)
			{
				EditorGUILayout.HelpBox(warningText);
			}
		}
	}
}
#endif
