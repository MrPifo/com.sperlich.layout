using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.UISystem {

	/// <summary>Beschreibt ein Flex-Item für die reine Rechen-Ebene (ohne Unity-Component-Bezug), damit die Algorithmen testbar bleiben.</summary>
	public struct FlexMathItem {
		/// <summary>Basisgröße auf der Hauptachse (CSS flex-basis, bereits aufgelöst).</summary>
		public float baseMain;
		public float grow;
		public float shrink;
		/// <summary>Aufgelöster Min-Clamp der Hauptachse (0 = keiner).</summary>
		public float minMain;
		/// <summary>Aufgelöster Max-Clamp der Hauptachse (<see cref="float.PositiveInfinity"/> = keiner).</summary>
		public float maxMain;
	}

	/// <summary>
	/// Reine, Unity-Component-freie Flexbox-Berechnungen (Line-Splitting, Grow/Shrink-Verteilung,
	/// Haupt-/Kreuzachsen-Ausrichtung). Wird von <see cref="FlexContainer"/> genutzt und ist über EditMode-Tests abgedeckt.
	/// </summary>
	public static class FlexLayoutMath {

		/// <summary>Greedy Line-Splitting auf der Hauptachse. Ergebnis: je Zeile (Startindex, Anzahl).</summary>
		public static List<Vector2Int> SplitIntoLines(IReadOnlyList<FlexMathItem> items, float availableMain, float mainGap, bool wrap) {
			var lines = new List<Vector2Int>();
			if (items.Count == 0) {
				return lines;
			}
			if (wrap == false) {
				lines.Add(new Vector2Int(0, items.Count));
				return lines;
			}

			int lineStart = 0;
			float lineMain = 0f;
			for (int i = 0; i < items.Count; i++) {
				float itemMain = Mathf.Max(0f, items[i].baseMain);
				if (i == lineStart) {
					lineMain = itemMain;
					continue;
				}
				float projected = lineMain + mainGap + itemMain;
				if (projected > availableMain + 0.01f) {
					lines.Add(new Vector2Int(lineStart, i - lineStart));
					lineStart = i;
					lineMain = itemMain;
				} else {
					lineMain = projected;
				}
			}
			lines.Add(new Vector2Int(lineStart, items.Count - lineStart));
			return lines;
		}

		/// <summary>
		/// Löst die endgültigen Hauptachsen-Größen einer Zeile über grow/shrink auf (Single-Pass mit Min/Max-Clamp,
		/// dokumentierter v1-Kompromiss gegenüber der iterativen CSS-Freeze-Konvergenz).
		/// </summary>
		public static void ResolveFlexibleLengths(IReadOnlyList<FlexMathItem> items, int start, int count, float availableMain, float mainGap, float[] outSizes) {
			if (count <= 0) {
				return;
			}

			float totalBase = 0f;
			for (int i = 0; i < count; i++) {
				totalBase += Mathf.Max(0f, items[start + i].baseMain);
			}
			float totalGap = mainGap * Mathf.Max(0, count - 1);
			float free = availableMain - totalBase - totalGap;

			if (Mathf.Abs(free) < 0.01f) {
				for (int i = 0; i < count; i++) {
					outSizes[i] = ClampItem(items[start + i], items[start + i].baseMain);
				}
				return;
			}

			if (free > 0f) {
				float totalGrow = 0f;
				for (int i = 0; i < count; i++) {
					totalGrow += Mathf.Max(0f, items[start + i].grow);
				}
				for (int i = 0; i < count; i++) {
					FlexMathItem it = items[start + i];
					float target = totalGrow > 0f
						? it.baseMain + free * (Mathf.Max(0f, it.grow) / totalGrow)
						: it.baseMain;
					outSizes[i] = ClampItem(it, target);
				}
				return;
			}

			float totalScaled = 0f;
			for (int i = 0; i < count; i++) {
				FlexMathItem it = items[start + i];
				totalScaled += Mathf.Max(0f, it.shrink) * Mathf.Max(0f, it.baseMain);
			}
			for (int i = 0; i < count; i++) {
				FlexMathItem it = items[start + i];
				float target;
				if (totalScaled > 0f) {
					float ratio = (Mathf.Max(0f, it.shrink) * Mathf.Max(0f, it.baseMain)) / totalScaled;
					target = it.baseMain + free * ratio;
				} else {
					target = it.baseMain;
				}
				outSizes[i] = ClampItem(it, target);
			}
		}

		private static float ClampItem(FlexMathItem it, float value) {
			value = Mathf.Max(value, Mathf.Max(0f, it.minMain));
			if (float.IsPositiveInfinity(it.maxMain) == false && it.maxMain > 0f) {
				value = Mathf.Min(value, it.maxMain);
			}
			return Mathf.Max(0f, value);
		}

		/// <summary>Berechnet Startoffsets je Item entlang einer Achse anhand von justify-content / align-content.</summary>
		public static void Distribute(float[] sizes, int count, float available, float gap, JustifyContent mode, float[] outPositions) {
			float used = 0f;
			for (int i = 0; i < count; i++) {
				used += sizes[i];
			}
			float remaining = available - used - gap * Mathf.Max(0, count - 1);

			float cursor = 0f;
			float between = gap;
			switch (mode) {
				case JustifyContent.Start:
					cursor = 0f;
					break;
				case JustifyContent.End:
					cursor = remaining;
					break;
				case JustifyContent.Center:
					cursor = remaining * 0.5f;
					break;
				case JustifyContent.SpaceBetween:
					if (count > 1) {
						between = gap + remaining / (count - 1);
					} else {
						cursor = 0f;
					}
					break;
				case JustifyContent.SpaceAround:
					if (count > 0) {
						float unit = remaining / count;
						cursor = unit * 0.5f;
						between = gap + unit;
					}
					break;
				case JustifyContent.SpaceEvenly:
					float step = remaining / (count + 1);
					cursor = step;
					between = gap + step;
					break;
			}

			for (int i = 0; i < count; i++) {
				outPositions[i] = cursor;
				cursor += sizes[i] + between;
			}
		}

		/// <summary>Konvertiert <see cref="AlignContent"/> in das kompatible <see cref="JustifyContent"/>-Verteilungsmuster (Stretch wird separat behandelt).</summary>
		public static JustifyContent ToDistribution(AlignContent align) {
			switch (align) {
				case AlignContent.End: return JustifyContent.End;
				case AlignContent.Center: return JustifyContent.Center;
				case AlignContent.SpaceBetween: return JustifyContent.SpaceBetween;
				case AlignContent.SpaceAround: return JustifyContent.SpaceAround;
				case AlignContent.SpaceEvenly: return JustifyContent.SpaceEvenly;
				default: return JustifyContent.Start;
			}
		}

		/// <summary>Kreuzachsen-Offset eines Items in seiner Zeile anhand von align-items / align-self.</summary>
		public static float AlignCross(AlignItems effective, float lineCross, float itemCross) {
			switch (effective) {
				case AlignItems.End: return lineCross - itemCross;
				case AlignItems.Center: return (lineCross - itemCross) * 0.5f;
				default: return 0f;
			}
		}

		/// <summary>Löst <see cref="AlignSelf.Auto"/> gegen den Container-Wert auf.</summary>
		public static AlignItems ResolveAlignSelf(AlignSelf self, AlignItems containerDefault) {
			switch (self) {
				case AlignSelf.Start: return AlignItems.Start;
				case AlignSelf.End: return AlignItems.End;
				case AlignSelf.Center: return AlignItems.Center;
				case AlignSelf.Stretch: return AlignItems.Stretch;
				default: return containerDefault;
			}
		}
	}
}
