using System;
using System.Linq;
using BH.Model;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Editor
{
	public static class ToolModifiers
	{
		[Shortcut("mode.toggle.actionCenter", KeyCode.S)]
		public static void ToggleModeHandlePosition()
		{
			Tools.pivotMode = Tools.pivotMode == PivotMode.Center ? PivotMode.Pivot : PivotMode.Center;
		}

		[Shortcut("mode.toggle.actionPivot", KeyCode.S, ShortcutModifiers.Shift)]
		public static void ToggleModeHandleHierarchy()
		{
			Tools.pivotRotation = Tools.pivotRotation == PivotRotation.Global ? PivotRotation.Local : PivotRotation.Global;
		}

		[Shortcut("mode.show.window.inspector", KeyCode.I)]
		public static void ShowWindowInspector()
		{
			var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.InspectorWindow");
			var window = EditorWindow.GetWindow(windowType);
		}

		[Shortcut("mode.show.window.console", KeyCode.C)]
		public static void ShowWindowConsole()
		{
			var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ConsoleWindow");
			var window = EditorWindow.GetWindow(windowType);
		}

		[Shortcut("mode.show.window.hierarchy", KeyCode.H)]
		public static void ShowWindowHierarchy()
		{
			var windowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.SceneHierarchyWindow");
			var window = EditorWindow.GetWindow(windowType);
		}

		[MenuItem("Utility/Show window types")]
		public static void LogWindowTypes()
		{
			var baseType = typeof(EditorWindow);
			var requiredAttribute = baseType.Assembly.GetType("UnityEditor.EditorWindowTitleAttribute");

			var types = from assembly in AppDomain.CurrentDomain.GetAssemblies()
				from type in assembly.GetTypes()
				where baseType.IsAssignableFrom(type) && type.GetCustomAttributes(requiredAttribute, true).Length > 0
				select type;

			types.OrderBy(_ => _.Name).ToText("All window types").Log();
		}
	}
}
