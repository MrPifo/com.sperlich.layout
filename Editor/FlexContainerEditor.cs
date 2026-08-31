using System;
using Sperlich.EditorKit;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Sperlich.UISystem.Editor {

	/// <summary>
	/// Inspector für <see cref="FlexContainer"/> nach den gemeinsamen SText/Sperlich-EditorKit-Standards.
	/// </summary>
	[CustomEditor(typeof(FlexContainer))]
	[CanEditMultipleObjects]
	public sealed class FlexContainerEditor : UnityEditor.Editor {

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

			SerializedProperty direction = serializedObject.FindProperty("direction");
			SerializedProperty wrap = serializedObject.FindProperty("wrap");
			SerializedProperty justify = serializedObject.FindProperty("justifyContent");
			SerializedProperty alignItems = serializedObject.FindProperty("alignItems");
			SerializedProperty alignContent = serializedObject.FindProperty("alignContent");
			SerializedProperty childSizing = serializedObject.FindProperty("childSizing");
			SerializedProperty gap = serializedObject.FindProperty("gap");
			SerializedProperty padding = serializedObject.FindProperty("padding");

			// ---- Layout ---------------------------------------------------------------------------
			var layout = Section(root, "LAYOUT", true);
			layout.Add(CreateDirectionControl(direction, Accent));
			layout.Add(SperlichEditorWidgets.Spacer(2));
			layout.Add(col.Row("Wrap", SperlichEditorWidgets.CreateEnumDropdown(wrap, Accent)));
			layout.Add(col.Row("Justify Content", SperlichEditorWidgets.CreateEnumDropdown(justify, Accent)));
			layout.Add(col.Row("Align Items", SperlichEditorWidgets.CreateEnumDropdown(alignItems, Accent)));
			layout.Add(col.Row("Align Content", SperlichEditorWidgets.CreateEnumDropdown(alignContent, Accent)));
			layout.Add(col.Row("Child Sizing", CreateChildSizingDropdown(childSizing, direction)));
			layout.Add(CreateReverseRow(direction));

			var wrapHint = new HelpBox("Align Content wirkt nur, wenn Wrap aktiv ist (mehrere Zeilen).", HelpBoxMessageType.Info);
			layout.Add(wrapHint);

			void RefreshHint() {
				bool noWrap = wrap.enumValueIndex == (int)FlexWrap.NoWrap;
				bool alignContentSet = alignContent.enumValueIndex != (int)AlignContent.Start;
				wrapHint.style.display = (noWrap && alignContentSet) ? DisplayStyle.Flex : DisplayStyle.None;
			}
			RefreshHint();
			root.TrackPropertyValue(wrap, _ => RefreshHint());
			root.TrackPropertyValue(alignContent, _ => RefreshHint());

			// ---- Spacing --------------------------------------------------------------------------
			var spacing = Section(root, "SPACING", true);
			spacing.Add(LayoutEditorStyle.CreateGapField(gap));
			spacing.Add(SperlichEditorWidgets.Spacer(2));
			spacing.Add(LayoutEditorStyle.CreatePaddingField(padding));

			root.TrackSerializedObjectValue(serializedObject, _ => {
				foreach (UnityEngine.Object t in targets) {
					if (t is FlexContainer fc) fc.RequestRebuild();
				}
			});

			SperlichInspectorScroll.Preserve(root, target);
			return root;
		}

		private static VisualElement Section(VisualElement parent, string title, bool expanded) {
			var (header, body, _) = SperlichEditorWidgets.CreateChevronSection(title, expanded, SperlichEditorTheme.BgStep, null, nameof(FlexContainerEditor));
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

		private static VisualElement CreateDirectionControl(SerializedProperty direction, Color accent) {
			var row = new VisualElement {
				style = {
					flexDirection = UnityEngine.UIElements.FlexDirection.Row,
					marginBottom = 6
				}
			};

			Texture2D horizIcon = EditorGUIUtility.IconContent("HorizontalLayoutGroup Icon")?.image as Texture2D;
			Texture2D vertIcon = EditorGUIUtility.IconContent("VerticalLayoutGroup Icon")?.image as Texture2D;

			bool IsHorizontal() {
				int dir = direction.enumValueIndex;
				return dir == (int)FlexDirection.Row || dir == (int)FlexDirection.RowReverse;
			}

			bool IsReverse() {
				int dir = direction.enumValueIndex;
				return dir == (int)FlexDirection.RowReverse || dir == (int)FlexDirection.ColumnReverse;
			}

			void SetButtonState(VisualElement btn, bool active) {
				SperlichEditorWidgets.SetBorderColor(btn, active ? accent : SperlichEditorTheme.BorderSubtle);
				btn.style.backgroundColor = active ? new Color(accent.r, accent.g, accent.b, 0.14f) : Color.clear;
				var lbl = btn.Q<Label>();
				if (lbl != null) {
					lbl.style.color = active ? accent : SperlichEditorTheme.TextSecondary;
					lbl.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
				}
				var img = btn.Q<Image>();
				if (img != null) {
					img.tintColor = active ? accent : SperlichEditorTheme.TextSecondary;
				}
			}

			Action refreshCallback = null;

			VisualElement CreateDirectionButton(Texture2D icon, string text, int targetAxis, bool isLast) {
				var btn = new VisualElement { pickingMode = PickingMode.Position };
				btn.style.flexDirection = UnityEngine.UIElements.FlexDirection.Row;
				btn.style.alignItems = Align.Center;
				btn.style.justifyContent = Justify.Center;
				btn.style.flexGrow = 1;
				btn.style.flexBasis = 0;
				btn.style.borderTopWidth = 1;
				btn.style.borderBottomWidth = 1;
				btn.style.borderLeftWidth = 1;
				btn.style.borderRightWidth = 1;
				SperlichEditorWidgets.SetRadius(btn, 3);
				btn.style.marginRight = isLast ? 0 : 3;
				btn.style.paddingTop = 4;
				btn.style.paddingBottom = 4;
				btn.style.paddingLeft = 4;
				btn.style.paddingRight = 4;
				SperlichEditorWidgets.ApplyColorTransition(btn, 100, "background-color", "border-color");
				SperlichEditorWidgets.SetHoverCursor(btn, MouseCursor.Link);

				if (icon != null) {
					var img = new Image { image = icon, pickingMode = PickingMode.Ignore };
					img.style.width = 14;
					img.style.height = 14;
					img.style.marginRight = 4;
					btn.Add(img);
				}

				var lbl = new Label(text) { pickingMode = PickingMode.Ignore };
				lbl.style.fontSize = 10;
				btn.Add(lbl);

				btn.RegisterCallback<ClickEvent>(_ => {
					bool rev = IsReverse();
					int newDir = targetAxis == 0
						? (rev ? (int)FlexDirection.RowReverse : (int)FlexDirection.Row)
						: (rev ? (int)FlexDirection.ColumnReverse : (int)FlexDirection.Column);
					if (direction.enumValueIndex != newDir) {
						direction.enumValueIndex = newDir;
						direction.serializedObject.ApplyModifiedProperties();
					}
					refreshCallback?.Invoke();
				});

				return btn;
			}

			var horizBtn = CreateDirectionButton(horizIcon, "Horizontal", 0, false);
			var vertBtn = CreateDirectionButton(vertIcon, "Vertical", 1, true);

			row.Add(horizBtn);
			row.Add(vertBtn);

			void Refresh() {
				bool horiz = IsHorizontal();
				SetButtonState(horizBtn, horiz);
				SetButtonState(vertBtn, !horiz);
			}

			refreshCallback = Refresh;
			Refresh();
			row.TrackPropertyValue(direction, _ => Refresh());
			return row;
		}

		private VisualElement CreateReverseRow(SerializedProperty directionProp) {
			bool IsReverse() {
				int dir = directionProp.enumValueIndex;
				return dir == (int)FlexDirection.RowReverse || dir == (int)FlexDirection.ColumnReverse;
			}

			bool IsHorizontal() {
				int dir = directionProp.enumValueIndex;
				return dir == (int)FlexDirection.Row || dir == (int)FlexDirection.RowReverse;
			}

			var pill = new PillToggle(IsReverse());
			pill.Clicked += () => {
				bool horiz = IsHorizontal();
				bool newRev = !IsReverse();
				int newDir = horiz
					? (newRev ? (int)FlexDirection.RowReverse : (int)FlexDirection.Row)
					: (newRev ? (int)FlexDirection.ColumnReverse : (int)FlexDirection.Column);
				directionProp.enumValueIndex = newDir;
				directionProp.serializedObject.ApplyModifiedProperties();
				pill.SetValue(newRev);
			};

			VisualElement row = col.Row("Reverse Direction", pill);
			row.TrackPropertyValue(directionProp, _ => pill.SetValue(IsReverse()));
			return row;
		}

		private static VisualElement CreateChildSizingDropdown(SerializedProperty childSizingProp, SerializedProperty directionProp) {
			string[] RowLabels = { "None", "Stretch Width", "Stretch Height", "Stretch Both" };
			string[] ColLabels = { "None", "Stretch Height", "Stretch Width", "Stretch Both" };

			bool IsRow() {
				int dir = directionProp.enumValueIndex;
				return dir == (int)FlexDirection.Row || dir == (int)FlexDirection.RowReverse;
			}

			string LabelFor(int index) {
				string[] labels = IsRow() ? RowLabels : ColLabels;
				if (index >= 0 && index < labels.Length) return labels[index];
				return "None";
			}

			var dd = SperlichEditorWidgets.BuildDropdown(
				() => 4,
				LabelFor,
				() => childSizingProp.enumValueIndex,
				i => {
					if (childSizingProp.enumValueIndex == i) return;
					childSizingProp.enumValueIndex = i;
					childSizingProp.serializedObject.ApplyModifiedProperties();
				},
				Accent);

			void Refresh() {
				Label valLbl = dd.Q<Label>();
				if (valLbl != null) valLbl.text = LabelFor(childSizingProp.enumValueIndex);
			}

			dd.TrackPropertyValue(childSizingProp, _ => Refresh());
			dd.TrackPropertyValue(directionProp, _ => Refresh());
			return dd;
		}
	}
}
