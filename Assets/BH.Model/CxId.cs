using System;
using System.Runtime.InteropServices;
using System.Text;
using Mirror;
using UnityEngine;

namespace BH.Model
{
	[StructLayout(LayoutKind.Explicit)]
	[Serializable]
	public unsafe struct CxId : IEquatable<CxId>
	{
		public const int SIZE_I = 16;

		// ReSharper disable once Unity.RedundantSerializeFieldAttribute
		[SerializeField]
		[FieldOffset(00)] private fixed byte _id[SIZE_I];
		[FieldOffset(00)] private byte _00;
		[FieldOffset(01)] private byte _01;
		[FieldOffset(02)] private byte _02;
		[FieldOffset(03)] private byte _03;
		[FieldOffset(04)] private byte _04;
		[FieldOffset(05)] private byte _05;
		[FieldOffset(06)] private byte _06;
		[FieldOffset(07)] private byte _07;
		[FieldOffset(08)] private byte _08;
		[FieldOffset(09)] private byte _09;
		[FieldOffset(10)] private byte _10;
		[FieldOffset(11)] private byte _11;
		[FieldOffset(12)] private byte _12;
		[FieldOffset(13)] private byte _13;
		[FieldOffset(14)] private byte _14;
		[FieldOffset(15)] private byte _15;
		/// <summary> immutable </summary>
		[FieldOffset(SIZE_I)] private int _hash;

		public bool IsEmpty => Compare(ref this, ref Empty);

		public static CxId Empty;

		static CxId()
		{
			for(var index = 0; index < 16; index++)
			{
				Empty._id[index] = 0;
			}
		}

		public CxId(Guid source)
		{
			_hash = source.GetHashCode();
			var array = source.ToByteArray();
			_00 = array[0];
			_01 = array[1];
			_02 = array[2];
			_03 = array[3];
			_04 = array[4];
			_05 = array[5];
			_06 = array[6];
			_07 = array[7];
			_08 = array[8];
			_09 = array[9];
			_10 = array[10];
			_11 = array[11];
			_12 = array[12];
			_13 = array[13];
			_14 = array[14];
			_15 = array[15];
		}

		public static bool operator ==(CxId left, CxId right)
		{
			return left.Equals(right);
		}

		public static bool operator !=(CxId left, CxId right)
		{
			return !left.Equals(right);
		}

		public static CxId Create()
		{
			return Guid.NewGuid();
		}

		public bool Equals(CxId other)
		{
			return Compare(ref this, ref other);
		}

		public override bool Equals(object other)
		{
			return other is CxId cast && Equals(cast);
		}

		public override int GetHashCode()
		{
			return _hash;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			for(var index = 0; index < 16; index++)
			{
				builder.Append($"{_id[index]:X2}");
				if(index != 15)
				{
					builder.Append(":");
				}
			}

			return builder.ToString();
		}

		public string ShortForm(bool isBraces = true)
		{
			return isBraces
				?$"[{_id[0]:X2}..{_id[14]:X2}{_id[15]:X2}]"
				:$"{_id[0]:X2}..{_id[14]:X2}{_id[15]:X2}";
		}

		private static bool Compare(ref CxId source, ref CxId comparand)
		{
			//~ optimize by bitwise operation
			for(var index = 0; index < 16; index++)
			{
				if(source._id[index] != comparand._id[index])
				{
					return false;
				}
			}

			return true;
		}

		public static implicit operator CxId(Guid guid)
		{
			return new(guid);
		}

		public static void Writer(NetworkWriter target, CxId source)
		{
			target.WriteBytes(source._id, 0, SIZE_I);
		}

		public static CxId Reader(NetworkReader source)
		{
			CxId target = default;
			var segment = source.ReadBytesSegment(SIZE_I);

			if(segment.Array == null)
			{
				throw new Exception("reader segment is null");
			}

			fixed(byte* segmentPtr = &segment.Array[segment.Offset])
			{
				Buffer.MemoryCopy(segmentPtr, target._id, SIZE_I, SIZE_I);
			}

			var h1 = HashCode.Combine(
				target._00,
				target._01,
				target._02,
				target._03,
				target._04,
				target._05,
				target._06,
				target._07);
			var h2 = HashCode.Combine(
				target._08,
				target._09,
				target._10,
				target._11,
				target._12,
				target._13,
				target._14,
				target._15);
			target._hash = HashCode.Combine(h1, h2);

			return target;
		}
	}
}
