using Sperlich.EditorKit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	[CustomEditor(typeof(GridContainer))]
	[CanEditMultipleObjects]
	public class GridContainerEditor : UnityEditor.Editor {

		private static readonly Color[] TrackColors = {
			new Color(0.35f, 0.70f, 0.95f),
			new Color(0.95f, 0.55f, 0.30f),
			new Color(0.55f, 0.85f, 0.45f),
			new Color(0.80f, 0.50f, 0.90f),
		};

		public override VisualElement CreateInspectorGUI() {
			var root = new VisualElement { style = { paddingBottom = 4 } };

			SerializedProperty columns = serializedObject.FindProperty("columns");
			SerializedProperty rows = serializedObject.FindProperty("rows");
			SerializedProperty autoFlow = serializedObject.FindProperty("autoFlow");
			SerializedProperty startCorner = serializedObject.FindProperty("startCorner");
			SerializedProperty columnRepeat = serializedObject.FindProperty("columnRepeat");
			SerializedProperty columnRepeatTemplate = serializedObject.FindProperty("columnRepeatTemplate");
			SerializedProperty columnRepeatMinSize = serializedObject.FindProperty("columnRepeatMinSize");
			SerializedProperty implicitRowTemplate = serializedObject.FindProperty("implicitRowTemplate");
			SerializedProperty implicitColumnTemplate = serializedObject.FindProperty("implicitColumnTemplate");
			SerializedProperty justifyItems = serializedObject.FindProperty("justifyItems");
			SerializedProperty alignItems = serializedObject.FindProperty("alignItems");
			SerializedProperty gap = serializedObject.FindProperty("gap");
			SerializedProperty padding = serializedObject.FindProperty("padding");

			var header = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row, alignItems = Align.Center,
					backgroundColor = SperlichEditorTheme.BgDark,
					paddingTop = 6, paddingBottom = 6, paddingLeft = 8, paddingRight = 8, marginBottom = 6
				}
			};
			header.Add(new Label("Grid container") { style = { fontSize = 13, unityFontStyleAndWeight = FontStyle.Bold, color = SperlichEditorTheme.TextPrimary, flexGrow = 1 } });
			root.Add(header);

			var preview = BuildTrackPreview(columns, columnRepeat);
			root.Add(preview.element);
			root.Add(SperlichEditorWidgets.Spacer(6));

			var tracksBox = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			var (tracksHeader, tracksBody, _) = SperlichEditorWidgets.CreateChevronSection("Tracks", true, SperlichEditorTheme.BgDark);
			tracksBody.style.paddingLeft = 6;
			tracksBody.style.paddingRight = 6;

			tracksBody.Add(SperlichEditorWidgets.CreateAlignedRow("Auto flow", SperlichEditorWidgets.CreateEnumDropdown(autoFlow)));
			tracksBody.Add(SperlichEditorWidgets.CreateAlignedRow("Start corner", SperlichEditorWidgets.CreateEnumDropdown(startCorner)));

			var repeatRow = SperlichEditorWidgets.CreateAlignedRow("Column repeat", SperlichEditorWidgets.CreateEnumDropdown(columnRepeat));
			tracksBody.Add(repeatRow);
			var repeatFields = new VisualElement();
			repeatFields.Add(new PropertyField(columnRepeatTemplate, "Repeat template"));
			repeatFields.Add(new PropertyField(columnRepeatMinSize, "Repeat min size"));
			tracksBody.Add(repeatFields);

			var columnsField = new PropertyField(columns, "Columns");
			var rowsField = new PropertyField(rows, "Rows");
			var implicitRowField = new PropertyField(implicitRowTemplate, "Implicit row");
			var implicitColumnField = new PropertyField(implicitColumnTemplate, "Implicit column");
			tracksBody.Add(columnsField);
			tracksBody.Add(rowsField);
			tracksBody.Add(implicitRowField);
			tracksBody.Add(implicitColumnField);

			var columnFlowHint = new HelpBox("Column-Flow: die Anzahl der Rows bestimmt, wie viele Items pro Spalte kommen — mindestens eine Row definieren. 'Column repeat' wird dabei ignoriert.", HelpBoxMessageType.Info);
			tracksBody.Add(columnFlowHint);

			void RefreshTracks() {
				bool columnFlow = autoFlow.enumValueIndex == (int)GridAutoFlow.Column;
				bool repeatOn = columnRepeat.enumValueIndex != (int)GridRepeatMode.None && columnFlow == false;

				repeatRow.style.display = columnFlow ? DisplayStyle.None : DisplayStyle.Flex;
				repeatFields.style.display = repeatOn ? DisplayStyle.Flex : DisplayStyle.None;
				columnsField.style.display = repeatOn ? DisplayStyle.None : DisplayStyle.Flex;

				implicitRowField.style.display = columnFlow ? DisplayStyle.None : DisplayStyle.Flex;
				implicitColumnField.style.display = columnFlow ? DisplayStyle.Flex : DisplayStyle.None;

				columnFlowHint.style.display = (columnFlow && rows.arraySize == 0) ? DisplayStyle.Flex : DisplayStyle.None;
			}
			RefreshTracks();
			root.TrackPropertyValue(autoFlow, _ => RefreshTracks());
			root.TrackPropertyValue(rows, _ => RefreshTracks());
			root.TrackPropertyValue(columnRepeat, _ => { RefreshTracks(); preview.refresh(); });
			root.TrackPropertyValue(columns, _ => preview.refresh());

			tracksBox.Add(tracksHeader);
			tracksBox.Add(tracksBody);
			root.Add(tracksBox);
			root.Add(SperlichEditorWidgets.Spacer(6));

			var alignBox = SperlichEditorWidgets.CreateBox(4, SperlichEditorTheme.BorderSubtle);
			var (alignHeader, alignBody, _) = SperlichEditorWidgets.CreateChevronSection("Alignment & spacing", true, SperlichEditorTheme.BgDark);
			alignBody.style.paddingLeft = 6;
			alignBody.style.paddingRight = 6;
			alignBody.Add(SperlichEditorWidgets.CreateAlignedRow("Justify items", SperlichEditorWidgets.CreateEnumDropdown(justifyItems)));
			alignBody.Add(SperlichEditorWidgets.CreateAlignedRow("Align items", SperlichEditorWidgets.CreateEnumDropdown(alignItems)));
			alignBody.Add(new PropertyField(gap));
			alignBody.Add(new PropertyField(padding));
			alignBox.Add(alignHeader);
			alignBox.Add(alignBody);
			root.Add(alignBox);

			return root;
		}

		private (VisualElement element, System.Action refresh) BuildTrackPreview(SerializedProperty columns, SerializedProperty columnRepeat) {
			var bar = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row, height = 22,
					borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
					overflow = Overflow.Hidden, marginLeft = 2, marginRight = 2
				}
			};

			void Refresh() {
				bar.Clear();
				if (columnRepeat.enumValueIndex != (int)GridRepeatMode.None) {
					var label = new Label("auto columns (repeat)") {
						style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter, fontSize = 10, color = SperlichEditorTheme.TextMuted, backgroundColor = SperlichEditorTheme.BgStep }
					};
					bar.Add(label);
					return;
				}

				int count = columns.arraySize;
				if (count == 0) {
					bar.Add(new Label("no columns") { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleCenter, fontSize = 10, color = SperlichEditorTheme.TextMuted, backgroundColor = SperlichEditorTheme.BgStep } });
					return;
				}

				var weights = new float[count];
				float total = 0f;
				for (int i = 0; i < count; i++) {
					SerializedProperty track = columns.GetArrayElementAtIndex(i);
					int mode = track.FindPropertyRelative("mode").enumValueIndex;
					float value = track.FindPropertyRelative("value").floatValue;
					float minPx = track.FindPropertyRelative("minPx").floatValue;
					weights[i] = mode switch {
						(int)GridTrackMode.Pixels => Mathf.Max(8f, value),
						(int)GridTrackMode.Fraction => Mathf.Max(0.25f, value) * 80f,
						(int)GridTrackMode.MinMax => Mathf.Max(24f, minPx),
						_ => 48f,
					};
					total += weights[i];
				}

				for (int i = 0; i < count; i++) {
					SerializedProperty track = columns.GetArrayElementAtIndex(i);
					int mode = track.FindPropertyRelative("mode").enumValueIndex;
					string tag = mode switch {
						(int)GridTrackMode.Pixels => track.FindPropertyRelative("value").floatValue + "px",
						(int)GridTrackMode.Fraction => track.FindPropertyRelative("value").floatValue + "fr",
						(int)GridTrackMode.Auto => "auto",
						_ => "minmax",
					};
					var seg = new Label(tag) {
						style = {
							width = new Length(100f * weights[i] / total, LengthUnit.Percent),
							unityTextAlign = TextAnchor.MiddleCenter, fontSize = 9,
							color = Color.white,
							backgroundColor = TrackColors[i % TrackColors.Length],
							borderRightWidth = i < count - 1 ? 1 : 0, borderRightColor = SperlichEditorTheme.BgDark
						}
					};
					bar.Add(seg);
				}
			}

			Refresh();
			return (bar, Refresh);
		}
	}
}
