using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.UISystem {

	/// <summary>
	/// Reine, Unity-Component-freie CSS-Grid-Berechnungen (Track-Sizing mit Pixels/fr/auto/minmax).
	/// Wird von <see cref="GridContainer"/> genutzt und ist über EditMode-Tests abgedeckt.
	/// </summary>
	public static class GridLayoutMath {

		/// <summary>
		/// Löst die Pixelgrößen aller Tracks auf: feste &amp; Auto-Tracks zuerst, danach Restplatz proportional
		/// auf die fr-Tracks (Single-Pass; Floors reduzieren das Budget ohne Reflow — dokumentierter v1-Kompromiss).
		/// </summary>
		/// <param name="autoSizes">Content-Größe je Track (für Auto und MinMax mit Content-Min). Länge == tracks.Count.</param>
		public static void ResolveTrackSizes(IReadOnlyList<GridTrack> tracks, float available, float gap, IReadOnlyList<float> autoSizes, float[] outSizes) {
			int n = tracks.Count;
			if (n == 0) {
				return;
			}

			float remaining = available - gap * Mathf.Max(0, n - 1);
			float[] frWeight = new float[n];
			float[] frFloor = new float[n];

			for (int i = 0; i < n; i++) {
				GridTrack t = tracks[i];
				float auto = (autoSizes != null && i < autoSizes.Count) ? Mathf.Max(0f, autoSizes[i]) : 0f;

				switch (t.mode) {
					case GridTrackMode.Pixels:
						outSizes[i] = Mathf.Max(0f, t.value);
						remaining -= outSizes[i];
						break;

					case GridTrackMode.Auto:
						outSizes[i] = auto;
						remaining -= outSizes[i];
						break;

					case GridTrackMode.Fraction:
						// fr-Tracks belegen keinen festen Platz; ihr Floor wird erst nach der fr-Verteilung
						// als Untergrenze angewendet (kann bei sehr großen Floors zu Overflow führen — v1).
						frWeight[i] = Mathf.Max(0f, t.value);
						frFloor[i] = Mathf.Max(0f, t.minPx);
						outSizes[i] = 0f;
						break;

					case GridTrackMode.MinMax:
						float mn = t.minPx > 0f ? t.minPx : auto;
						if (t.maxIsFraction) {
							frWeight[i] = Mathf.Max(0f, t.maxValue);
							frFloor[i] = mn;
							outSizes[i] = 0f;
						} else {
							float mx = Mathf.Max(mn, t.maxValue);
							float wanted = auto > 0f ? auto : mn;
							outSizes[i] = Mathf.Clamp(wanted, mn, mx);
							remaining -= outSizes[i];
						}
						break;
				}
			}

			float totalFr = 0f;
			for (int i = 0; i < n; i++) {
				totalFr += frWeight[i];
			}

			if (totalFr > 0f && remaining > 0f) {
				float unit = remaining / totalFr;
				for (int i = 0; i < n; i++) {
					if (frWeight[i] > 0f) {
						outSizes[i] = Mathf.Max(frFloor[i], unit * frWeight[i]);
					}
				}
			}

			for (int i = 0; i < n; i++) {
				outSizes[i] = Mathf.Max(0f, outSizes[i]);
			}
		}

		/// <summary>Anzahl der Tracks für repeat(auto-fill / auto-fit, minmax(minSize, ...)) bei gegebener Containerbreite.</summary>
		public static int ResolveRepeatCount(float available, float gap, float trackMinSize) {
			if (trackMinSize <= 0f) {
				return 1;
			}
			int count = Mathf.FloorToInt((available + gap) / (trackMinSize + gap));
			return Mathf.Max(1, count);
		}

		/// <summary>Kumulierter Startoffset von Track <paramref name="index"/> (inkl. Gaps und Start-Padding).</summary>
		public static float TrackOffset(float[] sizes, int index, float gap, float startPadding) {
			float offset = startPadding;
			for (int i = 0; i < index; i++) {
				offset += sizes[i] + gap;
			}
			return offset;
		}

		/// <summary>Spannweite über <paramref name="span"/> Tracks ab <paramref name="start"/> (inkl. innerer Gaps).</summary>
		public static float TrackSpan(float[] sizes, int start, int span, float gap) {
			float total = 0f;
			int end = Mathf.Min(sizes.Length, start + span);
			for (int i = start; i < end; i++) {
				total += sizes[i];
			}
			total += gap * Mathf.Max(0, (end - start) - 1);
			return total;
		}

		public static float AlignInCell(GridAlign align, float cellSize, float itemSize) {
			switch (align) {
				case GridAlign.End: return cellSize - itemSize;
				case GridAlign.Center: return (cellSize - itemSize) * 0.5f;
				default: return 0f;
			}
		}
	}
}
