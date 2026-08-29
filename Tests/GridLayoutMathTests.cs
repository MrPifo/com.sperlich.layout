using System.Collections.Generic;
using NUnit.Framework;

namespace Sperlich.UISystem.Tests {

	public class GridLayoutMathTests {

		[Test]
		public void FrTracks_SplitAvailableSpaceByWeight() {
			var tracks = new List<GridTrack> { GridTrack.Fr(1f), GridTrack.Fr(3f) };
			var sizes = new float[2];

			GridLayoutMath.ResolveTrackSizes(tracks, 400f, 0f, null, sizes);

			Assert.AreEqual(100f, sizes[0], 0.01f);
			Assert.AreEqual(300f, sizes[1], 0.01f);
		}

		[Test]
		public void FixedTrack_TakesPrecedenceOverFr() {
			var tracks = new List<GridTrack> { GridTrack.Pixels(120f), GridTrack.Fr(1f), GridTrack.Fr(1f) };
			var sizes = new float[3];

			GridLayoutMath.ResolveTrackSizes(tracks, 520f, 0f, null, sizes);

			Assert.AreEqual(120f, sizes[0], 0.01f);
			Assert.AreEqual(200f, sizes[1], 0.01f);
			Assert.AreEqual(200f, sizes[2], 0.01f);
		}

		[Test]
		public void Gap_ReducesSpaceAvailableToFrTracks() {
			var tracks = new List<GridTrack> { GridTrack.Fr(1f), GridTrack.Fr(1f), GridTrack.Fr(1f) };
			var sizes = new float[3];

			GridLayoutMath.ResolveTrackSizes(tracks, 340f, 20f, null, sizes);

			// 340 - 2*20 = 300 -> 100 each
			Assert.AreEqual(100f, sizes[0], 0.01f);
			Assert.AreEqual(100f, sizes[1], 0.01f);
			Assert.AreEqual(100f, sizes[2], 0.01f);
		}

		[Test]
		public void AutoTrack_UsesProvidedContentSize() {
			var tracks = new List<GridTrack> { GridTrack.Auto(), GridTrack.Fr(1f) };
			var sizes = new float[2];
			var autoSizes = new[] { 80f, 0f };

			GridLayoutMath.ResolveTrackSizes(tracks, 400f, 0f, autoSizes, sizes);

			Assert.AreEqual(80f, sizes[0], 0.01f);
			Assert.AreEqual(320f, sizes[1], 0.01f);
		}

		[Test]
		public void MinMax_WithFractionMax_NeverGoesBelowPixelFloor() {
			var tracks = new List<GridTrack> { GridTrack.MinMax(200f, 1f), GridTrack.Fr(1f) };
			var sizes = new float[2];

			// fr share would be 150 each, but track 0 is clamped up to its 200px floor
			// (v1: the plain fr keeps its 150 share, so the row overflows by 50 — documented compromise).
			GridLayoutMath.ResolveTrackSizes(tracks, 300f, 0f, null, sizes);

			Assert.AreEqual(200f, sizes[0], 0.01f);
			Assert.AreEqual(150f, sizes[1], 0.01f);
		}

		[Test]
		public void MinMax_WithFractionMax_GrowsAboveFloorWhenSpaceAllows() {
			var tracks = new List<GridTrack> { GridTrack.MinMax(100f, 1f), GridTrack.MinMax(100f, 1f) };
			var sizes = new float[2];

			GridLayoutMath.ResolveTrackSizes(tracks, 600f, 0f, null, sizes);

			Assert.AreEqual(300f, sizes[0], 0.01f);
			Assert.AreEqual(300f, sizes[1], 0.01f);
		}

		[Test]
		public void ResolveRepeatCount_FitsAsManyColumnsAsPossible() {
			// (available + gap) / (min + gap) = (640 + 10) / (200 + 10) = 3.09 -> 3
			Assert.AreEqual(3, GridLayoutMath.ResolveRepeatCount(640f, 10f, 200f));
			Assert.AreEqual(1, GridLayoutMath.ResolveRepeatCount(50f, 10f, 200f));
		}

		[Test]
		public void TrackOffset_AccumulatesSizesGapsAndPadding() {
			var sizes = new[] { 100f, 150f, 80f };

			Assert.AreEqual(8f, GridLayoutMath.TrackOffset(sizes, 0, 10f, 8f), 0.01f);
			Assert.AreEqual(118f, GridLayoutMath.TrackOffset(sizes, 1, 10f, 8f), 0.01f);
			Assert.AreEqual(278f, GridLayoutMath.TrackOffset(sizes, 2, 10f, 8f), 0.01f);
		}

		[Test]
		public void TrackSpan_CoversSpannedTracksIncludingInnerGaps() {
			var sizes = new[] { 100f, 150f, 80f };

			Assert.AreEqual(100f, GridLayoutMath.TrackSpan(sizes, 0, 1, 10f), 0.01f);
			Assert.AreEqual(260f, GridLayoutMath.TrackSpan(sizes, 0, 2, 10f), 0.01f);
			Assert.AreEqual(350f, GridLayoutMath.TrackSpan(sizes, 0, 3, 10f), 0.01f);
		}

		[Test]
		public void AlignInCell_CenterAndEnd() {
			Assert.AreEqual(25f, GridLayoutMath.AlignInCell(GridAlign.Center, 100f, 50f), 0.01f);
			Assert.AreEqual(50f, GridLayoutMath.AlignInCell(GridAlign.End, 100f, 50f), 0.01f);
			Assert.AreEqual(0f, GridLayoutMath.AlignInCell(GridAlign.Start, 100f, 50f), 0.01f);
		}
	}
}
