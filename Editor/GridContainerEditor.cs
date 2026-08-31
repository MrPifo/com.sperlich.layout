using Sperlich.EditorKit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	/// <summary>
	/// Inspector für <see cref="GridContainer"/> nach den gemeinsamen SText/Sperlich-EditorKit-Standards.
	/// </summary>
	[CustomEditor(typeof(GridContainer))]
	[CanEditMultipleObjects]
	public sealed class GridContainerEditor : UnityEditor.Editor {

		private static readonly Color Accent = SperlichEditorTheme.ButtonAccent;
		private static readonly Color[] TrackColors = {
			new Color(0.35f, 0.70f, 0.95f),
			new Color(0.95f, 0.55f, 0.30f),
			new Color(0.55f, 0.85f, 0.45f),
			new Color(0.80f, 0.50f, 0.90f),
		};

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

			// ---- Tracks Preview -------------------------------------------------------------------
			var preview = BuildTrackPreview(columns, columnRepeat);
			root.Add(preview.element);
			root.Add(SperlichEditorWidgets.Spacer(4));

			// ---- Flow & Tracks --------------------------------------------------------------------
			var tracks = Section(root, "FLOW & TRACKS", true);
			tracks.Add(col.Row("Auto Flow", SperlichEditorWidgets.CreateEnumDropdown(autoFlow, Accent)));
			tracks.Add(col.Row("Start Corner", SperlichEditorWidgets.CreateEnumDropdown(startCorner, Accent)));
			tracks.Add(col.Row("Column Repeat", SperlichEditorWidgets.CreateEnumDropdown(columnRepeat, Accent)));

			var repeatFields = new VisualElement();
			repeatFields.Add(col.Property(columnRepeatTemplate, "Repeat Template"));
			repeatFields.Add(col.Row("Repeat Min Size", LayoutEditorStyle.CreateValueRow(SperlichEditorWidgets.CreateDragNumberField(columnRepeatMinSize), "px")));
			tracks.Add(repeatFields);

			var columnsField = col.Property(columns, "Columns");
			var rowsField = col.Property(rows, "Rows");
			var implicitRowField = col.Property(implicitRowTemplate, "Implicit Row");
			var implicitColumnField = col.Property(implicitColumnTemplate, "Implicit Column");
			tracks.Add(columnsField);
			tracks.Add(rowsField);
			tracks.Add(implicitRowField);
			tracks.Add(implicitColumnField);

			var columnFlowHint = new HelpBox("Column-Flow: Die Anzahl der Rows bestimmt, wie viele Items pro Spalte kommen — mindestens eine Row definieren.", HelpBoxMessageType.Info);
			tracks.Add(columnFlowHint);

			void RefreshTracks() {
				bool columnFlow = autoFlow.enumValueIndex == (int)GridAutoFlow.Column;
				bool repeatOn = columnRepeat.enumValueIndex != (int)GridRepeatMode.None && columnFlow == false;

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

			// ---- Alignment & Spacing --------------------------------------------------------------
			var align = Section(root, "ALIGNMENT & SPACING", true);
			align.Add(col.Row("Justify Items", SperlichEditorWidgets.CreateEnumDropdown(justifyItems, Accent)));
			align.Add(col.Row("Align Items", SperlichEditorWidgets.CreateEnumDropdown(alignItems, Accent)));
			align.Add(SperlichEditorWidgets.Spacer(2));
			align.Add(LayoutEditorStyle.CreateGapField(gap));
			align.Add(SperlichEditorWidgets.Spacer(2));
			align.Add(LayoutEditorStyle.CreatePaddingField(padding));

			root.TrackSerializedObjectValue(serializedObject, _ => {
				foreach (UnityEngine.Object t in targets) {
					if (t is GridContainer gc) gc.RequestRebuild();
				}
			});

			SperlichInspectorScroll.Preserve(root, target);
			return root;
		}

		private static VisualElement Section(VisualElement parent, string title, bool expanded) {
			var (header, body, _) = SperlichEditorWidgets.CreateChevronSection(title, expanded, SperlichEditorTheme.BgStep, null, nameof(GridContainerEditor));
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

		private (VisualElement element, System.Action refresh) BuildTrackPreview(SerializedProperty columns, SerializedProperty columnRepeat) {
			var bar = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row, height = 22,
					borderTopLeftRadius = 3, borderTopRightRadius = 3, borderBottomLeftRadius = 3, borderBottomRightRadius = 3,
					overflow = Overflow.Hidden, marginLeft = 4, marginRight = 4
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
