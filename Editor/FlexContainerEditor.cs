using Sperlich.EditorKit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	[CustomEditor(typeof(FlexContainer))]
	[CanEditMultipleObjects]
	public class FlexContainerEditor : UnityEditor.Editor {

		private static readonly Color Accent = new Color(0.35f, 0.70f, 0.95f);
		private static readonly string[] DirectionLabels = { "Row", "Row ◀", "Column", "Column ▲" };

		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement { style = { paddingBottom = 4 } };

			SerializedProperty direction = serializedObject.FindProperty("direction");
			SerializedProperty wrap = serializedObject.FindProperty("wrap");
			SerializedProperty justify = serializedObject.FindProperty("justifyContent");
			SerializedProperty alignItems = serializedObject.FindProperty("alignItems");
			SerializedProperty alignContent = serializedObject.FindProperty("alignContent");
			SerializedProperty gap = serializedObject.FindProperty("gap");
			SerializedProperty padding = serializedObject.FindProperty("padding");

			var header = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					alignItems = Align.Center,
					backgroundColor = SperlichEditorTheme.BgDark,
					paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 6
				}
			};
			header.Add(new Label("Flex container") { style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary, flexGrow = 1 } });
			root.Add(header);

			var layoutBox = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			var (layoutHeader, layoutBody, _) = SperlichEditorWidgets.CreateChevronSection("Layout", true, SperlichEditorTheme.BgDark);
			layoutBody.style.paddingLeft = 6;
			layoutBody.style.paddingRight = 6;
			layoutBody.style.paddingTop = 4;
			layoutBody.style.paddingBottom = 4;

			layoutBody.Add(SperlichEditorWidgets.CreateSegmentedControl(direction, DirectionLabels, Accent, () => serializedObject.ApplyModifiedProperties()));
			layoutBody.Add(SperlichEditorWidgets.Spacer(4));
			layoutBody.Add(SperlichEditorWidgets.CreateAlignedRow("Wrap", SperlichEditorWidgets.CreateEnumDropdown(wrap)));
			layoutBody.Add(SperlichEditorWidgets.CreateAlignedRow("Justify content", SperlichEditorWidgets.CreateEnumDropdown(justify)));
			layoutBody.Add(SperlichEditorWidgets.CreateAlignedRow("Align items", SperlichEditorWidgets.CreateEnumDropdown(alignItems)));
			layoutBody.Add(SperlichEditorWidgets.CreateAlignedRow("Align content", SperlichEditorWidgets.CreateEnumDropdown(alignContent)));

			var wrapHint = new HelpBox("Align content wirkt nur, wenn Wrap aktiv ist (mehrere Zeilen).", HelpBoxMessageType.Info);
			layoutBody.Add(wrapHint);
			void RefreshHint() {
				bool noWrap = wrap.enumValueIndex == (int)FlexWrap.NoWrap;
				bool alignContentSet = alignContent.enumValueIndex != (int)AlignContent.Start;
				wrapHint.style.display = (noWrap && alignContentSet) ? DisplayStyle.Flex : DisplayStyle.None;
			}
			RefreshHint();
			root.TrackPropertyValue(wrap, _ => RefreshHint());
			root.TrackPropertyValue(alignContent, _ => RefreshHint());

			layoutBox.Add(layoutHeader);
			layoutBox.Add(layoutBody);
			root.Add(layoutBox);
			root.Add(SperlichEditorWidgets.Spacer(6));

			var spacingBox = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			var (spacingHeader, spacingBody, _) = SperlichEditorWidgets.CreateChevronSection("Spacing", true, SperlichEditorTheme.BgDark);
			spacingBody.style.paddingLeft = 6;
			spacingBody.style.paddingRight = 6;
			spacingBody.Add(new PropertyField(gap));
			spacingBody.Add(new PropertyField(padding));
			spacingBox.Add(spacingHeader);
			spacingBox.Add(spacingBody);
			root.Add(spacingBox);

			return root;
		}
	}
}
