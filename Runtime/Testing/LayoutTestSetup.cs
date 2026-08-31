using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Sperlich.UISystem {

	/// <summary>
	/// Preset-Auswahl für verschiedene Test- und Showcase-Szenarien der Sperlich-Layout-Elemente.
	/// </summary>
	public enum LayoutTestPreset {
		/// <summary>Zeigt alle Test-Szenarien nebeneinander in einer 4x2 Galerie über den gesamten 1920x1080 Bildschirm.</summary>
		AllInGallery = 0,
		/// <summary>Testet FlexContainer-Richtungen (Row, Column, Reversals) und Wrap-Modi im Vollbild.</summary>
		FlexFlowAndWrap = 1,
		/// <summary>Testet alle FlexElement-Größenmodi (Pixels, Flexible, Percent, Auto, Aspect, Min/Max) im Vollbild.</summary>
		FlexSizingModes = 2,
		/// <summary>Testet Justify-Content, Align-Items und Align-Self-Überschreibungen im Vollbild.</summary>
		FlexAlignAndJustify = 3,
		/// <summary>Testet GridContainer-Spaltentypen (Pixels, fr, auto, minmax) im Vollbild.</summary>
		GridTracksAndUnits = 4,
		/// <summary>Testet ColumnSpan, RowSpan sowie explizite Zellplatzierung (ColumnStart, RowStart) im Vollbild.</summary>
		GridSpansAndPositions = 5,
		/// <summary>Testet responsiven Spalten-Repeat (repeat auto-fit / auto-fill) im Vollbild.</summary>
		GridAutoRepeatResponsive = 6,
		/// <summary>Testet komplexe verschachtelte UI-Layouts (Cards mit Header, Grid-Body und Footer) im Vollbild.</summary>
		NestedComplexCards = 7,
		/// <summary>Testet FlexElement im Standalone-Modus (ohne übergeordnete LayoutGroup) im Vollbild.</summary>
		StandaloneFlexElement = 8,
		/// <summary>Testet FlexContainer Child Sizing Modi (Default, FillLineEqual, FillAndStretch) im Vollbild.</summary>
		ChildAutoSizing = 9
	}

	/// <summary>
	/// Test-Setup und Visualisierung für <see cref="FlexContainer"/>, <see cref="FlexElement"/> und <see cref="GridContainer"/>.
	/// Füllt automatisch die gesamte 1920x1080 GameView aus und funktioniert im Unity Editor (Edit Mode) sowie zur Laufzeit (Play Mode).
	/// </summary>
	[AddComponentMenu("Sperlich UI/Testing/Layout Test Setup")]
	[ExecuteAlways]
	[DisallowMultipleComponent]
	public class LayoutTestSetup : MonoBehaviour {

		[Header("Preset Selection")]
		[SerializeField] private LayoutTestPreset m_preset = LayoutTestPreset.AllInGallery;

		[Header("Settings")]
		[SerializeField] private bool m_autoRegenerateInEditMode = true;

		/// <summary>Aktives Test-Preset.</summary>
		public LayoutTestPreset Preset {
			get => m_preset;
			set {
				m_preset = value;
				GenerateSelectedPreset();
			}
		}

		private static readonly Color ColorHeader = new Color(0.18f, 0.22f, 0.28f, 1f);
		private static readonly Color ColorPanelBg = new Color(0.11f, 0.13f, 0.17f, 0.95f);
		private static readonly Color ColorCardA = new Color(0.25f, 0.45f, 0.85f, 0.9f);
		private static readonly Color ColorCardB = new Color(0.85f, 0.35f, 0.25f, 0.9f);
		private static readonly Color ColorCardC = new Color(0.25f, 0.75f, 0.45f, 0.9f);
		private static readonly Color ColorCardD = new Color(0.85f, 0.65f, 0.15f, 0.9f);
		private static readonly Color ColorCardE = new Color(0.65f, 0.35f, 0.85f, 0.9f);
		private static readonly Color ColorCardF = new Color(0.2f, 0.7f, 0.8f, 0.9f);

		private Canvas targetCanvas;
		private RectTransform rootContainer;

		private void Awake() {
			EnsureRootContainer();
		}

		private void Start() {
			if (rootContainer != null && rootContainer.childCount == 0) {
				GenerateSelectedPreset();
			}
		}

#if UNITY_EDITOR
		private LayoutTestPreset m_lastPreset = (LayoutTestPreset)(-1);

		private void OnValidate() {
			if (m_preset != m_lastPreset && isActiveAndEnabled) {
				m_lastPreset = m_preset;
				UnityEditor.EditorApplication.delayCall += () => {
					if (this != null && this.gameObject != null) {
						GenerateSelectedPreset();
					}
				};
			}
		}
#endif

		/// <summary>Generiert das aktuell im Inspector ausgewählte Test-Preset.</summary>
		[ContextMenu("Generate / Refresh Selected Preset")]
		public void GenerateSelectedPreset() {
			EnsureRootContainer();
			ClearLayouts();

			bool isSingle = m_preset != LayoutTestPreset.AllInGallery;

			if (!isSingle) {
				BuildGallery();
			} else {
				// Einzelszenario spannt sich voll über den gesamten 1920x1080 Bildschirm
				var wrapperGo = new GameObject("SingleScenarioWrapper", typeof(RectTransform));
				var wrapperRt = wrapperGo.GetComponent<RectTransform>();
				wrapperRt.SetParent(rootContainer, false);
				wrapperRt.anchorMin = Vector2.zero;
				wrapperRt.anchorMax = Vector2.one;
				wrapperRt.offsetMin = new Vector2(24f, 24f);
				wrapperRt.offsetMax = new Vector2(-24f, -24f);

				switch (m_preset) {
					case LayoutTestPreset.FlexFlowAndWrap:
						BuildFlexFlowScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.FlexSizingModes:
						BuildFlexSizingScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.FlexAlignAndJustify:
						BuildFlexAlignScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.GridTracksAndUnits:
						BuildGridTrackScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.GridSpansAndPositions:
						BuildGridSpanScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.GridAutoRepeatResponsive:
						BuildGridRepeatScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.NestedComplexCards:
						BuildNestedCardScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.StandaloneFlexElement:
						BuildStandaloneScenarios(wrapperRt, isSingle);
						break;
					case LayoutTestPreset.ChildAutoSizing:
						BuildChildSizingScenarios(wrapperRt, isSingle);
						break;
				}
			}

			ForceLayoutRefresh();
		}

		/// <summary>Generiert die vollständige Galerie mit allen 9 Szenarien im 3x3 Grid.</summary>
		[ContextMenu("Generate Full Gallery (All Presets)")]
		public void GenerateFullGallery() {
			m_preset = LayoutTestPreset.AllInGallery;
			GenerateSelectedPreset();
		}

		/// <summary>Löscht alle erstellten Test-Layouts.</summary>
		[ContextMenu("Clear Layouts")]
		public void ClearLayouts() {
			if (rootContainer == null) {
				return;
			}

			for (int i = rootContainer.childCount - 1; i >= 0; i--) {
				Transform child = rootContainer.GetChild(i);
				SafeDestroy(child.gameObject);
			}
		}

		/// <summary>Stellt sicher, dass ein 1920x1080 Canvas und der Wurzel-Container existieren.</summary>
		private void EnsureRootContainer() {
			targetCanvas = GetComponentInParent<Canvas>();
			if (targetCanvas == null) {
				targetCanvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
			}

			if (targetCanvas == null) {
				var canvasGo = new GameObject("LayoutCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
				targetCanvas = canvasGo.GetComponent<Canvas>();
				targetCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
			}

			// CanvasScaler für exakt 1920x1080 Fullscreen konfigurieren
			if (targetCanvas.TryGetComponent(out CanvasScaler scaler) == false) {
				scaler = targetCanvas.gameObject.AddComponent<CanvasScaler>();
			}
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920f, 1080f);
			scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
			scaler.matchWidthOrHeight = 0.5f;

			RectTransform canvasRt = targetCanvas.GetComponent<RectTransform>();
			if (canvasRt != null && (canvasRt.rect.width < 100f || canvasRt.rect.height < 100f)) {
				canvasRt.sizeDelta = new Vector2(1920f, 1080f);
			}

			if (rootContainer == null) {
				Transform existing = targetCanvas.transform.Find("LayoutTest_Root");
				if (existing != null) {
					rootContainer = existing as RectTransform;
				} else {
					var rootGo = new GameObject("LayoutTest_Root", typeof(RectTransform), typeof(Image));
					rootContainer = rootGo.GetComponent<RectTransform>();
					rootContainer.SetParent(targetCanvas.transform, false);

					var bg = rootGo.GetComponent<Image>();
					bg.color = new Color(0.07f, 0.08f, 0.11f, 1f);
				}
			}

			// Vollflächig über gesamte 1920x1080 GameView spannen
			rootContainer.anchorMin = Vector2.zero;
			rootContainer.anchorMax = Vector2.one;
			rootContainer.offsetMin = Vector2.zero;
			rootContainer.offsetMax = Vector2.zero;
			rootContainer.pivot = new Vector2(0.5f, 0.5f);

			Canvas.ForceUpdateCanvases();
		}

		/// <summary>Erzwingt eine sofortige Neuberechnung aller uGUI-Layouts.</summary>
		private void ForceLayoutRefresh() {
			if (rootContainer != null) {
				Canvas.ForceUpdateCanvases();
				LayoutRebuilder.ForceRebuildLayoutImmediate(rootContainer);
			}
		}

		// ====================================================================
		// SZENARIEN-GENERIERUNG
		// ====================================================================

		/// <summary>Baut die Gesamtschau mit allen 9 Test-Sektionen in einem 3x3 Grid auf.</summary>
		private void BuildGallery() {
			var gridGo = new GameObject("GalleryGrid", typeof(RectTransform));
			var gridRt = gridGo.GetComponent<RectTransform>();
			gridRt.SetParent(rootContainer, false);
			gridRt.anchorMin = Vector2.zero;
			gridRt.anchorMax = Vector2.one;
			gridRt.offsetMin = new Vector2(16f, 16f);
			gridRt.offsetMax = new Vector2(-16f, -16f);

			var grid = gridGo.AddComponent<GridContainer>();
			grid.Columns.Clear();
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1f));

			grid.Rows.Clear();
			grid.Rows.Add(GridTrack.Fr(1f));
			grid.Rows.Add(GridTrack.Fr(1f));
			grid.Rows.Add(GridTrack.Fr(1f));

			grid.Gap = new Vector2(12f, 12f);
			grid.Padding = new RectOffset(0, 0, 0, 0);
			grid.AutoFlow = GridAutoFlow.Row;

			BuildFlexFlowScenarios(gridRt, false);
			BuildFlexSizingScenarios(gridRt, false);
			BuildFlexAlignScenarios(gridRt, false);
			BuildGridTrackScenarios(gridRt, false);
			BuildGridSpanScenarios(gridRt, false);
			BuildGridRepeatScenarios(gridRt, false);
			BuildNestedCardScenarios(gridRt, false);
			BuildStandaloneScenarios(gridRt, false);
			BuildChildSizingScenarios(gridRt, false);
		}

		/// <summary>Szenario 1: FlexContainer Direction & Wrap.</summary>
		private void BuildFlexFlowScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "1. Flex Flow & Wrap", isSingle);

			var containerGo = new GameObject("FlowContainer", typeof(RectTransform));
			var containerRt = containerGo.GetComponent<RectTransform>();
			containerRt.SetParent(panel.transform, false);
			containerRt.anchorMin = Vector2.zero;
			containerRt.anchorMax = Vector2.one;
			containerRt.offsetMin = Vector2.zero;
			containerRt.offsetMax = Vector2.zero;

			var flex = containerGo.AddComponent<FlexContainer>();
			flex.Direction = FlexDirection.Row;
			flex.Wrap = FlexWrap.Wrap;
			flex.Gap = isSingle ? new Vector2(16f, 16f) : new Vector2(6f, 6f);
			flex.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);
			flex.JustifyContent = JustifyContent.Start;
			flex.AlignItems = AlignItems.Center;

			var flexEl = containerGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			Vector2 boxSize = isSingle ? new Vector2(320f, 130f) : new Vector2(95f, 48f);
			float fontSize = isSingle ? 18f : 11f;

			for (int i = 1; i <= 10; i++) {
				Color c = (i % 2 == 0) ? ColorCardA : ColorCardC;
				CreateBox(containerRt, $"Item_{i}", c, boxSize, $"Item {i}\n{(int)boxSize.x}x{(int)boxSize.y}", fontSize);
			}
		}

		/// <summary>Szenario 2: Alle FlexElement Sizing Modes.</summary>
		private void BuildFlexSizingScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "2. Flex Sizing Modes", isSingle);

			var containerGo = new GameObject("SizingContainer", typeof(RectTransform));
			var containerRt = containerGo.GetComponent<RectTransform>();
			containerRt.SetParent(panel.transform, false);
			containerRt.anchorMin = Vector2.zero;
			containerRt.anchorMax = Vector2.one;
			containerRt.offsetMin = Vector2.zero;
			containerRt.offsetMax = Vector2.zero;

			var flex = containerGo.AddComponent<FlexContainer>();
			flex.Direction = FlexDirection.Row;
			flex.Wrap = FlexWrap.NoWrap;
			flex.Gap = isSingle ? new Vector2(16f, 16f) : new Vector2(6f, 6f);
			flex.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);
			flex.AlignItems = AlignItems.Stretch;

			var flexEl = containerGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			float fontSize = isSingle ? 18f : 11f;
			float pxSize = isSingle ? 140f : 55f;
			float aspectVal = isSingle ? 0.35f : 0.22f;

			// 1. Pixels
			var b1 = CreateBox(containerRt, "PxBox", ColorCardA, Vector2.zero, $"Pixels\n{(int)pxSize}px", fontSize);
			var fe1 = b1.GetComponent<FlexElement>();
			fe1.Width = FlexSize.Pixels(pxSize);

			// 2. Flexible 1fr
			var b2 = CreateBox(containerRt, "Flex1", ColorCardB, Vector2.zero, "Flex\n1x", fontSize);
			var fe2 = b2.GetComponent<FlexElement>();
			fe2.Width = FlexSize.Flexible(1f);

			// 3. Flexible 2fr
			var b3 = CreateBox(containerRt, "Flex2", ColorCardC, Vector2.zero, "Flex\n2x", fontSize);
			var fe3 = b3.GetComponent<FlexElement>();
			fe3.Width = FlexSize.Flexible(2f);

			// 4. Percent
			var b4 = CreateBox(containerRt, "PercentBox", ColorCardD, Vector2.zero, "Percent\n15%", fontSize);
			var fe4 = b4.GetComponent<FlexElement>();
			fe4.Width = FlexSize.Percent(15f);

			// 5. Aspect
			var b5 = CreateBox(containerRt, "AspectBox", ColorCardE, Vector2.zero, $"Aspect\n{aspectVal:0.00}", fontSize);
			var fe5 = b5.GetComponent<FlexElement>();
			fe5.Width = FlexSize.Aspect(aspectVal);

			// 6. Auto (Content-based)
			var b6 = CreateBox(containerRt, "AutoBox", ColorCardF, Vector2.zero, "Auto\nSize", fontSize);
			var fe6 = b6.GetComponent<FlexElement>();
			fe6.Width = FlexSize.Auto();
		}

		/// <summary>Szenario 3: Flex JustifyContent, AlignItems und AlignSelf.</summary>
		private void BuildFlexAlignScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "3. Flex Alignment & AlignSelf", isSingle);

			var containerGo = new GameObject("AlignContainer", typeof(RectTransform));
			var containerRt = containerGo.GetComponent<RectTransform>();
			containerRt.SetParent(panel.transform, false);
			containerRt.anchorMin = Vector2.zero;
			containerRt.anchorMax = Vector2.one;
			containerRt.offsetMin = Vector2.zero;
			containerRt.offsetMax = Vector2.zero;

			var flex = containerGo.AddComponent<FlexContainer>();
			flex.Direction = FlexDirection.Row;
			flex.Wrap = FlexWrap.NoWrap;
			flex.JustifyContent = JustifyContent.SpaceBetween;
			flex.AlignItems = AlignItems.Center;
			flex.Gap = isSingle ? new Vector2(24f, 24f) : new Vector2(6f, 6f);
			flex.Padding = isSingle ? new RectOffset(24, 24, 24, 24) : new RectOffset(6, 6, 6, 6);

			var flexEl = containerGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			Vector2 boxSize = isSingle ? new Vector2(260f, 180f) : new Vector2(80f, 50f);
			float fontSize = isSingle ? 20f : 11f;

			CreateBox(containerRt, "AlignDefault", ColorCardA, boxSize, "Default\n(Center)", fontSize);

			var b2 = CreateBox(containerRt, "AlignSelfStart", ColorCardB, boxSize, "Self\nStart", fontSize);
			var fe2 = b2.GetComponent<FlexElement>();
			fe2.AlignSelf = AlignSelf.Start;

			var b3 = CreateBox(containerRt, "AlignSelfEnd", ColorCardC, boxSize, "Self\nEnd", fontSize);
			var fe3 = b3.GetComponent<FlexElement>();
			fe3.AlignSelf = AlignSelf.End;

			var b4 = CreateBox(containerRt, "AlignSelfStretch", ColorCardD, boxSize, "Self\nStretch", fontSize);
			var fe4 = b4.GetComponent<FlexElement>();
			fe4.AlignSelf = AlignSelf.Stretch;
		}

		/// <summary>Szenario 4: CSS Grid Tracks (Pixels, fr, Auto, MinMax).</summary>
		private void BuildGridTrackScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "4. CSS Grid Tracks", isSingle);

			var gridGo = new GameObject("TracksGrid", typeof(RectTransform));
			var gridRt = gridGo.GetComponent<RectTransform>();
			gridRt.SetParent(panel.transform, false);
			gridRt.anchorMin = Vector2.zero;
			gridRt.anchorMax = Vector2.one;
			gridRt.offsetMin = Vector2.zero;
			gridRt.offsetMax = Vector2.zero;

			var grid = gridGo.AddComponent<GridContainer>();
			float pxCol = isSingle ? 240f : 75f;
			float minCol = isSingle ? 180f : 60f;
			float r1H = isSingle ? 140f : 42f;

			grid.Columns.Clear();
			grid.Columns.Add(GridTrack.Pixels(pxCol));
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1.5f));
			grid.Columns.Add(GridTrack.Auto());
			grid.Columns.Add(GridTrack.MinMax(minCol, 1f));

			grid.Rows.Clear();
			grid.Rows.Add(GridTrack.Pixels(r1H));
			grid.Rows.Add(GridTrack.Fr(1f));

			grid.Gap = isSingle ? new Vector2(16f, 16f) : new Vector2(6f, 6f);
			grid.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var flexEl = gridGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			float fontSize = isSingle ? 18f : 11f;
			string[] labels = { $"{pxCol}px", "1fr", "1.5fr", "Auto Content", $"minmax({minCol}px, 1fr)", "R2 C1", "R2 C2", "R2 C3", "R2 Auto", "R2 MinMax" };
			Color[] colors = { ColorCardA, ColorCardB, ColorCardC, ColorCardD, ColorCardE };

			for (int i = 0; i < labels.Length; i++) {
				CreateBox(gridRt, $"Cell_{i}", colors[i % colors.Length], Vector2.zero, labels[i], fontSize);
			}
		}

		/// <summary>Szenario 5: CSS Grid Spans und explizite Zellplatzierung.</summary>
		private void BuildGridSpanScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "5. Grid Spans & Placement", isSingle);

			var gridGo = new GameObject("SpansGrid", typeof(RectTransform));
			var gridRt = gridGo.GetComponent<RectTransform>();
			gridRt.SetParent(panel.transform, false);
			gridRt.anchorMin = Vector2.zero;
			gridRt.anchorMax = Vector2.one;
			gridRt.offsetMin = Vector2.zero;
			gridRt.offsetMax = Vector2.zero;

			var grid = gridGo.AddComponent<GridContainer>();
			grid.Columns.Clear();
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1f));

			grid.Rows.Clear();
			grid.Rows.Add(GridTrack.Fr(1f));
			grid.Rows.Add(GridTrack.Fr(1f));
			grid.Rows.Add(GridTrack.Fr(1f));

			grid.Gap = isSingle ? new Vector2(16f, 16f) : new Vector2(6f, 6f);
			grid.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var flexEl = gridGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			float fontSize = isSingle ? 18f : 11f;

			// Item 1: ColSpan 2
			var b1 = CreateBox(gridRt, "Span2Col", ColorCardA, Vector2.zero, "ColSpan: 2", fontSize);
			var fe1 = b1.GetComponent<FlexElement>();
			fe1.ColumnSpan = 2;

			// Item 2: Normal
			CreateBox(gridRt, "Item2", ColorCardB, Vector2.zero, "1x1", fontSize);

			// Item 3: RowSpan 2
			var b3 = CreateBox(gridRt, "Span2Row", ColorCardC, Vector2.zero, "RowSpan: 2", fontSize);
			var fe3 = b3.GetComponent<FlexElement>();
			fe3.RowSpan = 2;

			// Item 4: ColSpan 2 + RowSpan 2
			var b4 = CreateBox(gridRt, "Span2x2", ColorCardD, Vector2.zero, "Span 2x2", fontSize);
			var fe4 = b4.GetComponent<FlexElement>();
			fe4.ColumnSpan = 2;
			fe4.RowSpan = 2;

			// Item 5: Normal
			CreateBox(gridRt, "Item5", ColorCardE, Vector2.zero, "1x1", fontSize);

			// Item 6: Explicit Placement (Row 3, Col 4)
			var b6 = CreateBox(gridRt, "ExplicitPos", ColorCardF, Vector2.zero, "Explicit\nCol:4, Row:3", fontSize);
			var fe6 = b6.GetComponent<FlexElement>();
			fe6.ColumnStart = 4;
			fe6.RowStart = 3;
		}

		/// <summary>Szenario 6: Responsive CSS Grid Auto-Repeat.</summary>
		private void BuildGridRepeatScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "6. Grid Repeat (Auto-Fit)", isSingle);

			var gridGo = new GameObject("RepeatGrid", typeof(RectTransform));
			var gridRt = gridGo.GetComponent<RectTransform>();
			gridRt.SetParent(panel.transform, false);
			gridRt.anchorMin = Vector2.zero;
			gridRt.anchorMax = Vector2.one;
			gridRt.offsetMin = Vector2.zero;
			gridRt.offsetMax = Vector2.zero;

			float rowH = isSingle ? 180f : 45f;
			float minRepeat = isSingle ? 260f : 120f;
			float fontSize = isSingle ? 18f : 11f;

			var grid = gridGo.AddComponent<GridContainer>();
			grid.ColumnRepeat = GridRepeatMode.AutoFit;
			grid.ColumnRepeatMinSize = minRepeat;
			grid.Rows.Clear();
			grid.Rows.Add(GridTrack.Pixels(rowH));
			grid.Rows.Add(GridTrack.Pixels(rowH));
			grid.Gap = isSingle ? new Vector2(16f, 16f) : new Vector2(6f, 6f);
			grid.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var flexEl = gridGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			for (int i = 1; i <= 8; i++) {
				CreateBox(gridRt, $"RepeatItem_{i}", (i % 2 == 0) ? ColorCardB : ColorCardF, new Vector2(0f, rowH), $"Auto-Fit #{i}\nmin: {minRepeat}px", fontSize);
			}
		}

		/// <summary>Szenario 7: Verschachtelte Real-World UI-Karten.</summary>
		private void BuildNestedCardScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "7. Nested Cards (Flex + Grid)", isSingle);

			var gridGo = new GameObject("CardsGrid", typeof(RectTransform));
			var gridRt = gridGo.GetComponent<RectTransform>();
			gridRt.SetParent(panel.transform, false);
			gridRt.anchorMin = Vector2.zero;
			gridRt.anchorMax = Vector2.one;
			gridRt.offsetMin = Vector2.zero;
			gridRt.offsetMax = Vector2.zero;

			var grid = gridGo.AddComponent<GridContainer>();
			grid.Columns.Clear();
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Columns.Add(GridTrack.Fr(1f));
			grid.Rows.Clear();
			grid.Rows.Add(GridTrack.Fr(1f));
			grid.Gap = isSingle ? new Vector2(24f, 24f) : new Vector2(8f, 8f);
			grid.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var flexEl = gridGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			CreateSampleCard(gridRt, "InventoryCard", "Inventar", ColorCardA, isSingle);
			CreateSampleCard(gridRt, "StatsCard", "Charakter-Status", ColorCardC, isSingle);
		}

		/// <summary>Szenario 8: FlexElement im Standalone-Modus (ohne LayoutGroup).</summary>
		private void BuildStandaloneScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "8. Standalone FlexElement", isSingle);

			var contentGo = new GameObject("StandaloneContainer", typeof(RectTransform));
			var contentRt = contentGo.GetComponent<RectTransform>();
			contentRt.SetParent(panel.transform, false);
			contentRt.anchorMin = Vector2.zero;
			contentRt.anchorMax = Vector2.one;
			contentRt.offsetMin = Vector2.zero;
			contentRt.offsetMax = Vector2.zero;

			var flexEl = contentGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			float fontSize = isSingle ? 24f : 12f;
			var box = CreateBox(contentRt, "StandaloneBox", ColorCardE, Vector2.zero, "Standalone FlexElement Mode\nWidth: 70%\nHeight: 60%", fontSize);
			var boxFe = box.GetComponent<FlexElement>();
			boxFe.Width = FlexSize.Percent(70f);
			boxFe.Height = FlexSize.Percent(60f);

			var boxRt = box.GetComponent<RectTransform>();
			boxRt.anchorMin = new Vector2(0.5f, 0.5f);
			boxRt.anchorMax = new Vector2(0.5f, 0.5f);
			boxRt.pivot = new Vector2(0.5f, 0.5f);
			boxRt.anchoredPosition = Vector2.zero;
		}

		/// <summary>Szenario 9: FlexContainer Child Sizing Modi (Default, FillLineEqual, FillAndStretch).</summary>
		private void BuildChildSizingScenarios(RectTransform parent, bool isSingle) {
			var panel = CreateTestCard(parent, "9. Child Auto-Sizing", isSingle);

			var containerGo = new GameObject("ChildSizingContainer", typeof(RectTransform));
			var containerRt = containerGo.GetComponent<RectTransform>();
			containerRt.SetParent(panel.transform, false);
			containerRt.anchorMin = Vector2.zero;
			containerRt.anchorMax = Vector2.one;
			containerRt.offsetMin = Vector2.zero;
			containerRt.offsetMax = Vector2.zero;

			var flex = containerGo.AddComponent<FlexContainer>();
			flex.Direction = FlexDirection.Column;
			flex.Gap = isSingle ? new Vector2(14f, 14f) : new Vector2(6f, 6f);
			flex.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var flexEl = containerGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Percent(100f);
			flexEl.Height = FlexSize.Flexible(1f);

			float fontSize = isSingle ? 14f : 8.5f;
			float labelFontSize = isSingle ? 15f : 9.5f;

			// Row 1: None (Children keep their own sizes)
			CreateChildSizingRow(containerRt, "1. None (Own Size): 140x50 px", FlexChildSizing.None, fontSize, labelFontSize, isSingle);

			// Row 2: MainAxis (3 Children share 100% width equally - 33.3% each)
			CreateChildSizingRow(containerRt, "2. Stretch Width (Main): 33.3% Width & 50px Height", FlexChildSizing.MainAxis, fontSize, labelFontSize, isSingle);

			// Row 3: CrossAxis (Children keep their 140px width, but stretch to full row height)
			CreateChildSizingRow(containerRt, "3. Stretch Height (Cross): 140px Width & Full Height", FlexChildSizing.CrossAxis, fontSize, labelFontSize, isSingle);

			// Row 4: Both (Children share 100% width and stretch to full row height)
			CreateChildSizingRow(containerRt, "4. Stretch Both (Main & Cross): 33.3% Width & Full Height", FlexChildSizing.Both, fontSize, labelFontSize, isSingle);
		}

		private void CreateChildSizingRow(RectTransform parent, string title, FlexChildSizing mode, float fontSize, float labelFontSize, bool isSingle) {
			var sectionGo = new GameObject($"Section_{mode}", typeof(RectTransform));
			var sectionRt = sectionGo.GetComponent<RectTransform>();
			sectionRt.SetParent(parent, false);

			var sFlex = sectionGo.AddComponent<FlexContainer>();
			sFlex.Direction = FlexDirection.Column;
			sFlex.Gap = isSingle ? new Vector2(6f, 6f) : new Vector2(2f, 2f);

			var sFe = sectionGo.AddComponent<FlexElement>();
			sFe.Width = FlexSize.Percent(100f);
			sFe.Height = FlexSize.Flexible(1f);

			// Title Label
			var titleGo = new GameObject("Label", typeof(RectTransform));
			var titleRt = titleGo.GetComponent<RectTransform>();
			titleRt.SetParent(sectionRt, false);
			var tFe = titleGo.AddComponent<FlexElement>();
			tFe.Width = FlexSize.Percent(100f);
			tFe.Height = FlexSize.Pixels(isSingle ? 24f : 14f);
			CreateLabel(titleRt, title, labelFontSize, Color.white, TextAlignmentOptions.MidlineLeft, Vector2.zero);

			// Row Container with visual background
			var rowGo = new GameObject($"RowContainer_{mode}", typeof(RectTransform), typeof(Image));
			var rowRt = rowGo.GetComponent<RectTransform>();
			rowRt.SetParent(sectionRt, false);
			rowGo.GetComponent<Image>().color = new Color(0.12f, 0.14f, 0.18f, 1f);

			var rowFlex = rowGo.AddComponent<FlexContainer>();
			rowFlex.Direction = FlexDirection.Row;
			rowFlex.ChildSizing = mode;
			rowFlex.Gap = isSingle ? new Vector2(10f, 10f) : new Vector2(4f, 4f);
			rowFlex.Padding = isSingle ? new RectOffset(8, 8, 8, 8) : new RectOffset(4, 4, 4, 4);

			var rFe = rowGo.AddComponent<FlexElement>();
			rFe.Width = FlexSize.Percent(100f);
			rFe.Height = FlexSize.Flexible(1f);

			Color[] colors = { ColorCardA, ColorCardC, ColorCardD };
			string[] names = { "Btn 1", "Btn 2", "Btn 3" };
			float defaultBtnW = isSingle ? 140f : 55f;
			float defaultBtnH = isSingle ? 60f : 24f;

			for (int i = 0; i < 3; i++) {
				var btnGo = new GameObject(names[i], typeof(RectTransform), typeof(Image));
				var btnRt = btnGo.GetComponent<RectTransform>();
				btnRt.SetParent(rowRt, false);
				btnGo.GetComponent<Image>().color = colors[i];

				btnRt.sizeDelta = new Vector2(defaultBtnW, defaultBtnH);

				CreateLabel(btnRt, $"{names[i]}\n{mode}", fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero);
			}
		}

		/// <summary>Erstellt eine beispielhafte verschachtelte UI-Karte.</summary>
		private void CreateSampleCard(RectTransform parent, string cardName, string title, Color accent, bool isSingle) {
			var cardGo = new GameObject(cardName, typeof(RectTransform), typeof(Image));
			var cardRt = cardGo.GetComponent<RectTransform>();
			cardRt.SetParent(parent, false);
			cardRt.anchorMin = Vector2.zero;
			cardRt.anchorMax = Vector2.one;
			cardRt.offsetMin = Vector2.zero;
			cardRt.offsetMax = Vector2.zero;

			var img = cardGo.GetComponent<Image>();
			img.color = new Color(0.16f, 0.19f, 0.24f, 1f);

			var flex = cardGo.AddComponent<FlexContainer>();
			flex.Direction = FlexDirection.Column;
			flex.Gap = isSingle ? new Vector2(12f, 12f) : new Vector2(4f, 4f);
			flex.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var cardFe = cardGo.AddComponent<FlexElement>();
			cardFe.Width = FlexSize.Percent(100f);
			cardFe.Height = FlexSize.Flexible(1f);

			float headerH = isSingle ? 54f : 26f;
			float btnH = isSingle ? 48f : 24f;
			float fontSize = isSingle ? 20f : 11f;

			// Header
			var header = CreateBox(cardRt, "Header", accent, new Vector2(0f, headerH), title, fontSize + 2f);
			var hFlex = header.GetComponent<FlexElement>();
			hFlex.Width = FlexSize.Percent(100f);
			hFlex.Height = FlexSize.Pixels(headerH);

			// Body (Grid mit 2x2 Slots)
			var bodyGo = new GameObject("BodyGrid", typeof(RectTransform));
			var bodyRt = bodyGo.GetComponent<RectTransform>();
			bodyRt.SetParent(cardRt, false);
			bodyRt.anchorMin = Vector2.zero;
			bodyRt.anchorMax = Vector2.one;
			bodyRt.offsetMin = Vector2.zero;
			bodyRt.offsetMax = Vector2.zero;

			var bodyGrid = bodyGo.AddComponent<GridContainer>();
			bodyGrid.Columns.Clear();
			bodyGrid.Columns.Add(GridTrack.Fr(1f));
			bodyGrid.Columns.Add(GridTrack.Fr(1f));
			bodyGrid.Rows.Clear();
			bodyGrid.Rows.Add(GridTrack.Fr(1f));
			bodyGrid.Rows.Add(GridTrack.Fr(1f));
			bodyGrid.Gap = isSingle ? new Vector2(10f, 10f) : new Vector2(4f, 4f);

			var bFlex = bodyGo.AddComponent<FlexElement>();
			bFlex.Width = FlexSize.Percent(100f);
			bFlex.Height = FlexSize.Flexible(1f);

			for (int s = 1; s <= 4; s++) {
				CreateBox(bodyRt, $"Slot_{s}", new Color(0.22f, 0.26f, 0.32f, 1f), Vector2.zero, $"Slot {s}", fontSize);
			}

			// Footer (Row mit Action Buttons)
			var footerGo = new GameObject("FooterRow", typeof(RectTransform));
			var footerRt = footerGo.GetComponent<RectTransform>();
			footerRt.SetParent(cardRt, false);
			footerRt.anchorMin = Vector2.zero;
			footerRt.anchorMax = Vector2.one;
			footerRt.offsetMin = Vector2.zero;
			footerRt.offsetMax = Vector2.zero;

			var footerFlex = footerGo.AddComponent<FlexContainer>();
			footerFlex.Direction = FlexDirection.Row;
			footerFlex.JustifyContent = JustifyContent.SpaceBetween;
			footerFlex.Gap = isSingle ? new Vector2(12f, 12f) : new Vector2(4f, 4f);

			var fFlex = footerGo.AddComponent<FlexElement>();
			fFlex.Width = FlexSize.Percent(100f);
			fFlex.Height = FlexSize.Pixels(btnH);

			var btn1 = CreateBox(footerRt, "BtnCancel", new Color(0.4f, 0.2f, 0.2f, 1f), Vector2.zero, "Cancel", fontSize);
			var b1Fe = btn1.GetComponent<FlexElement>();
			b1Fe.Width = FlexSize.Flexible(1f);
			b1Fe.Height = FlexSize.Pixels(btnH);

			var btn2 = CreateBox(footerRt, "BtnAccept", new Color(0.2f, 0.5f, 0.3f, 1f), Vector2.zero, "OK", fontSize);
			var b2Fe = btn2.GetComponent<FlexElement>();
			b2Fe.Width = FlexSize.Flexible(1f);
			b2Fe.Height = FlexSize.Pixels(btnH);
		}

		// ====================================================================
		// VISUELLE HILFSMETHODEN (Cards, Boxes, Labels)
		// ====================================================================

		/// <summary>Erstellt eine übergeordnete Test-Karte mit Titel-Leiste und flexiblem Container.</summary>
		private GameObject CreateTestCard(RectTransform parent, string title, bool isSingle) {
			var cardGo = new GameObject($"Card_{title}", typeof(RectTransform), typeof(Image));
			var cardRt = cardGo.GetComponent<RectTransform>();
			cardRt.SetParent(parent, false);

			// Stretch vollflächig über Parent
			cardRt.anchorMin = Vector2.zero;
			cardRt.anchorMax = Vector2.one;
			cardRt.offsetMin = Vector2.zero;
			cardRt.offsetMax = Vector2.zero;

			var img = cardGo.GetComponent<Image>();
			img.color = ColorPanelBg;

			var flex = cardGo.AddComponent<FlexContainer>();
			flex.Direction = FlexDirection.Column;
			flex.Gap = isSingle ? new Vector2(12f, 12f) : new Vector2(4f, 4f);
			flex.Padding = isSingle ? new RectOffset(16, 16, 16, 16) : new RectOffset(6, 6, 6, 6);

			var flexEl = cardGo.AddComponent<FlexElement>();
			flexEl.Width = FlexSize.Flexible(1f);
			flexEl.Height = FlexSize.Flexible(1f);

			// Title Header
			float headerH = isSingle ? 48f : 26f;
			float headerFontSize = isSingle ? 22f : 12f;

			var titleGo = new GameObject("TitleBar", typeof(RectTransform), typeof(Image));
			var titleRt = titleGo.GetComponent<RectTransform>();
			titleRt.SetParent(cardRt, false);
			titleGo.GetComponent<Image>().color = ColorHeader;

			var titleFlex = titleGo.AddComponent<FlexElement>();
			titleFlex.Width = FlexSize.Percent(100f);
			titleFlex.Height = FlexSize.Pixels(headerH);

			CreateLabel(titleRt, title, headerFontSize, Color.white, TextAlignmentOptions.MidlineLeft, new Vector2(12f, 0f));

			return cardGo;
		}

		/// <summary>Erstellt eine farbige uGUI-Box mit FlexElement und zentriertem Text-Label.</summary>
		private GameObject CreateBox(RectTransform parent, string name, Color color, Vector2 size, string text, float fontSize = 11f) {
			var go = new GameObject(name, typeof(RectTransform), typeof(Image));
			var rt = go.GetComponent<RectTransform>();
			rt.SetParent(parent, false);

			var img = go.GetComponent<Image>();
			img.color = color;

			var fe = go.AddComponent<FlexElement>();
			if (size.x > 0f) {
				fe.Width = FlexSize.Pixels(size.x);
			} else {
				fe.Width = FlexSize.Flexible(1f);
			}

			if (size.y > 0f) {
				fe.Height = FlexSize.Pixels(size.y);
			} else {
				fe.Height = FlexSize.Flexible(1f);
			}

			if (string.IsNullOrEmpty(text) == false) {
				CreateLabel(rt, text, fontSize, Color.white, TextAlignmentOptions.Center, Vector2.zero);
			}

			return go;
		}

		/// <summary>Erstellt ein TextMeshProUGUI-Label mit Auto-Sizing und Ausrichtung.</summary>
		private void CreateLabel(RectTransform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment, Vector2 offset) {
			var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
			var rt = labelGo.GetComponent<RectTransform>();
			rt.SetParent(parent, false);
			rt.anchorMin = Vector2.zero;
			rt.anchorMax = Vector2.one;
			rt.offsetMin = new Vector2(4f + offset.x, 2f + offset.y);
			rt.offsetMax = new Vector2(-4f - offset.x, -2f - offset.y);

			var tmp = labelGo.GetComponent<TextMeshProUGUI>();
			tmp.text = text;
			tmp.fontSize = fontSize;
			tmp.color = color;
			tmp.alignment = alignment;
			tmp.enableWordWrapping = true;
			tmp.overflowMode = TextOverflowModes.Ellipsis;
		}

		/// <summary>Löscht ein GameObject sicher im Editor (Immediate) oder in Playmode (Destroy).</summary>
		private static void SafeDestroy(GameObject target) {
			if (target == null) return;
#if UNITY_EDITOR
			if (Application.isPlaying == false) {
				UnityEditor.Undo.DestroyObjectImmediate(target);
				return;
			}
#endif
			Destroy(target);
		}
	}
}
