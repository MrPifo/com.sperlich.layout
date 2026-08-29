using Sperlich.EditorKit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	/// <summary>Layout-package-spezifische Ergänzungen zum gemeinsamen Sperlich-Editor-Stil (siehe Sperlich.EditorKit für die generischen Bausteine).</summary>
	public static class LayoutEditorStyle {

		public static readonly Color WidthAccent = new Color(0.20f, 0.75f, 0.95f);
		public static readonly Color HeightAccent = new Color(0.95f, 0.35f, 0.20f);

		public static VisualElement CreateValueRow(VisualElement field, string suffix) {
			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };
			field.style.flexGrow = 1;
			row.Add(field);
			if (string.IsNullOrEmpty(suffix) == false) {
				var suffixLabel = new Label(suffix) { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, marginLeft = 6 } };
				row.Add(suffixLabel);
			}
			return row;
		}

		public static VisualElement CreateInfoRow(string text) {
			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center } };
			var label = new Label(text) { style = { fontSize = 10, color = SperlichEditorTheme.TextMuted, unityFontStyleAndWeight = FontStyle.Italic } };
			row.Add(label);
			return row;
		}

		/// <summary>Kompaktes Zahlenfeld mit kleinem Label darüber und optionaler Einheiten-Auswahl (Min/Max/Grow/Shrink im "Advanced"-Bereich einer Achsen-Karte).</summary>
		public static VisualElement CreateCompactField(string label, SerializedProperty valueProp, SerializedProperty unitProp) {
			var col = new VisualElement { style = { width = new Length(48, LengthUnit.Percent), marginBottom = 6 } };

			var lbl = new Label(label) { style = { fontSize = 9, color = SperlichEditorTheme.TextMuted, marginBottom = 2 } };
			col.Add(lbl);

			var row = new VisualElement { style = { flexDirection = UnityEngine.UIElements.FlexDirection.Row } };

			var field = new PropertyField(valueProp, "");
			field.style.flexGrow = 1;
			row.Add(field);

			if (unitProp != null) {
				var unitField = SperlichEditorWidgets.CreateEnumDropdown(unitProp);
				unitField.style.flexGrow = 0;
				unitField.style.width = 58;
				unitField.style.marginLeft = 2;
				row.Add(unitField);
			}

			col.Add(row);
			return col;
		}
	}
}
