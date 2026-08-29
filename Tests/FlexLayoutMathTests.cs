using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Sperlich.UISystem.Tests {

	public class FlexLayoutMathTests {

		private static FlexMathItem Item(float baseMain, float grow = 0f, float shrink = 1f, float min = 0f, float max = float.PositiveInfinity) {
			return new FlexMathItem { baseMain = baseMain, grow = grow, shrink = shrink, minMain = min, maxMain = max };
		}

		[Test]
		public void Grow_DistributesFreeSpaceProportionally() {
			var items = new List<FlexMathItem> { Item(100f, grow: 1f), Item(100f, grow: 3f) };
			var sizes = new float[2];

			FlexLayoutMath.ResolveFlexibleLengths(items, 0, 2, 600f, 0f, sizes);

			// 400 free, split 1:3 -> +100 / +300
			Assert.AreEqual(200f, sizes[0], 0.01f);
			Assert.AreEqual(400f, sizes[1], 0.01f);
		}

		[Test]
		public void Grow_WithoutGrowFactors_KeepsBaseSizes() {
			var items = new List<FlexMathItem> { Item(100f), Item(150f) };
			var sizes = new float[2];

			FlexLayoutMath.ResolveFlexibleLengths(items, 0, 2, 500f, 0f, sizes);

			Assert.AreEqual(100f, sizes[0], 0.01f);
			Assert.AreEqual(150f, sizes[1], 0.01f);
		}

		[Test]
		public void Grow_RespectsMaxClamp() {
			var items = new List<FlexMathItem> { Item(100f, grow: 1f, max: 120f), Item(100f, grow: 1f) };
			var sizes = new float[2];

			FlexLayoutMath.ResolveFlexibleLengths(items, 0, 2, 400f, 0f, sizes);

			Assert.AreEqual(120f, sizes[0], 0.01f);
		}

		[Test]
		public void Shrink_DistributesDeficitByScaledShrink() {
			var items = new List<FlexMathItem> { Item(200f, shrink: 1f), Item(200f, shrink: 1f) };
			var sizes = new float[2];

			FlexLayoutMath.ResolveFlexibleLengths(items, 0, 2, 300f, 0f, sizes);

			// deficit 100, equal scaled shrink -> 150 / 150
			Assert.AreEqual(150f, sizes[0], 0.01f);
			Assert.AreEqual(150f, sizes[1], 0.01f);
		}

		[Test]
		public void Shrink_RespectsMinClamp() {
			var items = new List<FlexMathItem> { Item(200f, shrink: 1f, min: 180f), Item(200f, shrink: 1f) };
			var sizes = new float[2];

			FlexLayoutMath.ResolveFlexibleLengths(items, 0, 2, 300f, 0f, sizes);

			Assert.AreEqual(180f, sizes[0], 0.01f);
		}

		[Test]
		public void Gap_IsSubtractedFromAvailableSpace() {
			var items = new List<FlexMathItem> { Item(0f, grow: 1f), Item(0f, grow: 1f) };
			var sizes = new float[2];

			FlexLayoutMath.ResolveFlexibleLengths(items, 0, 2, 220f, 20f, sizes);

			Assert.AreEqual(100f, sizes[0], 0.01f);
			Assert.AreEqual(100f, sizes[1], 0.01f);
		}

		[Test]
		public void SplitIntoLines_NoWrap_ReturnsSingleLine() {
			var items = new List<FlexMathItem> { Item(100f), Item(100f), Item(100f) };

			var lines = FlexLayoutMath.SplitIntoLines(items, 150f, 0f, false);

			Assert.AreEqual(1, lines.Count);
			Assert.AreEqual(new Vector2Int(0, 3), lines[0]);
		}

		[Test]
		public void SplitIntoLines_Wrap_BreaksWhenOverflowing() {
			var items = new List<FlexMathItem> { Item(100f), Item(100f), Item(100f), Item(100f) };

			var lines = FlexLayoutMath.SplitIntoLines(items, 250f, 0f, true);

			Assert.AreEqual(2, lines.Count);
			Assert.AreEqual(new Vector2Int(0, 2), lines[0]);
			Assert.AreEqual(new Vector2Int(2, 2), lines[1]);
		}

		[Test]
		public void SplitIntoLines_Wrap_CountsGapTowardLineWidth() {
			var items = new List<FlexMathItem> { Item(100f), Item(100f), Item(100f) };

			// 100 + 20 + 100 = 220 fits in 230; +20+100 = 340 does not
			var lines = FlexLayoutMath.SplitIntoLines(items, 230f, 20f, true);

			Assert.AreEqual(2, lines.Count);
			Assert.AreEqual(2, lines[0].y);
			Assert.AreEqual(1, lines[1].y);
		}

		[Test]
		public void SplitIntoLines_Wrap_SingleOversizedItemGetsOwnLine() {
			var items = new List<FlexMathItem> { Item(400f), Item(100f) };

			var lines = FlexLayoutMath.SplitIntoLines(items, 200f, 0f, true);

			Assert.AreEqual(2, lines.Count);
			Assert.AreEqual(1, lines[0].y);
			Assert.AreEqual(1, lines[1].y);
		}

		[Test]
		public void Distribute_Start_PlacesItemsFromZeroWithGap() {
			var sizes = new[] { 50f, 50f };
			var positions = new float[2];

			FlexLayoutMath.Distribute(sizes, 2, 300f, 10f, JustifyContent.Start, positions);

			Assert.AreEqual(0f, positions[0], 0.01f);
			Assert.AreEqual(60f, positions[1], 0.01f);
		}

		[Test]
		public void Distribute_Center_CentersTheRow() {
			var sizes = new[] { 100f, 100f };
			var positions = new float[2];

			FlexLayoutMath.Distribute(sizes, 2, 400f, 0f, JustifyContent.Center, positions);

			Assert.AreEqual(100f, positions[0], 0.01f);
			Assert.AreEqual(200f, positions[1], 0.01f);
		}

		[Test]
		public void Distribute_SpaceBetween_PushesToEdges() {
			var sizes = new[] { 100f, 100f };
			var positions = new float[2];

			FlexLayoutMath.Distribute(sizes, 2, 400f, 0f, JustifyContent.SpaceBetween, positions);

			Assert.AreEqual(0f, positions[0], 0.01f);
			Assert.AreEqual(300f, positions[1], 0.01f);
		}

		[Test]
		public void Distribute_SpaceEvenly_EqualGapsIncludingEnds() {
			var sizes = new[] { 100f, 100f };
			var positions = new float[2];

			FlexLayoutMath.Distribute(sizes, 2, 400f, 0f, JustifyContent.SpaceEvenly, positions);

			// 200 free / 3 slots ~ 66.67
			Assert.AreEqual(66.67f, positions[0], 0.1f);
			Assert.AreEqual(233.33f, positions[1], 0.1f);
		}

		[Test]
		public void ResolveAlignSelf_AutoFallsBackToContainerDefault() {
			Assert.AreEqual(AlignItems.Center, FlexLayoutMath.ResolveAlignSelf(AlignSelf.Auto, AlignItems.Center));
			Assert.AreEqual(AlignItems.End, FlexLayoutMath.ResolveAlignSelf(AlignSelf.End, AlignItems.Start));
		}

		[Test]
		public void AlignCross_CenterAndEnd() {
			Assert.AreEqual(20f, FlexLayoutMath.AlignCross(AlignItems.Center, 100f, 60f), 0.01f);
			Assert.AreEqual(40f, FlexLayoutMath.AlignCross(AlignItems.End, 100f, 60f), 0.01f);
			Assert.AreEqual(0f, FlexLayoutMath.AlignCross(AlignItems.Start, 100f, 60f), 0.01f);
		}
	}
}
