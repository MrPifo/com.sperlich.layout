using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sperlich.UISystem {

	/// <summary>
	/// CSS-Flexbox-artiger Layout-Container. Ersetzt Unitys Horizontal-/VerticalLayoutGroup (+ ContentSizeFitter)
	/// durch eine einzelne Komponente mit direction/wrap/justify/align, individuell überschreibbaren Kindgrößen
	/// (<see cref="FlexElement"/>) und eingebauter Auto-Content-Size.
	/// </summary>
	[AddComponentMenu("Sperlich UI/Flex Container")]
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public class FlexContainer : LayoutContainerBase {

		[SerializeField] private FlexDirection direction = FlexDirection.Row;
		[SerializeField] private FlexWrap wrap = FlexWrap.NoWrap;
		[SerializeField] private JustifyContent justifyContent = JustifyContent.Start;
		[SerializeField] private AlignItems alignItems = AlignItems.Start;
		[SerializeField, Tooltip("Nur bei aktivem Wrap: verteilt mehrere Zeilen im Kreuzraum.")]
		private AlignContent alignContent = AlignContent.Start;

		public FlexDirection Direction { get => direction; set { direction = value; SetDirty(); } }
		public FlexWrap Wrap { get => wrap; set { wrap = value; SetDirty(); } }
		public JustifyContent JustifyContent { get => justifyContent; set { justifyContent = value; SetDirty(); } }
		public AlignItems AlignItems { get => alignItems; set { alignItems = value; SetDirty(); } }
		public AlignContent AlignContent { get => alignContent; set { alignContent = value; SetDirty(); } }

		private int MainAxis => (direction == FlexDirection.Column || direction == FlexDirection.ColumnReverse) ? 1 : 0;
		private bool MainReversed => direction == FlexDirection.RowReverse || direction == FlexDirection.ColumnReverse;
		private bool DoWrap => wrap != FlexWrap.NoWrap;

		private struct Item {
			public RectTransform rect;
			public float baseMain;
			public float baseCross;
			public float grow;
			public float shrink;
			public float minMain;
			public float maxMain;
			public bool crossStretch;
			public AlignSelf alignSelf;
			public float mainSize;
			public float mainStart;
			public float crossSize;
			public float crossStart;
		}

		private readonly List<Item> items = new List<Item>();

		private void BuildItems() {
			items.Clear();
			int main = MainAxis;
			int cross = 1 - main;
			float basisMain = main == 0 ? InnerWidth : InnerHeight;

			for (int i = 0; i < children.Count; i++) {
				RectTransform child = children[i];
				FlexElement fe = FlexOf(child);

				FlexSize sizeMain = fe != null ? (main == 0 ? fe.Width : fe.Height) : FlexSize.Ignored;
				FlexSize sizeCross = fe != null ? (cross == 0 ? fe.Width : fe.Height) : FlexSize.Ignored;

				Item it = new Item {
					rect = child,
					baseMain = ResolveBase(child, fe, main, sizeMain),
					baseCross = ResolveBase(child, fe, cross, sizeCross),
					grow = fe != null ? sizeMain.EffectiveGrow : 0f,
					shrink = fe != null ? sizeMain.EffectiveShrink : 1f,
					minMain = fe != null ? sizeMain.ResolvedMin(basisMain) : 0f,
					maxMain = fe != null ? sizeMain.ResolvedMax(basisMain) : float.PositiveInfinity,
					crossStretch = fe == null || sizeCross.mode == FlexMode.Ignore,
					alignSelf = fe != null ? fe.AlignSelf : AlignSelf.Auto,
				};
				items.Add(it);
			}
		}

		private static float ResolveBase(RectTransform child, FlexElement fe, int axis, FlexSize size) {
			if (fe == null) {
				return Mathf.Max(0f, PreferredOf(child, axis));
			}
			switch (size.mode) {
				case FlexMode.Ignore:
					return Mathf.Max(0f, PreferredOf(child, axis));
				case FlexMode.Flexible:
					return 0f;
				default:
					float resolved = axis == 0 ? fe.preferredWidth : fe.preferredHeight;
					return resolved >= 0f ? resolved : Mathf.Max(0f, PreferredOf(child, axis));
			}
		}

		private List<FlexMathItem> ToMathItems() {
			var list = new List<FlexMathItem>(items.Count);
			for (int i = 0; i < items.Count; i++) {
				Item it = items[i];
				list.Add(new FlexMathItem {
					baseMain = it.baseMain,
					grow = it.grow,
					shrink = it.shrink,
					minMain = it.minMain,
					maxMain = it.maxMain,
				});
			}
			return list;
		}

		protected override void CalculateContentSize(int axis) {
			BuildItems();
			int main = MainAxis;
			float mainGap = main == 0 ? gap.x : gap.y;
			float crossGap = (1 - main) == 0 ? gap.x : gap.y;
			float pad = axis == 0 ? Padding.horizontal : Padding.vertical;

			float preferred;
			float min;

			if (axis == main) {
				float sum = 0f;
				float largestMin = 0f;
				for (int i = 0; i < items.Count; i++) {
					sum += items[i].baseMain;
					largestMin = Mathf.Max(largestMin, items[i].minMain > 0f ? items[i].minMain : items[i].baseMain);
				}
				sum += mainGap * Mathf.Max(0, items.Count - 1);
				preferred = pad + sum;
				min = pad + largestMin;
			} else {
				float innerMain = main == 0 ? InnerWidth : InnerHeight;
				List<Vector2Int> lines = FlexLayoutMath.SplitIntoLines(ToMathItems(), innerMain, mainGap, DoWrap);
				float total = 0f;
				for (int li = 0; li < lines.Count; li++) {
					float lineCross = 0f;
					Vector2Int line = lines[li];
					for (int i = 0; i < line.y; i++) {
						lineCross = Mathf.Max(lineCross, items[line.x + i].baseCross);
					}
					total += lineCross;
				}
				total += crossGap * Mathf.Max(0, lines.Count - 1);
				preferred = pad + total;
				min = preferred;
			}

			if (axis == 0) {
				m_MinWidth = min;
				m_PreferredWidth = preferred;
			} else {
				m_MinHeight = min;
				m_PreferredHeight = preferred;
			}
		}

		protected override void Arrange(int applyAxis) {
			if (children.Count == 0) {
				return;
			}

			BuildItems();
			int main = MainAxis;
			int cross = 1 - main;
			float innerMain = main == 0 ? InnerWidth : InnerHeight;
			float innerCross = cross == 0 ? InnerWidth : InnerHeight;
			float mainGap = main == 0 ? gap.x : gap.y;
			float crossGap = cross == 0 ? gap.x : gap.y;

			List<FlexMathItem> mathItems = ToMathItems();
			List<Vector2Int> lines = FlexLayoutMath.SplitIntoLines(mathItems, innerMain, mainGap, DoWrap);

			float[] lineCrossSizes = new float[lines.Count];
			float sizeScratchMax = 0;
			for (int li = 0; li < lines.Count; li++) {
				sizeScratchMax = Mathf.Max(sizeScratchMax, lines[li].y);
			}
			float[] sizeScratch = new float[Mathf.Max(1, (int)sizeScratchMax)];
			float[] posScratch = new float[sizeScratch.Length];

			for (int li = 0; li < lines.Count; li++) {
				Vector2Int line = lines[li];
				FlexLayoutMath.ResolveFlexibleLengths(mathItems, line.x, line.y, innerMain, mainGap, sizeScratch);
				FlexLayoutMath.Distribute(sizeScratch, line.y, innerMain, mainGap, justifyContent, posScratch);

				float lineCross = 0f;
				for (int i = 0; i < line.y; i++) {
					Item it = items[line.x + i];
					it.mainSize = sizeScratch[i];
					it.mainStart = posScratch[i];
					items[line.x + i] = it;
					lineCross = Mathf.Max(lineCross, it.baseCross);
				}
				lineCrossSizes[li] = lineCross;
			}

			float[] linePositions = new float[lines.Count];
			if (DoWrap == false) {
				// Single-Line-Flexbox: align-content entfällt, die Zeile füllt die volle Kreuzachse (wie CSS-Default).
				lineCrossSizes[0] = Mathf.Max(lineCrossSizes[0], innerCross);
				linePositions[0] = 0f;
			} else {
				if (alignContent == AlignContent.Stretch && lines.Count > 0) {
					float totalLineCross = 0f;
					for (int li = 0; li < lines.Count; li++) {
						totalLineCross += lineCrossSizes[li];
					}
					totalLineCross += crossGap * Mathf.Max(0, lines.Count - 1);
					float extra = Mathf.Max(0f, innerCross - totalLineCross) / lines.Count;
					for (int li = 0; li < lines.Count; li++) {
						lineCrossSizes[li] += extra;
					}
				}
				FlexLayoutMath.Distribute(lineCrossSizes, lines.Count, innerCross, crossGap, FlexLayoutMath.ToDistribution(alignContent), linePositions);
			}

			bool wrapReverse = wrap == FlexWrap.WrapReverse;

			for (int li = 0; li < lines.Count; li++) {
				Vector2Int line = lines[li];
				float lineCross = lineCrossSizes[li];
				float linePos = wrapReverse
					? innerCross - linePositions[li] - lineCross
					: linePositions[li];

				for (int i = 0; i < line.y; i++) {
					Item it = items[line.x + i];
					AlignItems effective = FlexLayoutMath.ResolveAlignSelf(it.alignSelf, alignItems);

					float itemCross = (effective == AlignItems.Stretch && it.crossStretch) ? lineCross : it.baseCross;
					float crossOffset = FlexLayoutMath.AlignCross(effective, lineCross, itemCross);

					float mainStart = MainReversed ? innerMain - it.mainStart - it.mainSize : it.mainStart;
					float crossStart = linePos + crossOffset;

					float x = (main == 0 ? mainStart : crossStart) + Padding.left;
					float y = (main == 0 ? crossStart : mainStart) + Padding.top;
					float w = main == 0 ? it.mainSize : itemCross;
					float h = main == 0 ? itemCross : it.mainSize;

					it.crossSize = itemCross;
					it.crossStart = crossStart;
					items[line.x + i] = it;

					if (applyAxis == 0) {
						WriteChildAxis(it.rect, 0, x, w);
					} else {
						WriteChildAxis(it.rect, 1, y, h);
					}
				}
			}
		}
	}
}
