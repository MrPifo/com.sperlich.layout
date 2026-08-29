using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sperlich.UISystem {

	[AddComponentMenu("Sperlich UI/Flex Element")]
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public class FlexElement : UIBehaviour, ILayoutElement, ILayoutSelfController {

		[SerializeField] private FlexSize width = FlexSize.Ignored;
		[SerializeField] private FlexSize height = FlexSize.Ignored;
		[SerializeField, Tooltip("Worauf sich Percent-Werte beziehen: volle Parent-Größe, abzüglich LayoutGroup-Padding, oder zusätzlich abzüglich Spacing.")]
		private PercentBasis percentBasis = PercentBasis.ParentSizeMinusPadding;
		[SerializeField] private bool roundToPixel = true;
		[SerializeField] private int priority = 2;
		[SerializeField, Tooltip("Überschreibt Align-Items des FlexContainer-Parents für dieses Element. Auto = erbt vom Container.")]
		private AlignSelf alignSelf = AlignSelf.Auto;
		[SerializeField, Tooltip("Visuelle Reihenfolge innerhalb des FlexContainer-Parents (CSS 'order'), unabhängig von der Hierarchie-Reihenfolge.")]
		private int order = 0;

		[SerializeField, Min(1), Tooltip("Nur unter GridContainer: über wie viele Spalten sich dieses Element erstreckt (CSS grid-column span).")]
		private int columnSpan = 1;
		[SerializeField, Min(1), Tooltip("Nur unter GridContainer: über wie viele Zeilen sich dieses Element erstreckt (CSS grid-row span).")]
		private int rowSpan = 1;
		[SerializeField, Min(0), Tooltip("Nur unter GridContainer: explizite 1-basierte Startspalte. 0 = automatische Platzierung (auto-flow).")]
		private int columnStart = 0;
		[SerializeField, Min(0), Tooltip("Nur unter GridContainer: explizite 1-basierte Startzeile. 0 = automatische Platzierung (auto-flow).")]
		private int rowStart = 0;

		public FlexSize Width { get => width; set { width = value; MarkDirty(); } }
		public FlexSize Height { get => height; set { height = value; MarkDirty(); } }
		public PercentBasis PercentBasis { get => percentBasis; set { percentBasis = value; MarkDirty(); } }
		public AlignSelf AlignSelf { get => alignSelf; set { alignSelf = value; MarkDirty(); } }
		public int Order { get => order; set { order = value; MarkDirty(); } }
		public int ColumnSpan { get => Mathf.Max(1, columnSpan); set { columnSpan = Mathf.Max(1, value); MarkDirty(); } }
		public int RowSpan { get => Mathf.Max(1, rowSpan); set { rowSpan = Mathf.Max(1, value); MarkDirty(); } }
		public int ColumnStart { get => Mathf.Max(0, columnStart); set { columnStart = Mathf.Max(0, value); MarkDirty(); } }
		public int RowStart { get => Mathf.Max(0, rowStart); set { rowStart = Mathf.Max(0, value); MarkDirty(); } }

		public float minWidth { get; private set; } = -1f;
		public float preferredWidth { get; private set; } = -1f;
		public float flexibleWidth { get; private set; } = -1f;
		public float minHeight { get; private set; } = -1f;
		public float preferredHeight { get; private set; } = -1f;
		public float flexibleHeight { get; private set; } = -1f;
		public int layoutPriority => priority;

		private RectTransform rectTransform;
		private DrivenRectTransformTracker tracker;
		private bool isResolvingContent;

		private RectTransform SelfRect {
			get {
				if (rectTransform == null) {
					rectTransform = transform as RectTransform;
				}
				return rectTransform;
			}
		}

		private void OnEnable() {
			MarkDirty();
		}

		private void OnDisable() {
			tracker.Clear();
			LayoutRebuilder.MarkLayoutForRebuild(SelfRect.parent as RectTransform ?? SelfRect);
		}

		private void OnTransformParentChanged() => MarkDirty();
		private void OnDidApplyAnimationProperties() => MarkDirty();

		private void OnRectTransformDimensionsChange() {
			if (StandaloneModeActive) {
				MarkDirty();
			}
		}

#if UNITY_EDITOR
		private void OnValidate() {
			width.value = Mathf.Max(0f, width.value);
			height.value = Mathf.Max(0f, height.value);
			MarkDirty();
		}
#endif

#if UNITY_EDITOR
		private bool editorRebuildQueued;
#endif

		private void MarkDirty() {
			if (isActiveAndEnabled == false) {
				return;
			}

			RectTransform target = SelfRect.parent as RectTransform ?? SelfRect;

#if UNITY_EDITOR
			if (Application.isPlaying == false) {
				if (editorRebuildQueued == false) {
					editorRebuildQueued = true;
					UnityEditor.EditorApplication.delayCall += () => ForceRebuildIfAlive(target);
				}
				return;
			}
#endif
			LayoutRebuilder.MarkLayoutForRebuild(target);
		}

#if UNITY_EDITOR
		private void ForceRebuildIfAlive(RectTransform target) {
			editorRebuildQueued = false;
			if (this == null || target == null) {
				return;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(target);
		}
#endif

		public bool StandaloneModeActive => FlexLayoutUtility.HasLayoutGroupParent(SelfRect) == false;

		public void CalculateLayoutInputHorizontal() {
			ResolveAxis(0, width, out float min, out float preferred, out float flexible);
			minWidth = min;
			preferredWidth = preferred;
			flexibleWidth = flexible;
		}

		public void CalculateLayoutInputVertical() {
			ResolveAxis(1, height, out float min, out float preferred, out float flexible);
			minHeight = min;
			preferredHeight = preferred;
			flexibleHeight = flexible;
		}

		private void ResolveAxis(int axis, FlexSize size, out float min, out float preferred, out float flexible) {
			flexible = -1f;

			switch (size.mode) {
				case FlexMode.Ignore:
					min = -1f;
					preferred = -1f;
					return;

				case FlexMode.Pixels: {
					float resolved = Round(size.Clamp(size.value, 0f));
					min = resolved;
					preferred = resolved;
					return;
				}

				case FlexMode.Percent: {
					float basis = FlexLayoutUtility.GetBasis(SelfRect, axis, percentBasis);
					float resolved = Round(size.Clamp(basis * size.value * 0.01f, basis));
					min = resolved;
					preferred = resolved;
					return;
				}

				case FlexMode.Flexible:
					min = 0f;
					preferred = 0f;
					flexible = Mathf.Max(0f, size.value);
					return;

				case FlexMode.Auto: {
					if (isResolvingContent) {
						min = -1f;
						preferred = -1f;
						return;
					}
					isResolvingContent = true;
					FlexLayoutUtility.GetContentSize(gameObject, this, axis, out float contentMin, out float contentPreferred);
					isResolvingContent = false;
					min = Round(size.Clamp(contentMin, 0f));
					preferred = Round(size.Clamp(contentPreferred, 0f));
					return;
				}

				case FlexMode.Aspect: {
					float otherAxisSize = axis == 0 ? SelfRect.rect.height : SelfRect.rect.width;
					float resolved = Round(size.Clamp(otherAxisSize * size.value, otherAxisSize));
					min = resolved;
					preferred = resolved;
					return;
				}

				default:
					min = -1f;
					preferred = -1f;
					return;
			}
		}

		private float Round(float value) => roundToPixel ? Mathf.Round(value) : value;

		public void SetLayoutHorizontal() => ApplyStandalone(0);
		public void SetLayoutVertical() => ApplyStandalone(1);

		private void ApplyStandalone(int axis) {
			if (StandaloneModeActive == false) {
				return;
			}

			FlexSize size = axis == 0 ? width : height;
			if (size.mode == FlexMode.Ignore || size.mode == FlexMode.Flexible) {
				tracker.Clear();
				return;
			}

			ResolveAxis(axis, size, out _, out float preferred, out _);
			if (preferred < 0f) {
				return;
			}

			tracker.Add(this, SelfRect, axis == 0 ? DrivenTransformProperties.SizeDeltaX : DrivenTransformProperties.SizeDeltaY);
			SelfRect.SetSizeWithCurrentAnchors(axis == 0 ? UnityEngine.RectTransform.Axis.Horizontal : UnityEngine.RectTransform.Axis.Vertical, preferred);
		}
	}
}
