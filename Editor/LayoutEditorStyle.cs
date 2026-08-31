using Sperlich.EditorKit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	/// <summary>Layout-package-spezifische Ergänzungen zum gemeinsamen Sperlich-Editor-Stil (siehe Sperlich.EditorKit für die generischen Bausteine).</summary>
	public static class LayoutEditorStyle {

		public static readonly Color WidthAccent = new Color(56f / 255f, 189f / 255f, 248f / 255f);
		public static readonly Color HeightAccent = new Color(251f / 255f, 146f / 255f, 60f / 255f);

		public static VisualElement CreateValueRow(VisualElement field, string suffix) {
			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };
			field.style.flexGrow = 1;
			row.Add(field);
			if (string.IsNullOrEmpty(suffix) == false) {
				var suffixLabel = new Label(suffix) { style = { fontSize = 11, color = SperlichEditorTheme.TextMuted, marginLeft = 6 } };
				row.Add(suffixLabel);
			}
			return row;
		}

		public static VisualElement CreateInfoRow(string text) {
			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };
			var label = new Label(text) { style = { fontSize = 11, color = SperlichEditorTheme.TextMuted, unityFontStyleAndWeight = FontStyle.Italic } };
			row.Add(label);
			return row;
		}

		/// <summary>Kompaktes Zahlenfeld mit kleinem Label darüber und optionaler Einheiten-Auswahl (Min/Max/Grow/Shrink im "Advanced"-Bereich einer Achsen-Karte).</summary>
		public static VisualElement CreateCompactField(string label, SerializedProperty valueProp, SerializedProperty unitProp) {
			var col = new VisualElement { style = { width = new Length(48, LengthUnit.Percent), marginBottom = 6 } };

			var lbl = new Label(label) { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginBottom = 2 } };
			col.Add(lbl);

			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };

			VisualElement field = SperlichEditorWidgets.CreateDragNumberField(valueProp);
			field.style.flexGrow = 1;
			row.Add(field);

			if (unitProp != null) {
				string[] unitLabels = { "px", "%" };
				string UnitLabelFor(int i) => (i >= 0 && i < unitLabels.Length) ? unitLabels[i] : "—";
				var unitField = SperlichEditorWidgets.BuildDropdown(
					() => unitLabels.Length,
					UnitLabelFor,
					() => unitProp.enumValueIndex,
					idx => {
						unitProp.enumValueIndex = idx;
						unitProp.serializedObject.ApplyModifiedProperties();
					}
				);
				unitField.TrackPropertyValue(unitProp, _ => {
					Label valLbl = unitField.Q<Label>();
					if (valLbl != null) valLbl.text = UnitLabelFor(unitProp.enumValueIndex);
				});
				unitField.style.flexGrow = 0;
				unitField.style.width = 36;
				unitField.style.paddingLeft = 4;
				unitField.style.paddingRight = 4;
				unitField.style.marginLeft = 2;
				row.Add(unitField);
			}

			col.Add(row);
			return col;
		}

		/// <summary>Erstellt ein 4-teiliges kompaktes Padding-Feld (Left, Top, Right, Bottom) analog zu den Margins im SText-Inspector.</summary>
		public static VisualElement CreatePaddingField(SerializedProperty paddingProp) {
			if (paddingProp == null) return new VisualElement();
			SerializedProperty left = paddingProp.FindPropertyRelative("m_Left");
			SerializedProperty top = paddingProp.FindPropertyRelative("m_Top");
			SerializedProperty right = paddingProp.FindPropertyRelative("m_Right");
			SerializedProperty bottom = paddingProp.FindPropertyRelative("m_Bottom");

			return SperlichEditorWidgets.CreateAlignedRow("Padding", SperlichEditorWidgets.CreateFieldCluster(44,
				SperlichEditorWidgets.CreateCompactField("Left", left, captionAbove: true),
				SperlichEditorWidgets.CreateCompactField("Top", top, captionAbove: true),
				SperlichEditorWidgets.CreateCompactField("Right", right, captionAbove: true),
				SperlichEditorWidgets.CreateCompactField("Bottom", bottom, captionAbove: true)));
		}

		/// <summary>Erstellt ein 2-teiliges kompaktes Gap-Feld (Column X, Row Y) analog zum SText-Layout-Stil.</summary>
		public static VisualElement CreateGapField(SerializedProperty gapProp) {
			if (gapProp == null) return new VisualElement();
			SerializedProperty gapX = gapProp.FindPropertyRelative("x");
			SerializedProperty gapY = gapProp.FindPropertyRelative("y");

			return SperlichEditorWidgets.CreateAlignedRow("Gap", SperlichEditorWidgets.CreateFieldCluster(52,
				SperlichEditorWidgets.CreateCompactField("Column (X)", gapX, captionAbove: true),
				SperlichEditorWidgets.CreateCompactField("Row (Y)", gapY, captionAbove: true)));
		}
	}
}
