using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace BH.Model
{
	public static class ExtensionsMath
	{
		public const float EPSILON = 0.000001f;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool AreEquals(this Rect source, Rect other)
		{
			var @is =
				Mathf.Approximately(source.x, other.x) &&
				Mathf.Approximately(source.y, other.y) &&
				Mathf.Approximately(source.height, other.height) &&
				Mathf.Approximately(source.width, other.width);
			return @is;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Abs(this float source)
		{
			return source > 0 ? source : -source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this float source, float value)
		{
			return (source > value ? source - value : value - source) <= EPSILON;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this Vector2 source, Vector2 value)
		{
			return (value - source).magnitude.Approx(0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool Approx(this Vector3 source, Vector3 value)
		{
			return (value - source).magnitude.Approx(0f);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int Clamp(this int source, int min, int max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static float Clamp(this float source, float min, float max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static double Clamp(this double source, double min, double max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TimeSpan Clamp(this TimeSpan source, TimeSpan min, TimeSpan max)
		{
			source = source < min ? min : source;
			source = source > max ? max : source;
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Quaternion Clamp(this Quaternion source, float minAngle, float maxAngle)
		{
			source.ToAngleAxis(out var angle, out var axis);

			if(angle < minAngle)
			{
				return Quaternion.AngleAxis(minAngle, axis);
			}

			if(angle > maxAngle)
			{
				return Quaternion.AngleAxis(maxAngle, axis);
			}

			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static Vector2 ProjectOnto(this Vector2 source, Vector2 target)
		{
			var normal = target.normalized;
			var dot = Vector2.Dot(source.normalized, normal);
			return normal * (source.magnitude * dot);
		}
	}
}
