using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Sperlich.UISystem {
	public static class FlexLayoutUtility {

		private static readonly List<ILayoutElement> elementBuffer = new List<ILayoutElement>();

		public static RectTransform GetParent(RectTransform rect) => rect.parent as RectTransform;

		public static bool HasLayoutGroupParent(RectTransform rect) {
			RectTransform parent = GetParent(rect);
			if (parent == null) {
				return false;
			}
			return parent.TryGetComponent(out ILayoutGroup group) && (group as Behaviour).isActiveAndEnabled;
		}

		public static float GetBasis(RectTransform rect, int axis, PercentBasis basis) {
			RectTransform parent = GetParent(rect);
			if (parent == null) {
				return 0f;
			}

			float size = axis == 0 ? parent.rect.width : parent.rect.height;
			if (basis == PercentBasis.FullParentSize || parent.TryGetComponent(out LayoutGroup group) == false || group.isActiveAndEnabled == false) {
				return Mathf.Max(0f, size);
			}

			RectOffset padding = group.padding;
			size -= axis == 0 ? padding.horizontal : padding.vertical;

			if (basis == PercentBasis.ParentSizeMinusPaddingAndSpacing && group is HorizontalOrVerticalLayoutGroup hv) {
				bool groupAxis = (group is HorizontalLayoutGroup && axis == 0) || (group is VerticalLayoutGroup && axis == 1);
				if (groupAxis) {
					size -= hv.spacing * Mathf.Max(0, CountLayoutChildren(parent) - 1);
				}
			}

			return Mathf.Max(0f, size);
		}

		public static int CountLayoutChildren(RectTransform parent) {
			int count = 0;
			for (int i = 0; i < parent.childCount; i++) {
				if (parent.GetChild(i) is RectTransform child == false || child.gameObject.activeInHierarchy == false) {
					continue;
				}
				if (child.TryGetComponent(out ILayoutIgnorer ignorer) && ignorer.ignoreLayout) {
					continue;
				}
				count++;
			}
			return count;
		}

		public static void GetContentSize(GameObject target, ILayoutElement exclude, int axis, out float min, out float preferred) {
			min = 0f;
			preferred = 0f;

			target.GetComponents(elementBuffer);
			for (int i = 0; i < elementBuffer.Count; i++) {
				ILayoutElement element = elementBuffer[i];
				if (ReferenceEquals(element, exclude) || (element as Behaviour).isActiveAndEnabled == false) {
					continue;
				}

				if (axis == 0) {
					element.CalculateLayoutInputHorizontal();
					min = Mathf.Max(min, element.minWidth);
					preferred = Mathf.Max(preferred, element.preferredWidth);
				} else {
					element.CalculateLayoutInputVertical();
					min = Mathf.Max(min, element.minHeight);
					preferred = Mathf.Max(preferred, element.preferredHeight);
				}
			}
			elementBuffer.Clear();

			preferred = Mathf.Max(preferred, min);
		}
	}
}
