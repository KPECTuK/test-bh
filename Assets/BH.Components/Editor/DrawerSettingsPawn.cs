#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace BH.Components.Editor
{
	[CustomPropertyDrawer(typeof(SettingsPawn))]
	public sealed class DrawerSettingsPawn : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var height = 0f;

			{
				var content = new GUIContent("Settings:");
				var inner = GUI.skin.textField.CalcSize(content);
				height += GUI.skin.label.CalcScreenSize(inner).y;
			}

			var target = property.GetValue<SettingsPawn>();
			if(target != null)
			{
				var type = typeof(SettingsPawn);
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

				foreach(var info in fields)
				{
					var contentText = new GUIContent($"{info.GetValue(target):F3}");
					var sizeText = GUI.skin.textField.CalcSize(contentText);
					sizeText = GUI.skin.textField.CalcScreenSize(sizeText);
					height += sizeText.y;
				}

				{
					var content = new GUIContent(target.RenderUltTime());
					var inner = GUI.skin.label.CalcSize(content);
					height += GUI.skin.label.CalcScreenSize(inner).y;
				}

				if(target.Features != null)
				{
					var content = new GUIContent("Feature:");
					var innerLabel = GUI.skin.label.CalcSize(content);
					var sizeLabel = GUI.skin.label.CalcScreenSize(innerLabel);
					var innerColor = GUI.skin.textField.CalcSize(content);
					var sizeColor = GUI.skin.label.CalcScreenSize(innerColor);
					var size = Mathf.Max(sizeColor.y, sizeLabel.y);
					height += size * target.Features.Length;
				}
			}

			return height;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			{
				var content = new GUIContent("Settings:");
				var sizeInner = GUI.skin.textField.CalcSize(content);
				var size = GUI.skin.textField.CalcScreenSize(sizeInner);
				EditorGUI.ObjectField(
					new Rect(position.position, new Vector2(position.width, sizeInner.y)),
					property,
					content);
				position.Set(
					position.x,
					position.y + size.y,
					position.width,
					position.height - size.y);
			}

			GUI.enabled = false;

			var target = property.GetValue<SettingsPawn>();
			if(target != null)
			{
				var type = typeof(SettingsPawn);
				var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);

				foreach(var info in fields)
				{
					var contentLabel = new GUIContent($"{info.Name}:");
					var contentText = new GUIContent($"{info.GetValue(target):F3}");

					var sizeInner = GUI.skin.textField.CalcSize(contentText);
					var size = GUI.skin.textField.CalcScreenSize(sizeInner);

					var value = EditorGUI.TextField(
						new Rect(position.position, new Vector2(position.width, sizeInner.y)),
						contentLabel,
						contentText.text);

					//if(!string.Equals(value, contentText.text) && float.TryParse(value, out var changed))
					//{
					//	var range = info.GetCustomAttribute<RangeAttribute>();
					//	if(range != null)
					//	{
					//		changed = changed.Clamp(range.min, range.max);
					//	}

					//	info.SetValue(target, changed);
					//}

					position.Set(
						position.x,
						position.y + size.y,
						position.width,
						position.height - size.y);
				}

				{
					var content = new GUIContent(target.RenderUltTime());
					var sizeInner = GUI.skin.label.CalcSize(content);
					var size = GUI.skin.label.CalcScreenSize(sizeInner);
					EditorGUI.LabelField(
						new Rect(position.position, new Vector2(position.width, sizeInner.y)),
						content);
					position.Set(
						position.x,
						position.y + size.y,
						position.width,
						position.height - size.y);
				}

				{
					if(target.Features != null)
					{
						for(var index = 0; index < target.Features.Length; index++)
						{
							var content = new GUIContent($"Feature [{target.Features[index].IdFeature.ShortForm()}]:");
							var sizeInner = GUI.skin.textField.CalcSize(content);
							var size = GUI.skin.textField.CalcScreenSize(sizeInner);
							EditorGUI.ColorField(
								new Rect(position.position, new Vector2(position.width, sizeInner.y)),
								content,
								target.Features[index].PawnColor);
							position.Set(
								position.x,
								position.y + size.y,
								position.width,
								position.height - size.y);
						}
					}
				}
			}

			GUI.enabled = true;
			EditorGUI.EndProperty();
		}
	}
}
#endif
