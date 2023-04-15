using System;
using BH.Model;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace BH.Components
{
	[CreateAssetMenu(menuName = "Create Settings Pawn", fileName = "settings_pawn", order = 0)]
	public class SettingsPawn : ScriptableObject
	{
		[Range(.01f, 10f)] public float MaxSpeedMove = 2f;
		[Range(.01f, 10f)] public float MaxSpeedStrafe = 2f;
		[Range(1f, 360f)] public float MaxSpeedTurnDeg = 16f;
		[Range(1f, 360f)] public float MaxSpeedPitchDeg = 16f;
		[Range(-89f, 0f)] public float PitchMinDeg = -60;
		[Range(0f, 89f)] public float PitchMaxDeg = 60;

		[Range(.01f, 10f)] public float MaxUltSpeed = 4f;
		[Range(.01f, 10f)] public float MaxUltDistance = 2f;

		public string RenderUltTime()
		{
			return $"Utl interval (sec): {MaxUltDistance / MaxUltSpeed:F3}";
		}

		#if UNITY_EDITOR
		private void OnValidate()
		{
			{
				var (min, max) = GetRange(nameof(MaxSpeedMove));
				MaxSpeedMove = MaxSpeedMove.Clamp(min, max);
			}

			{
				var (min, max) = GetRange(nameof(MaxSpeedStrafe));
				MaxSpeedStrafe = MaxSpeedStrafe.Clamp(min, max);
			}

			{
				var (min, max) = GetRange(nameof(MaxSpeedTurnDeg));
				MaxSpeedTurnDeg = MaxSpeedTurnDeg.Clamp(min, max);
			}

			{
				var (min, max) = GetRange(nameof(MaxSpeedPitchDeg));
				MaxSpeedPitchDeg = MaxSpeedPitchDeg.Clamp(min, max);
			}

			{
				var (minPitchMin, maxPitchMin) = GetRange(nameof(PitchMinDeg));
				var (minPitchMax, maxPitchMax) = GetRange(nameof(PitchMaxDeg));

				PitchMinDeg = PitchMinDeg.Clamp(minPitchMin, maxPitchMin);
				PitchMaxDeg = PitchMaxDeg.Clamp(minPitchMax, maxPitchMax);

				// to smallest interval
				if(PitchMaxDeg < PitchMinDeg)
				{
					PitchMinDeg = maxPitchMin;
				}

				if(PitchMinDeg > PitchMaxDeg)
				{
					PitchMaxDeg = minPitchMax;
				}
			}

			{
				var (minMove, maxMove) = GetRange(nameof(MaxSpeedMove));
				var (minUlt, maxUlt) = GetRange(nameof(MaxUltSpeed));
				MaxUltSpeed = MaxUltSpeed.Clamp(minUlt, maxUlt);
				MaxUltSpeed = MaxUltSpeed.Clamp(minMove, maxUlt);
			}

			{
				var (min, max) = GetRange(nameof(MaxUltDistance));
				MaxUltDistance = MaxUltDistance.Clamp(min, max);
			}
		}

		private static (float min, float max) GetRange(string name)
		{
			var info = string.IsNullOrWhiteSpace(name)
				? throw new Exception("field name request")
				: typeof(SettingsPawn).GetField(name, BindingFlags.Public | BindingFlags.Instance);
			var attr = info == null
				? throw new Exception($"field not found with name: {name}")
				: info.GetCustomAttribute<RangeAttribute>();
			return (attr.min, attr.max);
		}
		#endif
	}
}
