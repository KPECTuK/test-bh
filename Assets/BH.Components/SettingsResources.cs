using System;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace BH.Components
{
	[CreateAssetMenu(menuName = "Create Settings Resources", fileName = "settings_service_resources", order = 0)]
	public sealed class SettingsResources : ScriptableObject
	{
		public RectTransform ProtoItemServer;
		public RectTransform ProtoItemUser;

		#if UNITY_EDITOR
		private void OnValidate()
		{
			var type = typeof(SettingsResources);
			var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
			foreach(var info in fields)
			{
				// class, interface, nullable
				if(info.FieldType.IsByRefLike)
				{
					var value = info.GetValue(this);
					if(ReferenceEquals(null, value))
					{
						throw new Exception($"validation exception: prototype is not set '{info.Name}'");
					}
				}
			}
		}
		#endif
	}
}
