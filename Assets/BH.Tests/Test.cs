using System;
using System.Collections.Generic;
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
			var source = new Queue<ModelViewUser>();
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:1")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:7")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:3")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:2")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:6")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:4")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:5")});
			source.Enqueue(new ModelViewUser { FirstUpdated = DateTime.Parse("01/01/2000 0:0:1")});

			// ReSharper disable once InconsistentNaming
			static bool isOrdered(ModelViewUser[] set)
			{
				var result = true;
				for(var index = 0; index < set.Length - 1; index++)
				{
					result = result && set[index].FirstUpdated <= set[index + 1].FirstUpdated;
				}
				return result;
			}

			var result_00 = new ModelViewUser[0];
			var size = source.GetRecent(result_00, CxId.Empty);
			result_00.ToText($"test 00 (num: {size})", _ => _.FirstUpdated.ToString()).Log();
			Assert.IsTrue(isOrdered(result_00));

			var result_01 = new ModelViewUser[1];
			size = source.GetRecent(result_01, CxId.Empty);
			result_01.ToText($"test 01 (num: {size})", _ => _.FirstUpdated.ToString()).Log();
			Assert.IsTrue(isOrdered(result_01));

			var result_02 = new ModelViewUser[4];
			size = source.GetRecent(result_02, CxId.Empty);
			result_02.ToText($"test 02 (num: {size})", _ => _.FirstUpdated.ToString()).Log();
			Assert.IsTrue(isOrdered(result_02));
		}
	}
}
