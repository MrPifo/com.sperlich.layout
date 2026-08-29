using System;
using UnityEngine;

namespace Sperlich.UISystem {

	/// <summary>Definition einer Grid-Spalte oder -Zeile (CSS grid-template-columns/-rows Eintrag).</summary>
	[Serializable]
	public struct GridTrack {

		public GridTrackMode mode;
		/// <summary>Pixels: Pixelgröße. Fraction: fr-Gewicht.</summary>
		public float value;
		/// <summary>MinMax: Min-Seite in Pixeln (0 = Content-Größe). Sonst optionaler Mindestwert.</summary>
		public float minPx;
		/// <summary>MinMax: Wert der Max-Seite (Pixel oder fr, je nach <see cref="maxIsFraction"/>).</summary>
		public float maxValue;
		/// <summary>MinMax: true = Max-Seite ist ein fr-Anteil, false = feste Pixel-Obergrenze.</summary>
		public bool maxIsFraction;

		public static GridTrack Pixels(float px) => new GridTrack { mode = GridTrackMode.Pixels, value = Mathf.Max(0f, px) };
		public static GridTrack Fr(float fr) => new GridTrack { mode = GridTrackMode.Fraction, value = Mathf.Max(0f, fr) };
		public static GridTrack Auto() => new GridTrack { mode = GridTrackMode.Auto };
		public static GridTrack MinMax(float minPixels, float maxFraction) => new GridTrack {
			mode = GridTrackMode.MinMax,
			minPx = Mathf.Max(0f, minPixels),
			maxValue = Mathf.Max(0f, maxFraction),
			maxIsFraction = true,
		};

		/// <summary>fr-Gewicht dieses Tracks (0, wenn kein flexibler Anteil).</summary>
		public readonly float FractionWeight {
			get {
				if (mode == GridTrackMode.Fraction) {
					return Mathf.Max(0f, value);
				}
				if (mode == GridTrackMode.MinMax && maxIsFraction) {
					return Mathf.Max(0f, maxValue);
				}
				return 0f;
			}
		}
	}
}
