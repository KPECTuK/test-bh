using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace BH.Model
{
	public static class ExtensionsSets
	{
		public static void CopyTo<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> target)
		{
			foreach(var (key, value) in source)
			{
				target.Add(key, value);
			}
		}

		public static T[] Clear<T>(this T[] source, T @default)
		{
			var size = source?.Length ?? 0;
			for(var index = 0; index < size; index++)
			{
				// ReSharper disable once PossibleNullReferenceException
				source[index] = @default;
			}
			return source;
		}

		public static T[] Clear<T>(this T[] source, Func<int, T> initializer)
		{
			var size = source?.Length ?? 0;
			initializer ??= _ => default;
			for(var index = 0; index < size; index++)
			{
				// ReSharper disable once PossibleNullReferenceException
				source[index] = initializer(index);
			}
			return source;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Swap<T>(this T[] target, int left, int right)
		{
			var temp = target[left];
			target[left] = target[right];
			target[right] = temp;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Swap<T>(this IList<T> target, int left, int right)
		{
			var temp = target[left];
			target[left] = target[right];
			target[right] = temp;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ShiftToLow<T>(this T[] target)
		{
			T temp = default;
			for(var index = target.Length - 1; index > -1; index--)
			{
				var tempSecond = target[index];
				target[index] = temp;
				temp = tempSecond;
			}
			return temp;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T ShiftToHi<T>(this T[] target)
		{
			T temp = default;
			for(var index = 0; index < target.Length; index++)
			{
				var tempSecond = target[index];
				target[index] = temp;
				temp = tempSecond;
			}
			return temp;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] ShiftCycleToLow<T>(this T[] target, int offset)
		{
			offset = target.Length - offset;
			target.Reverse(0, target.Length);
			target.Reverse(0, offset);
			target.Reverse(offset, target.Length - offset);
			return target;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] ShiftCycleToHi<T>(this T[] target, int offset)
		{
			target.Reverse(0, target.Length);
			target.Reverse(0, offset);
			target.Reverse(offset, target.Length - offset);
			return target;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Reverse<T>(this T[] target, int index, int size)
		{
			var indexHead = index;
			var indexTail = index + size - 1;
			while(indexHead < indexTail)
			{
				target.Swap(indexTail, indexHead);
				indexHead++;
				indexTail--;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void DeFragment<T>(this T[] target) where T : class
		{
			for(var index = 1; index < target.Length; index++)
			{
				var indexFix = index;
				while(indexFix > 0 && !ReferenceEquals(target[indexFix], null))
				{
					(target[indexFix], target[indexFix - 1]) = (target[indexFix - 1], target[indexFix]);
					indexFix--;
				}
			}
		}

		/// <summary> Fisher Yates Algorithm </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T[] Shuffle<T>(this T[] target)
		{
			for(var index = target.Length - 1; index > -1; index--)
			{
				var indexRandom = UnityEngine.Random.Range(0, index);
				target.Swap(index, indexRandom);
			}

			return target;
		}

		/// <summary> Fisher Yates Algorithm </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IList<T> Shuffle<T>(this IList<T> target)
		{
			for(var index = target.Count - 1; index > -1; index--)
			{
				var indexRandom = UnityEngine.Random.Range(0, index);
				target.Swap(index, indexRandom);
			}
			return target;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int SetDefault<T>(this T[] target, T comparand) where T : class
		{
			for(var index = 0; index < target.Length; index++)
			{
				if(ReferenceEquals(target[index], comparand))
				{
					target[index] = default;

					return index;
				}
			}

			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int InsertAtFirstDefault<T>(this T[] target, T value) where T : class
		{
			//? use bi-search
			for(var index = 0; index < target.Length; index++)
			{
				if(ReferenceEquals(target[index], default))
				{
					target[index] = value;

					return index;
				}
			}

			return -1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool SeqEquals<T>(this IComparer<T> comparer, ArraySegment<T> left, ArraySegment<T> right)
		{
			if(left.Count != right.Count)
			{
				return false;
			}

			var size = left.Count;
			for(var index = 0; index < size; index++)
			{
				var indexLeft = index + left.Offset;
				var indexRight = index + right.Offset;
				if(comparer.Compare(left[indexLeft], right[indexRight]) != 0)
				{
					return false;
				}
			}

			return true;
		}
	}
}
