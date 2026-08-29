using PrimeTween;

namespace Sperlich.UISystem {

	/// <summary>
	/// Wird von Layout-Containern (und optional eigenen Custom-Components) implementiert, die ihre
	/// Positions-/Größenänderungen weich interpolieren wollen statt hart zu snappen (CSS-transition-Äquivalent).
	/// </summary>
	public interface IAnimatableLayout {
		/// <summary>Wenn true, werden Kind-Layoutänderungen von Frame zu Frame per PrimeTween interpoliert.</summary>
		bool AnimateLayoutChanges { get; }
		/// <summary>Dauer der Interpolation in Sekunden.</summary>
		float AnimationDuration { get; }
		/// <summary>Easing der Interpolation.</summary>
		Ease AnimationEase { get; }
	}
}
