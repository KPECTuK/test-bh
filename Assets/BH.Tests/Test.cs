using System;
using System.Collections.Generic;
using System.Globalization;
using BH.Components;
using BH.Model;
using NUnit.Framework;

namespace BH.Tests
{
	public class Test
	{
		// im sure its reliable, but to prove
		// not fully covered

		//! hello Unity..
		//public static IEnumerable Data_A()
		//{
		//	var @const = CxId.Create();
		//	return new[]
		//	{
		//		new TestCaseData(CxId.Empty, CxId.Empty).Returns(true),
		//		new TestCaseData(CxId.Create(), CxId.Empty).Returns(false),
		//		new TestCaseData(CxId.Empty, CxId.Create()).Returns(false),
		//		new TestCaseData(CxId.Create(), @const).Returns(false),
		//		new TestCaseData(@const, CxId.Create()).Returns(false),
		//		new TestCaseData(@const, @const).Returns(true),
		//	};
		//}

		//[TestCaseSource(typeof(Test), nameof(Data_A))]
		//public bool AAA_TestIdEquals(CxId left, CxId right)
		//{
		//	return left.Equals(right);
		//}

		//[TestCaseSource(typeof(Test), nameof(Data_A))]
		//public bool AAB_TestIdEquals(CxId left, CxId right)
		//{
		//	return left == right;
		//}

		private readonly CxId Const = CxId.Create();

		[Test]
		public void AAA_TestIdEquals()
		{
			var left = CxId.Empty;
			var right = CxId.Empty;
			Assert.IsTrue(left.Equals(right));
		}

		[Test]
		public void ABA_TestIdEquals()
		{
			var left = CxId.Empty;
			var right = CxId.Empty;
			Assert.IsTrue(left == right);
		}

		[Test]
		public void AAB_TestIdEquals()
		{
			var left = CxId.Create();
			var right = CxId.Empty;
			Assert.IsFalse(left.Equals(right));
		}

		[Test]
		public void ABB_TestIdEquals()
		{
			var left = CxId.Empty;
			var right = CxId.Create();
			Assert.IsFalse(left == right);
		}

		[Test]
		public void AAC_TestIdEquals()
		{
			var left = CxId.Create();
			var right = Const;
			Assert.IsFalse(left.Equals(right));
		}

		[Test]
		public void ABC_TestIdEquals()
		{
			var left = CxId.Empty;
			var right = Const;
			Assert.IsTrue(left != right);
		}

		[Test]
		public void AAD_TestIdEquals()
		{
			var left = Const;
			var right = Const;
			Assert.IsTrue(left.Equals(right));
		}

		[Test]
		public void ABD_TestIdEquals()
		{
			var left = Const;
			var right = Const;
			Assert.IsTrue(left == right);
		}

		[Test]
		public void BAA_TestGetRecent()
		{
			var source = new ListRef<ModelUser>();
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:1")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:7")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:3")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:2")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:6")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:4")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:5")});
			source.Add(new ModelUser { IdUser = CxId.Create(), TimestampDiscovery = DateTime.Parse("01/01/2000 0:0:1")});

			// ReSharper disable once InconsistentNaming
			static bool isUnique(CxId[] set, ListRef<ModelUser> data)
			{
				// uniqueness
				for(var indexOuter = 0; indexOuter < set.Length; indexOuter++)
				{
					for(var indexInner = 0; indexInner < set.Length; indexInner++)
					{
						if(indexInner == indexOuter)
						{
							continue;
						}

						if(set[indexInner] == set[indexOuter])
						{
							return false;
						}
					}
				}

				return true;
			}

			static bool isOrdered(CxId[] set, ListRef<ModelUser> data)
			{
				// ordered
				var result = true;
				for(var index = 0; index < set.Length - 1; index++)
				{
					var itemLeft = data.Get(set[index], out var containsLeft);
					var itemRight = data.Get(set[index+ 1], out var containsRight);
					result = 
						containsLeft && 
						containsRight && 
						result && 
						itemLeft.TimestampDiscovery <= itemRight.TimestampDiscovery;
				}
				return result;
			}

			var result = new CxId[0];
			var size = source.GetRecentForHost(result, CxId.Empty);
			result
				.ToText(
					$"test 00 (num: {size})",
					_ => source.Get(_, out var contains)
						.TimestampDiscovery.ToString(CultureInfo.InvariantCulture))
				.Log();
			Assert.IsTrue(isUnique(result, source), "not unique: 00");
			Assert.IsTrue(isOrdered(result, source), "not ordered: 00");

			result = new CxId[1];
			size = source.GetRecentForHost(result, CxId.Empty);
			result
				.ToText(
					$"test 01 (num: {size})",
					_ => source.Get(_, out var contains)
						.TimestampDiscovery.ToString(CultureInfo.InvariantCulture))
				.Log();
			Assert.IsTrue(isUnique(result, source), "not unique: 01");
			Assert.IsTrue(isOrdered(result, source), "not ordered: 01");

			result = new CxId[4];
			size = source.GetRecentForHost(result, CxId.Empty);
			result
				.ToText(
					$"test 01 (num: {size})",
					_ => source.Get(_, out var contains)
						.TimestampDiscovery.ToString(CultureInfo.InvariantCulture))
				.Log();
			Assert.IsTrue(isUnique(result, source), "not unique: 02");
			Assert.IsTrue(isOrdered(result, source), "not ordered: 02");
		}
	}
}
