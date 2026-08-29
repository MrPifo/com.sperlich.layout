using PrimeTween;
using UnityEngine;

namespace Sperlich.UISystem {

	/// <summary>
	/// PrimeTween-Helfer, um abgeleitete Layout-Größen (FlexSize-Werte, Padding) weich zu animieren.
	/// Nutzt <see cref="Tween.Custom"/>, weil ein berechneter Wert getweent wird, kein einzelnes Transform-Feld.
	/// Der Container rebuildt bei jeder Wertänderung automatisch (die Setter markieren dirty).
	/// </summary>
	public static class LayoutTweenExtensions {

		/// <summary>Animiert den Zahlenwert der Breiten-<see cref="FlexSize"/> (Modus bleibt unverändert).</summary>
		public static Tween AnimateWidth(this FlexElement element, float targetValue, float duration, Ease ease = Ease.InOutSine) {
			FlexSize start = element.Width;
			return Tween.Custom(start.value, targetValue, duration, v => {
				FlexSize s = element.Width;
				s.value = v;
				element.Width = s;
			}, ease);
		}

		/// <summary>Animiert den Zahlenwert der Höhen-<see cref="FlexSize"/> (Modus bleibt unverändert).</summary>
		public static Tween AnimateHeight(this FlexElement element, float targetValue, float duration, Ease ease = Ease.InOutSine) {
			FlexSize start = element.Height;
			return Tween.Custom(start.value, targetValue, duration, v => {
				FlexSize s = element.Height;
				s.value = v;
				element.Height = s;
			}, ease);
		}

		/// <summary>Animiert das Padding eines Layout-Containers (alle vier Seiten gemeinsam).</summary>
		public static Tween AnimatePadding(this LayoutContainerBase container, RectOffset target, float duration, Ease ease = Ease.InOutSine) {
			RectOffset from = new RectOffset(container.Padding.left, container.Padding.right, container.Padding.top, container.Padding.bottom);
			return Tween.Custom(0f, 1f, duration, t => {
				container.Padding.left = Mathf.RoundToInt(Mathf.Lerp(from.left, target.left, t));
				container.Padding.right = Mathf.RoundToInt(Mathf.Lerp(from.right, target.right, t));
				container.Padding.top = Mathf.RoundToInt(Mathf.Lerp(from.top, target.top, t));
				container.Padding.bottom = Mathf.RoundToInt(Mathf.Lerp(from.bottom, target.bottom, t));
				container.RequestRebuild();
			}, ease);
		}

		/// <summary>Animiert den Spalt (gap) eines Layout-Containers.</summary>
		public static Tween AnimateGap(this LayoutContainerBase container, Vector2 target, float duration, Ease ease = Ease.InOutSine) {
			Vector2 from = container.Gap;
			return Tween.Custom(from, target, duration, v => container.Gap = v, ease);
		}
	}
}
