using Sperlich.EditorKit;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {
	[CustomEditor(typeof(FlexElement))]
	[CanEditMultipleObjects]
	public class FlexElementEditor : UnityEditor.Editor {

		private static readonly string[] ModeLabels = { "Ignore", "Px", "%", "Grow", "Auto", "Ratio" };

		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement();
			root.style.paddingBottom = 4;

			SerializedProperty widthProp = serializedObject.FindProperty("width");
			SerializedProperty heightProp = serializedObject.FindProperty("height");
			SerializedProperty percentBasisProp = serializedObject.FindProperty("percentBasis");
			SerializedProperty roundToPixelProp = serializedObject.FindProperty("roundToPixel");
			SerializedProperty priorityProp = serializedObject.FindProperty("priority");
			SerializedProperty alignSelfProp = serializedObject.FindProperty("alignSelf");
			SerializedProperty orderProp = serializedObject.FindProperty("order");

			var headerRow = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					alignItems = Align.Center,
					backgroundColor = SperlichEditorTheme.BgDark,
					paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8,
					marginBottom = 6
				}
			};
			headerRow.Add(new Label("Flex element") { style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary } });
			var stateBadge = SperlichEditorWidgets.CreateBadge("", SperlichEditorTheme.BadgeNeutralBg);
			headerRow.Add(stateBadge);
			root.Add(headerRow);

			void RefreshStateBadge() {
				bool anyStandalone = false, anyContained = false;
				foreach (var obj in targets) {
					if (obj is FlexElement fe) {
						if (fe.StandaloneModeActive) anyStandalone = true; else anyContained = true;
					}
				}
				if (anyStandalone && anyContained) {
					stateBadge.text = "MIXED";
					stateBadge.style.backgroundColor = SperlichEditorTheme.BadgeNeutralBg;
				} else if (anyStandalone) {
					stateBadge.text = "STANDALONE";
					stateBadge.style.backgroundColor = SperlichEditorTheme.BadgeWarnBg;
				} else {
					stateBadge.text = "IN CONTAINER";
					stateBadge.style.backgroundColor = SperlichEditorTheme.ToggleOnBg;
				}
			}
			RefreshStateBadge();
			headerRow.schedule.Execute(RefreshStateBadge).Every(250);

			bool hasLegacy = false;
			foreach (var obj in targets) {
				if (obj is FlexElement fe && fe.TryGetComponent(out LayoutElement _)) {
					hasLegacy = true;
					break;
				}
			}
			if (hasLegacy) {
				root.Add(new HelpBox("Dieses GameObject hat zusätzlich ein Unity LayoutElement. FlexElement hat eine höhere Layout-Priorität und gewinnt — das LayoutElement kann entfernt werden.", HelpBoxMessageType.Warning));
			}

			var aspectBothWarning = new HelpBox("Breite und Höhe können nicht beide auf Ratio stehen — es gibt keine Basis-Achse mehr. Breite wird ignoriert.", HelpBoxMessageType.Error);
			root.Add(aspectBothWarning);

			root.Add(BuildAxisCard(widthProp, 0, "Width", LayoutEditorStyle.WidthAccent));
			root.Add(SperlichEditorWidgets.Spacer(6));
			root.Add(BuildAxisCard(heightProp, 1, "Height", LayoutEditorStyle.HeightAccent));
			root.Add(SperlichEditorWidgets.Spacer(6));

			var (settingsHeader, settingsBody, _) = SperlichEditorWidgets.CreateChevronSection("Settings", true, SperlichEditorTheme.BgDark);

			var percentBasisField = SperlichEditorWidgets.CreateEnumDropdown(percentBasisProp);
			var priorityField = new PropertyField(priorityProp, "");
			var alignSelfField = SperlichEditorWidgets.CreateEnumDropdown(alignSelfProp);
			var orderField = new PropertyField(orderProp, "");

			var roundToggle = new PillToggle(roundToPixelProp.boolValue);
			roundToggle.Clicked += () => {
				bool newValue = !roundToPixelProp.boolValue;
				roundToPixelProp.boolValue = newValue;
				roundToPixelProp.serializedObject.ApplyModifiedProperties();
				roundToggle.SetValue(newValue);
			};

			settingsBody.style.paddingLeft = 6;
			settingsBody.style.paddingRight = 6;
			settingsBody.style.paddingTop = 4;
			settingsBody.style.paddingBottom = 4;
			settingsBody.Add(SperlichEditorWidgets.CreateAlignedRow("Round to pixel", roundToggle));
			settingsBody.Add(SperlichEditorWidgets.CreateAlignedRow("Percent basis", percentBasisField));
			settingsBody.Add(SperlichEditorWidgets.CreateAlignedRow("Priority", priorityField));
			settingsBody.Add(SperlichEditorWidgets.CreateAlignedRow("Align self", alignSelfField));
			settingsBody.Add(SperlichEditorWidgets.CreateAlignedRow("Order", orderField));

			var settingsBox = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			settingsBox.Add(settingsHeader);
			settingsBox.Add(settingsBody);
			root.Add(settingsBox);

			void RefreshModeWarnings() {
				var widthMode = (FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex;
				var heightMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
				aspectBothWarning.style.display = (widthMode == FlexMode.Aspect && heightMode == FlexMode.Aspect) ? DisplayStyle.Flex : DisplayStyle.None;
			}

			RefreshModeWarnings();
			root.TrackPropertyValue(widthProp, _ => RefreshModeWarnings());
			root.TrackPropertyValue(heightProp, _ => RefreshModeWarnings());

			return root;
		}

		private VisualElement BuildAxisCard(SerializedProperty sizeProp, int axis, string title, Color accent) {
			SerializedProperty modeProp = sizeProp.FindPropertyRelative("mode");
			SerializedProperty valueProp = sizeProp.FindPropertyRelative("value");
			SerializedProperty minProp = sizeProp.FindPropertyRelative("min");
			SerializedProperty minUnitProp = sizeProp.FindPropertyRelative("minUnit");
			SerializedProperty maxProp = sizeProp.FindPropertyRelative("max");
			SerializedProperty maxUnitProp = sizeProp.FindPropertyRelative("maxUnit");
			SerializedProperty growProp = sizeProp.FindPropertyRelative("grow");
			SerializedProperty shrinkProp = sizeProp.FindPropertyRelative("shrink");

			var card = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);

			var header = new VisualElement {
				style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center, backgroundColor = SperlichEditorTheme.BgDark, paddingTop = 5, paddingBottom = 5, paddingLeft = 0 }
			};
			var colorBar = new VisualElement { style = { width = 5, alignSelf = Align.Stretch, backgroundColor = accent, marginRight = 8 } };
			header.Add(colorBar);
			header.Add(new Label(title) { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary, flexGrow = 1 } });
			var summaryLabel = new Label { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginRight = 8 } };
			header.Add(summaryLabel);

			var body = new VisualElement { style = { backgroundColor = SperlichEditorTheme.BgStepBody, paddingLeft = 8, paddingRight = 8, paddingTop = 8, paddingBottom = 8 } };

			var pixelsRow = LayoutEditorStyle.CreateValueRow(new PropertyField(valueProp, ""), "px");
			var weightRow = LayoutEditorStyle.CreateValueRow(new PropertyField(valueProp, ""), "weight");
			var ratioRow = LayoutEditorStyle.CreateValueRow(new PropertyField(valueProp, ""), axis == 0 ? "× height" : "× width");

			var (percentBar, refreshPercentBar) = SperlichEditorWidgets.CreateDraggableBar(valueProp, 0f, 100f, accent);
			var percentValueLabel = new Label { style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary, minWidth = 32, unityTextAlign = TextAnchor.MiddleRight, marginLeft = 8 } };
			var percentRow = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };
			percentRow.Add(percentBar);
			percentRow.Add(percentValueLabel);
			void RefreshPercentLabel() {
				percentValueLabel.text = Mathf.RoundToInt(valueProp.floatValue) + "%";
				refreshPercentBar();
			}
			RefreshPercentLabel();
			percentRow.TrackPropertyValue(valueProp, _ => RefreshPercentLabel());

			var autoRow = LayoutEditorStyle.CreateInfoRow("Sized from content");
			var ignoreRow = LayoutEditorStyle.CreateInfoRow("Not constrained");

			var (advancedHeader, advancedBody, _) = SperlichEditorWidgets.CreateChevronSection("Advanced", false, SperlichEditorTheme.BgStep);
			advancedBody.style.paddingTop = 6;
			advancedBody.style.paddingLeft = 2;
			var grid = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, flexWrap = Wrap.Wrap, justifyContent = Justify.SpaceBetween } };
			grid.Add(LayoutEditorStyle.CreateCompactField("Min", minProp, minUnitProp));
			grid.Add(LayoutEditorStyle.CreateCompactField("Max", maxProp, maxUnitProp));
			grid.Add(LayoutEditorStyle.CreateCompactField("Grow", growProp, null));
			grid.Add(LayoutEditorStyle.CreateCompactField("Shrink", shrinkProp, null));
			advancedBody.Add(grid);
			var advancedSection = new VisualElement();
			advancedSection.Add(advancedHeader);
			advancedSection.Add(advancedBody);

			void RefreshAxis() {
				var mode = (FlexMode)modeProp.enumValueIndex;
				pixelsRow.style.display = mode == FlexMode.Pixels ? DisplayStyle.Flex : DisplayStyle.None;
				percentRow.style.display = mode == FlexMode.Percent ? DisplayStyle.Flex : DisplayStyle.None;
				weightRow.style.display = mode == FlexMode.Flexible ? DisplayStyle.Flex : DisplayStyle.None;
				ratioRow.style.display = mode == FlexMode.Aspect ? DisplayStyle.Flex : DisplayStyle.None;
				autoRow.style.display = mode == FlexMode.Auto ? DisplayStyle.Flex : DisplayStyle.None;
				ignoreRow.style.display = mode == FlexMode.Ignore ? DisplayStyle.Flex : DisplayStyle.None;
				advancedSection.style.display = mode == FlexMode.Ignore ? DisplayStyle.None : DisplayStyle.Flex;

				var summaryParts = new List<string>();
				if (minProp.floatValue != 0f) summaryParts.Add($"min {minProp.floatValue:0.#}");
				if (maxProp.floatValue != 0f) summaryParts.Add($"max {maxProp.floatValue:0.#}");
				float grow = mode == FlexMode.Flexible ? valueProp.floatValue : growProp.floatValue;
				if (grow > 0f) summaryParts.Add($"grow {grow:0.#}");
				summaryLabel.text = summaryParts.Count > 0 ? string.Join(" · ", summaryParts) : string.Empty;
			}

			card.Add(header);
			card.Add(body);
			body.Add(SperlichEditorWidgets.CreateSegmentedControl(modeProp, ModeLabels, accent, () => RefreshAxis()));
			body.Add(pixelsRow);
			body.Add(percentRow);
			body.Add(weightRow);
			body.Add(ratioRow);
			body.Add(autoRow);
			body.Add(ignoreRow);
			card.Add(advancedSection);

			RefreshAxis();
			card.TrackPropertyValue(modeProp, _ => RefreshAxis());
			card.TrackPropertyValue(minProp, _ => RefreshAxis());
			card.TrackPropertyValue(maxProp, _ => RefreshAxis());
			card.TrackPropertyValue(growProp, _ => RefreshAxis());
			card.TrackPropertyValue(valueProp, _ => RefreshAxis());

			return card;
		}
	}
}
