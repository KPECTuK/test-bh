using System;
using System.Linq;
using BH.Model;
using UnityEngine;
#if UNITY_EDITOR
using System.Reflection;
#endif

namespace BH.Components
{
	[CreateAssetMenu(menuName = "Create Settings Pawn", fileName = "settings_pawn", order = 0)]
	public sealed class SettingsPawn : ScriptableObject
	{
		[Range(.01f, 10f)] public float MaxSpeedMove = 2f;
		[Range(.01f, 10f)] public float MaxSpeedStrafe = 2f;
		[Range(1f, 360f)] public float MaxSpeedTurnDeg = 16f;
		[Range(1f, 360f)] public float MaxSpeedPitchDeg = 16f;
		[Range(-89f, 0f)] public float PitchMinDeg = -60;
		[Range(0f, 89f)] public float PitchMaxDeg = 60;

		[Range(.01f, 10f)] public float MaxUltSpeed = 4f;
		[Range(.01f, 10f)] public float MaxUltDistance = 2f;

		public PawnFeature[] Features;

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

			// ReSharper disable once InconsistentNaming
			bool isFeatureNotUnique(int index)
			{
				if(Features[index].IdFeature.IsEmpty)
				{
					return true;
				}

				for(var indexSearch = 0; indexSearch < Features.Length; indexSearch++)
				{
					if(indexSearch == index)
					{
						continue;
					}

					if(Features[index].IdFeature == Features[indexSearch].IdFeature)
					{
						return true;
					}
				}

				return false;
			}

			if(Features != null)
			{
				//$"features: {Features.Length}".Log();

				//! unity makes a copy of another element (probably selected one) .. nice
				for(var index = 0; index < Features.Length; index++)
				{
					var attempts = 0;
					const int ATTEMPTS_I = 100;
					while(isFeatureNotUnique(index) && ++attempts < ATTEMPTS_I)
					{
						Features[index].IdFeature = CxId.Create();
						Features[index].PawnColor = Color.HSVToRGB(UnityEngine.Random.value, 1f, 1f);
					}

					if(attempts != 0)
					{
						$"generated [{attempts,00}]: ( {Features[index].IdFeature}, {Features[index].PawnColor} )".Log();
					}
				}
			}
			//else
			//{
			//	"no features found".Log();
			//}
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

	[Serializable]
	public class PawnFeature
	{
		public CxId IdFeature;
		public Color PawnColor;
	}
}
