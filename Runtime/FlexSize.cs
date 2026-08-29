using System;
using UnityEngine;

namespace Sperlich.UISystem {

	public enum FlexMode {
		Ignore = 0,
		Pixels = 1,
		Percent = 2,
		Flexible = 3,
		Auto = 4,
		Aspect = 5,
	}

	public enum FlexUnit {
		Pixels = 0,
		Percent = 1,
	}

	public enum PercentBasis {
		/// <summary>Prozent bezieht sich auf die volle Rect-Größe des Parents, inkl. eventuellem Padding einer LayoutGroup.</summary>
		FullParentSize = 0,
		/// <summary>Prozent bezieht sich auf den Parent abzüglich des Paddings seiner LayoutGroup (Default, wie CSS content-box).</summary>
		ParentSizeMinusPadding = 1,
		/// <summary>Wie ParentSizeMinusPadding, zusätzlich wird der Spacing-Platz zwischen den Geschwister-Elementen abgezogen — damit sich z.B. 3x 33.3% inkl. Abständen exakt zu 100% aufsummieren.</summary>
		ParentSizeMinusPaddingAndSpacing = 2,
	}

	[Serializable]
	public struct FlexSize {

		public FlexMode mode;
		public float value;

		/// <summary>0 = kein Min-Clamp aktiv.</summary>
		public float min;
		public FlexUnit minUnit;

		/// <summary>0 = kein Max-Clamp aktiv.</summary>
		public float max;
		public FlexUnit maxUnit;

		/// <summary>CSS flex-grow-Äquivalent. Nur innerhalb eines FlexContainer wirksam, unabhängig vom gewählten Mode.</summary>
		public float grow;
		/// <summary>CSS flex-shrink-Äquivalent. Nur innerhalb eines FlexContainer wirksam, unabhängig vom gewählten Mode. Default 1, wie in CSS.</summary>
		public float shrink;

		public readonly bool HasMin => min != 0f;
		public readonly bool HasMax => max != 0f;

		/// <summary>Liest grow/shrink für FlexContainer-Berechnungen; berücksichtigt zusätzlich den Legacy-Mode Flexible für Rückwärtskompatibilität.</summary>
		public readonly float EffectiveGrow => mode == FlexMode.Flexible ? Mathf.Max(0f, value) : Mathf.Max(0f, grow);
		/// <summary>0 gilt als "nicht gesetzt" und fällt auf den CSS-Default 1 zurück (gleiche Sentinel-Konvention wie bei Min/Max) — verhindert, dass vor dieser Erweiterung serialisierte FlexElements plötzlich shrink=0 bekommen.</summary>
		public readonly float EffectiveShrink => shrink <= 0f ? 1f : shrink;

		public static FlexSize Ignored => new FlexSize() { mode = FlexMode.Ignore, value = 0f, maxUnit = FlexUnit.Pixels, minUnit = FlexUnit.Pixels, shrink = 1f };
		public static FlexSize Pixels(float pixels) => new FlexSize() { mode = FlexMode.Pixels, value = pixels, shrink = 1f };
		public static FlexSize Percent(float percent) => new FlexSize() { mode = FlexMode.Percent, value = percent, shrink = 1f };
		public static FlexSize Flexible(float weight = 1f) => new FlexSize() { mode = FlexMode.Flexible, value = weight, grow = weight, shrink = 1f };
		public static FlexSize Auto() => new FlexSize() { mode = FlexMode.Auto, value = 0f, shrink = 1f };
		public static FlexSize Aspect(float ratio) => new FlexSize() { mode = FlexMode.Aspect, value = ratio, shrink = 1f };

		public float Clamp(float size, float basis) {
			if (HasMin) {
				size = Mathf.Max(size, Resolve(min, minUnit, basis));
			}
			if (HasMax) {
				size = Mathf.Min(size, Resolve(max, maxUnit, basis));
			}
			return size;
		}

		/// <summary>Aufgelöster Min-Clamp in Pixeln (0 = kein Min), für die FlexContainer-Grow/Shrink-Verteilung.</summary>
		public readonly float ResolvedMin(float basis) => HasMin ? Mathf.Max(0f, Resolve(min, minUnit, basis)) : 0f;

		/// <summary>Aufgelöster Max-Clamp in Pixeln (<see cref="float.PositiveInfinity"/> = kein Max), für die FlexContainer-Grow/Shrink-Verteilung.</summary>
		public readonly float ResolvedMax(float basis) => HasMax ? Mathf.Max(0f, Resolve(max, maxUnit, basis)) : float.PositiveInfinity;

		private static float Resolve(float amount, FlexUnit unit, float basis) => unit == FlexUnit.Percent ? basis * amount * 0.01f : amount;
	}
}
