using Sperlich.EditorKit;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	/// <summary>
	/// Inspector für <see cref="FlexElement"/> mit Quick-Presets und kompakter 2-Spalten-Matrix.
	/// </summary>
	[CustomEditor(typeof(FlexElement))]
	[CanEditMultipleObjects]
	public sealed class FlexElementEditor : UnityEditor.Editor {

		private static readonly Color Accent = SperlichEditorTheme.ButtonAccent;
		private readonly SperlichFieldColumn col = new(130f);

		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement {
				style = {
					paddingTop = 2,
					paddingBottom = 4,
					marginLeft = -15,
					marginRight = -4
				}
			};

			SerializedProperty widthProp = serializedObject.FindProperty("width");
			SerializedProperty heightProp = serializedObject.FindProperty("height");
			SerializedProperty percentBasisProp = serializedObject.FindProperty("percentBasis");
			SerializedProperty roundToPixelProp = serializedObject.FindProperty("roundToPixel");
			SerializedProperty priorityProp = serializedObject.FindProperty("priority");
			SerializedProperty alignSelfProp = serializedObject.FindProperty("alignSelf");
			SerializedProperty orderProp = serializedObject.FindProperty("order");

			// ---- Header & State -------------------------------------------------------------------
			var headerRow = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					alignItems = Align.Center,
					backgroundColor = SperlichEditorTheme.BgDark,
					paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8,
					marginBottom = 6, marginLeft = 4, marginRight = 4
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

			// ---- Warnings -------------------------------------------------------------------------
			bool hasLegacy = false;
			foreach (var obj in targets) {
				if (obj is FlexElement fe && fe.TryGetComponent(out UnityEngine.UI.LayoutElement _)) {
					hasLegacy = true;
					break;
				}
			}
			if (hasLegacy) {
				root.Add(new HelpBox("Dieses GameObject hat zusätzlich ein Unity LayoutElement. FlexElement hat eine höhere Layout-Priorität und gewinnt — das LayoutElement kann entfernt werden.", HelpBoxMessageType.Warning));
			}

			var aspectBothWarning = new HelpBox("Breite und Höhe können nicht beide auf Ratio stehen — es gibt keine Basis-Achse mehr. Breite wird ignoriert.", HelpBoxMessageType.Error);
			root.Add(aspectBothWarning);

			// ---- Quick Presets Bar ----------------------------------------------------------------
			root.Add(BuildPresetsCard(widthProp, heightProp));
			root.Add(SperlichEditorWidgets.Spacer(4));

			// ---- 2-Column Dimension Matrix (Width & Height) ---------------------------------------
			var matrixRow = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					marginLeft = 4,
					marginRight = 4
				}
			};
			var widthCol = BuildAxisColumn(widthProp, 0, "Width", LayoutEditorStyle.WidthAccent);
			widthCol.style.flexGrow = 1;
			widthCol.style.flexBasis = 0;
			widthCol.style.marginRight = 3;

			var heightCol = BuildAxisColumn(heightProp, 1, "Height", LayoutEditorStyle.HeightAccent);
			heightCol.style.flexGrow = 1;
			heightCol.style.flexBasis = 0;
			heightCol.style.marginLeft = 3;

			matrixRow.Add(widthCol);
			matrixRow.Add(heightCol);
			root.Add(matrixRow);
			root.Add(SperlichEditorWidgets.Spacer(4));

			// ---- Settings Section -----------------------------------------------------------------
			var settings = Section(root, "SETTINGS", true);

			var percentBasisField = SperlichEditorWidgets.CreateEnumDropdown(percentBasisProp, Accent);
			var alignSelfField = SperlichEditorWidgets.CreateEnumDropdown(alignSelfProp, Accent);

			var roundToggle = new PillToggle(roundToPixelProp.boolValue);
			roundToggle.Clicked += () => {
				bool newValue = !roundToPixelProp.boolValue;
				roundToPixelProp.boolValue = newValue;
				roundToPixelProp.serializedObject.ApplyModifiedProperties();
				roundToggle.SetValue(newValue);
			};

			var priorityOrderCluster = SperlichEditorWidgets.CreateFieldCluster(60,
				SperlichEditorWidgets.CreateCompactField("Priority", SperlichEditorWidgets.CreateSteppedNumberField(priorityProp), captionAbove: false),
				SperlichEditorWidgets.CreateCompactField("Order", SperlichEditorWidgets.CreateSteppedNumberField(orderProp), captionAbove: false)
			);

			settings.Add(col.Row("Round to Pixel", roundToggle));
			settings.Add(col.Row("Percent Basis", percentBasisField));
			settings.Add(col.Row("Align Self", alignSelfField));
			settings.Add(col.Row("Priority / Order", priorityOrderCluster));

			void RefreshModeWarnings() {
				var widthMode = (FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex;
				var heightMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
				aspectBothWarning.style.display = (widthMode == FlexMode.Aspect && heightMode == FlexMode.Aspect) ? DisplayStyle.Flex : DisplayStyle.None;
			}

			RefreshModeWarnings();
			root.TrackPropertyValue(widthProp, _ => RefreshModeWarnings());
			root.TrackPropertyValue(heightProp, _ => RefreshModeWarnings());

			root.TrackSerializedObjectValue(serializedObject, _ => {
				foreach (UnityEngine.Object t in targets) {
					if (t is FlexElement fe) fe.RequestRebuild();
				}
			});

			SperlichInspectorScroll.Preserve(root, target);
			return root;
		}

		private static VisualElement Section(VisualElement parent, string title, bool expanded) {
			var (header, body, _) = SperlichEditorWidgets.CreateChevronSection(title, expanded, SperlichEditorTheme.BgStep, null, nameof(FlexElementEditor));
			body.style.paddingLeft = 6;
			body.style.paddingRight = 6;
			body.style.paddingTop = 4;
			body.style.paddingBottom = 6;
			var wrap = new VisualElement { style = { marginBottom = 4 } };
			wrap.Add(header);
			wrap.Add(body);
			parent.Add(wrap);
			return body;
		}

		/// <summary>Erstellt die Quick-Presets Leiste mit 5 Direkt-Aktionen.</summary>
		private VisualElement BuildPresetsCard(SerializedProperty widthProp, SerializedProperty heightProp) {
			var card = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			card.style.marginLeft = 4;
			card.style.marginRight = 4;
			card.style.backgroundColor = SperlichEditorTheme.BgPanel;
			card.style.paddingLeft = 6;
			card.style.paddingRight = 6;
			card.style.paddingTop = 5;
			card.style.paddingBottom = 6;

			var header = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					justifyContent = Justify.SpaceBetween,
					alignItems = Align.Center,
					marginBottom = 4
				}
			};
			var title = new Label("QUICK PRESETS") {
				style = { fontSize = 10, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextMuted }
			};
			var statusLabel = new Label("custom") {
				style = { fontSize = 10, color = SperlichEditorTheme.TextSecondary }
			};
			header.Add(title);
			header.Add(statusLabel);
			card.Add(header);

			var buttonsRow = new VisualElement {
				style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row }
			};

			var presetButtons = new List<(VisualElement btn, Func<bool> isActive, Action apply, string name)>();

			void AddPreset(string label, string subText, Func<bool> isActive, Action apply, string name) {
				var btn = new VisualElement { pickingMode = PickingMode.Position };
				btn.style.flexGrow = 1;
				btn.style.flexBasis = 0;
				btn.style.alignItems = Align.Center;
				btn.style.justifyContent = Justify.Center;
				btn.style.paddingTop = 4;
				btn.style.paddingBottom = 4;
				btn.style.marginRight = 2;
				btn.style.borderTopWidth = 1;
				btn.style.borderBottomWidth = 1;
				btn.style.borderLeftWidth = 1;
				btn.style.borderRightWidth = 1;
				SperlichEditorWidgets.SetRadius(btn, 3);
				SperlichEditorWidgets.SetBorderColor(btn, SperlichEditorTheme.BorderSubtle);
				btn.style.backgroundColor = SperlichEditorTheme.ButtonBg;
				SperlichEditorWidgets.ApplyColorTransition(btn, 100, "background-color", "border-color");
				SperlichEditorWidgets.SetHoverCursor(btn, MouseCursor.Link);

				var mainLbl = new Label(label) { pickingMode = PickingMode.Ignore, style = { fontSize = 11, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary } };
				var subLbl = new Label(subText) { pickingMode = PickingMode.Ignore, style = { fontSize = 9, color = SperlichEditorTheme.TextMuted } };
				btn.Add(mainLbl);
				btn.Add(subLbl);

				btn.RegisterCallback<ClickEvent>(_ => {
					if (isActive()) {
						widthProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Ignore;
						heightProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Ignore;
					} else {
						apply();
					}
					widthProp.serializedObject.ApplyModifiedProperties();
				});

				presetButtons.Add((btn, isActive, apply, name));
				buttonsRow.Add(btn);
			}

			// Preset 1: Full Width (W=100%, H=Grow 1)
			AddPreset("↔ 100%", "Full Width",
				() => {
					var wMode = (FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex;
					var hMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
					float wVal = widthProp.FindPropertyRelative("value").floatValue;
					return wMode == FlexMode.Percent && Mathf.Approximately(wVal, 100f) && hMode != FlexMode.Aspect;
				},
				() => {
					widthProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Percent;
					widthProp.FindPropertyRelative("value").floatValue = 100f;
					widthProp.FindPropertyRelative("grow").floatValue = 0f;
					widthProp.FindPropertyRelative("shrink").floatValue = 1f;
					if ((FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex == FlexMode.Aspect) {
						heightProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Flexible;
						heightProp.FindPropertyRelative("value").floatValue = 1f;
					}
				},
				"100% full width"
			);

			// Preset 2: Fill Both (W=Grow 1, H=Grow 1)
			AddPreset("⇥⇤ Fill", "Grow Both",
				() => {
					var wMode = (FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex;
					var hMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
					return wMode == FlexMode.Flexible && hMode == FlexMode.Flexible;
				},
				() => {
					widthProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Flexible;
					widthProp.FindPropertyRelative("value").floatValue = 1f;
					widthProp.FindPropertyRelative("grow").floatValue = 1f;
					widthProp.FindPropertyRelative("shrink").floatValue = 1f;
					heightProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Flexible;
					heightProp.FindPropertyRelative("value").floatValue = 1f;
					heightProp.FindPropertyRelative("grow").floatValue = 1f;
					heightProp.FindPropertyRelative("shrink").floatValue = 1f;
				},
				"grow both (fill)"
			);

			// Preset 3: Auto (Content)
			AddPreset("⤢ Auto", "Content",
				() => {
					var wMode = (FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex;
					var hMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
					return wMode == FlexMode.Auto && hMode == FlexMode.Auto;
				},
				() => {
					widthProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Auto;
					widthProp.FindPropertyRelative("grow").floatValue = 0f;
					widthProp.FindPropertyRelative("shrink").floatValue = 1f;
					heightProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Auto;
					heightProp.FindPropertyRelative("grow").floatValue = 0f;
					heightProp.FindPropertyRelative("shrink").floatValue = 1f;
				},
				"fit content (auto)"
			);

			// Preset 4: Fixed Pixels (W=100px, H=100px)
			AddPreset("⊞ Fixed", "Pixels",
				() => {
					var wMode = (FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex;
					var hMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
					return wMode == FlexMode.Pixels && hMode == FlexMode.Pixels;
				},
				() => {
					widthProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Pixels;
					if (widthProp.FindPropertyRelative("value").floatValue <= 0f) widthProp.FindPropertyRelative("value").floatValue = 100f;
					heightProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Pixels;
					if (heightProp.FindPropertyRelative("value").floatValue <= 0f) heightProp.FindPropertyRelative("value").floatValue = 100f;
				},
				"fixed pixel size"
			);

			// Preset 5: 1:1 Aspect Ratio
			AddPreset("⊡ 1 : 1", "Square",
				() => {
					var hMode = (FlexMode)heightProp.FindPropertyRelative("mode").enumValueIndex;
					float hVal = heightProp.FindPropertyRelative("value").floatValue;
					return hMode == FlexMode.Aspect && Mathf.Approximately(hVal, 1f);
				},
				() => {
					if ((FlexMode)widthProp.FindPropertyRelative("mode").enumValueIndex == FlexMode.Aspect) {
						widthProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Percent;
						widthProp.FindPropertyRelative("value").floatValue = 100f;
					}
					heightProp.FindPropertyRelative("mode").enumValueIndex = (int)FlexMode.Aspect;
					heightProp.FindPropertyRelative("value").floatValue = 1f;
				},
				"square 1:1 aspect"
			);

			if (buttonsRow.childCount > 0) {
				buttonsRow[buttonsRow.childCount - 1].style.marginRight = 0;
			}
			card.Add(buttonsRow);

			void RefreshPresets() {
				int activeIndex = -1;
				for (int i = 0; i < presetButtons.Count; i++) {
					if (presetButtons[i].isActive()) {
						activeIndex = i;
						break;
					}
				}

				for (int i = 0; i < presetButtons.Count; i++) {
					bool active = i == activeIndex;
					var btn = presetButtons[i].btn;
					SperlichEditorWidgets.SetBorderColor(btn, active ? Accent : SperlichEditorTheme.BorderSubtle);
					btn.style.backgroundColor = active ? new Color(Accent.r, Accent.g, Accent.b, 0.16f) : SperlichEditorTheme.ButtonBg;
					var lbl = btn.Q<Label>();
					if (lbl != null) lbl.style.color = active ? Accent : SperlichEditorTheme.TextPrimary;
				}
				statusLabel.text = activeIndex >= 0 ? presetButtons[activeIndex].name : "custom";
				statusLabel.style.color = activeIndex >= 0 ? Accent : SperlichEditorTheme.TextMuted;
			}

			RefreshPresets();
			card.TrackPropertyValue(widthProp, _ => RefreshPresets());
			card.TrackPropertyValue(heightProp, _ => RefreshPresets());

			return card;
		}

		/// <summary>Erstellt eine einzelne Spalte (Width oder Height) in der 2-Spalten-Matrix.</summary>
		private VisualElement BuildAxisColumn(SerializedProperty sizeProp, int axis, string title, Color accent) {
			SerializedProperty modeProp = sizeProp.FindPropertyRelative("mode");
			SerializedProperty valueProp = sizeProp.FindPropertyRelative("value");
			SerializedProperty minProp = sizeProp.FindPropertyRelative("min");
			SerializedProperty minUnitProp = sizeProp.FindPropertyRelative("minUnit");
			SerializedProperty maxProp = sizeProp.FindPropertyRelative("max");
			SerializedProperty maxUnitProp = sizeProp.FindPropertyRelative("maxUnit");
			SerializedProperty growProp = sizeProp.FindPropertyRelative("grow");
			SerializedProperty shrinkProp = sizeProp.FindPropertyRelative("shrink");

			var colBox = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			colBox.style.backgroundColor = SperlichEditorTheme.BgPanel;

			// Header Bar
			var header = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					alignItems = Align.Center,
					backgroundColor = SperlichEditorTheme.BgStep,
					paddingTop = 4, paddingBottom = 4, paddingLeft = 0, paddingRight = 6,
					borderBottomWidth = 1,
					borderBottomColor = SperlichEditorTheme.BorderSubtle
				}
			};
			var colorBar = new VisualElement { style = { width = 4, alignSelf = Align.Stretch, backgroundColor = accent, marginRight = 6 } };
			header.Add(colorBar);
			header.Add(new Label(title) { style = { fontSize = 12, unityFontStyleAndWeight = FontStyle.Bold, color = accent, flexGrow = 1 } });
			var badge = SperlichEditorWidgets.CreateBadge("", new Color(accent.r, accent.g, accent.b, 0.15f), accent);
			header.Add(badge);
			colBox.Add(header);

			// Body Container
			var body = new VisualElement {
				style = { paddingLeft = 6, paddingRight = 6, paddingTop = 6, paddingBottom = 6 }
			};

			// 1. Value & Mode in ONE line
			var valModeWrap = new VisualElement { style = { marginBottom = 5 } };
			valModeWrap.Add(new Label("Value & Mode") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginBottom = 2 } });
			var valModeRow = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };

			VisualElement valueField = SperlichEditorWidgets.CreateDragNumberField(valueProp);
			valueField.style.flexGrow = 1;
			valueField.style.marginRight = 3;

			VisualElement modeDropdown = SperlichEditorWidgets.CreateEnumDropdown(modeProp, accent);
			modeDropdown.style.width = 64;
			modeDropdown.style.flexGrow = 0;

			valModeRow.Add(valueField);
			valModeRow.Add(modeDropdown);
			valModeWrap.Add(valModeRow);
			body.Add(valModeWrap);

			// 2. Min / Max Row
			var minMaxWrap = new VisualElement { style = { marginBottom = 5 } };
			minMaxWrap.Add(new Label("Min / Max") { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginBottom = 2 } });
			var minMaxRow = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };

			VisualElement minField = LayoutEditorStyle.CreateCompactField("Min", minProp, minUnitProp);
			minField.style.width = new Length(49, LengthUnit.Percent);
			minField.style.marginRight = 4;
			minField.style.marginBottom = 0;
			var minLbl = minField.Q<Label>();
			if (minLbl != null) minLbl.style.display = DisplayStyle.None; // hide redundant sublabel

			VisualElement maxField = LayoutEditorStyle.CreateCompactField("Max", maxProp, maxUnitProp);
			maxField.style.width = new Length(49, LengthUnit.Percent);
			maxField.style.marginBottom = 0;
			var maxLbl = maxField.Q<Label>();
			if (maxLbl != null) maxLbl.style.display = DisplayStyle.None;

			minMaxRow.Add(minField);
			minMaxRow.Add(maxField);
			minMaxWrap.Add(minMaxRow);
			body.Add(minMaxWrap);

			// 3. Grow & Shrink Row
			var growShrinkWrap = new VisualElement {
				style = {
					borderTopWidth = 1,
					borderTopColor = SperlichEditorTheme.BorderSubtle,
					paddingTop = 4
				}
			};
			var growShrinkRow = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };

			VisualElement growField = LayoutEditorStyle.CreateCompactField("Grow", growProp, null);
			growField.style.width = new Length(49, LengthUnit.Percent);
			growField.style.marginRight = 4;
			growField.style.marginBottom = 0;

			VisualElement shrinkField = LayoutEditorStyle.CreateCompactField("Shrink", shrinkProp, null);
			shrinkField.style.width = new Length(49, LengthUnit.Percent);
			shrinkField.style.marginBottom = 0;

			growShrinkRow.Add(growField);
			growShrinkRow.Add(shrinkField);
			growShrinkWrap.Add(growShrinkRow);
			body.Add(growShrinkWrap);

			colBox.Add(body);

			// Dynamic badge & visibility update
			void RefreshColumn() {
				var mode = (FlexMode)modeProp.enumValueIndex;
				float val = valueProp.floatValue;
				string badgeText = mode switch {
					FlexMode.Pixels => $"{val:0.#}px",
					FlexMode.Percent => $"{val:0.#}%",
					FlexMode.Flexible => $"grow {val:0.#}",
					FlexMode.Auto => "auto",
					FlexMode.Aspect => axis == 0 ? $"{val:0.##}×H" : $"{val:0.##}×W",
					FlexMode.Ignore => "ignore",
					_ => mode.ToString()
				};
				badge.text = badgeText;
				valueField.SetEnabled(mode != FlexMode.Auto && mode != FlexMode.Ignore);
			}

			RefreshColumn();
			colBox.TrackPropertyValue(modeProp, _ => RefreshColumn());
			colBox.TrackPropertyValue(valueProp, _ => RefreshColumn());

			return colBox;
		}
	}
}

