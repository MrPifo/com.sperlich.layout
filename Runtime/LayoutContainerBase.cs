using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Sperlich.UISystem {

	/// <summary>
	/// Gemeinsame Basis für <see cref="FlexContainer"/> und <see cref="GridContainer"/>: Kind-Erfassung,
	/// Dirty-Marking über Unitys <see cref="LayoutRebuilder"/>, das Schreiben von Kind-Position/-Größe
	/// und die "Auto-Content-Size"-Anbindung über <see cref="ILayoutElement"/>.
	/// Erbt bewusst NICHT von Unitys LayoutGroup-Komponenten, sondern implementiert nur deren generische Interfaces.
	/// </summary>
	public abstract class LayoutContainerBase : UIBehaviour, ILayoutGroup, ILayoutElement {

		[SerializeField] protected RectOffset padding = new RectOffset();
		[SerializeField, Tooltip("Abstand zwischen den Kindern. X = horizontaler Spalt, Y = vertikaler Spalt.")]
		protected Vector2 gap = Vector2.zero;

		public RectOffset Padding {
			get { padding ??= new RectOffset(); return padding; }
			set { padding = value ?? new RectOffset(); SetDirty(); }
		}
		public Vector2 Gap { get => gap; set { gap = value; SetDirty(); } }

		protected float m_MinWidth, m_PreferredWidth, m_MinHeight, m_PreferredHeight;

		public float minWidth => m_MinWidth;
		public float preferredWidth => m_PreferredWidth;
		public float flexibleWidth => -1f;
		public float minHeight => m_MinHeight;
		public float preferredHeight => m_PreferredHeight;
		public float flexibleHeight => -1f;
		public int layoutPriority => 1;

		private RectTransform cachedRect;
		protected RectTransform SelfRect {
			get {
				if (cachedRect == null) {
					cachedRect = transform as RectTransform;
				}
				return cachedRect;
			}
		}

		protected DrivenRectTransformTracker tracker;
		protected readonly List<RectTransform> children = new List<RectTransform>();

		protected float InnerWidth => Mathf.Max(0f, SelfRect.rect.width - Padding.horizontal);
		protected float InnerHeight => Mathf.Max(0f, SelfRect.rect.height - Padding.vertical);

		protected override void OnEnable() {
			base.OnEnable();
			SetDirty();
		}

		protected override void OnDisable() {
			tracker.Clear();
			LayoutRebuilder.MarkLayoutForRebuild(SelfRect);
			base.OnDisable();
		}

		protected override void OnRectTransformDimensionsChange() => SetDirty();
		protected override void OnTransformParentChanged() => SetDirty();
		protected override void OnDidApplyAnimationProperties() => SetDirty();

		protected virtual void OnTransformChildrenChanged() => SetDirty();

#if UNITY_EDITOR
		protected override void OnValidate() {
			base.OnValidate();
			SetDirty();
		}

		private bool editorRebuildQueued;
#endif

		/// <summary>Erzwingt einen Layout-Rebuild von außen (z.B. nach Padding-/Gap-Änderungen per Script).</summary>
		public void RequestRebuild() => SetDirty();

		protected void SetDirty() {
			if (IsActive() == false) {
				return;
			}
			LayoutRebuilder.MarkLayoutForRebuild(SelfRect);
#if UNITY_EDITOR
			if (Application.isPlaying == false) {
				if (editorRebuildQueued == false) {
					editorRebuildQueued = true;
					UnityEditor.EditorApplication.delayCall += EditorForceRebuild;
				}
			}
#endif
		}

#if UNITY_EDITOR
		private void EditorForceRebuild() {
			editorRebuildQueued = false;
			if (this == null || SelfRect == null) {
				return;
			}
			LayoutRebuilder.ForceRebuildLayoutImmediate(SelfRect);
		}
#endif

		protected void CollectChildren() {
			children.Clear();
			RectTransform self = SelfRect;
			for (int i = 0; i < self.childCount; i++) {
				if (self.GetChild(i) is RectTransform child == false) {
					continue;
				}
				if (child.gameObject.activeInHierarchy == false) {
					continue;
				}
				if (child.TryGetComponent(out ILayoutIgnorer ignorer) && ignorer.ignoreLayout) {
					continue;
				}
				children.Add(child);
			}
			SortChildrenByOrder();
		}

		/// <summary>Stabile Sortierung nach <see cref="FlexElement.Order"/> (CSS 'order'); Elemente ohne FlexElement behalten Order 0.</summary>
		private void SortChildrenByOrder() {
			for (int i = 1; i < children.Count; i++) {
				RectTransform key = children[i];
				int keyOrder = OrderOf(key);
				int j = i - 1;
				while (j >= 0 && OrderOf(children[j]) > keyOrder) {
					children[j + 1] = children[j];
					j--;
				}
				children[j + 1] = key;
			}
		}

		private static int OrderOf(RectTransform rect) => rect.TryGetComponent(out FlexElement fe) ? fe.Order : 0;

		protected static FlexElement FlexOf(RectTransform rect) => rect.TryGetComponent(out FlexElement fe) && fe.isActiveAndEnabled ? fe : null;

		// --- ILayoutElement -------------------------------------------------

		public virtual void CalculateLayoutInputHorizontal() {
			CollectChildren();
			CalculateContentSize(0);
		}

		public virtual void CalculateLayoutInputVertical() => CalculateContentSize(1);

		// --- ILayoutController --------------------------------------------

		public virtual void SetLayoutHorizontal() {
			tracker.Clear();
			Arrange(0);
		}

		public virtual void SetLayoutVertical() => Arrange(1);

		/// <summary>Füllt m_MinWidth/m_PreferredWidth bzw. Height für die "Auto-Content-Size" ohne ContentSizeFitter.</summary>
		protected abstract void CalculateContentSize(int axis);

		/// <summary>Ordnet die Kinder an und schreibt die Werte der übergebenen Achse (0 = horizontal, 1 = vertikal).</summary>
		protected abstract void Arrange(int applyAxis);

		// --- Kind schreiben ---------------------------------------------------

		protected void WriteChildAxis(RectTransform rect, int axis, float pos, float size) {
			tracker.Add(this, rect, DrivenTransformProperties.Anchors |
				(axis == 0
					? (DrivenTransformProperties.AnchoredPositionX | DrivenTransformProperties.SizeDeltaX)
					: (DrivenTransformProperties.AnchoredPositionY | DrivenTransformProperties.SizeDeltaY)));

			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(0f, 1f);

			float anchored = axis == 0
				? pos + size * rect.pivot.x
				: -pos - size * (1f - rect.pivot.y);

			Vector2 sd = rect.sizeDelta;
			sd[axis] = size;
			rect.sizeDelta = sd;

			Vector2 ap = rect.anchoredPosition;
			ap[axis] = anchored;
			rect.anchoredPosition = ap;
		}

		protected static float PreferredOf(RectTransform child, int axis) {
			float pref = LayoutUtility.GetPreferredSize(child, axis);
			if (pref > 0f) {
				return pref;
			}
			float sd = axis == 0 ? child.sizeDelta.x : child.sizeDelta.y;
			if (sd > 0f) {
				return sd;
			}
			return Mathf.Max(0f, axis == 0 ? child.rect.width : child.rect.height);
		}
	}
}
