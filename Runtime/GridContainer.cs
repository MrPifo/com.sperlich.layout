using System.Collections.Generic;
using UnityEngine;

namespace Sperlich.UISystem {

	/// <summary>
	/// CSS-Grid-artiger Layout-Container. Spalten/Zeilen als typisierte <see cref="GridTrack"/>-Listen
	/// (Pixels / fr / auto / minmax), optional responsive über repeat(auto-fill|auto-fit). Item-Platzierung
	/// per auto-flow (zeilen- oder spaltenweise), Start-Ecke wählbar, überschreibbar über die Grid-Felder
	/// auf <see cref="FlexElement"/> (Span / Startzelle).
	/// </summary>
	[AddComponentMenu("Sperlich UI/Grid Container")]
	[ExecuteAlways]
	[DisallowMultipleComponent]
	[RequireComponent(typeof(RectTransform))]
	public class GridContainer : LayoutContainerBase {

		[SerializeField] private List<GridTrack> columns = new List<GridTrack> { GridTrack.Fr(1f), GridTrack.Fr(1f) };
		[SerializeField] private List<GridTrack> rows = new List<GridTrack>();
		[SerializeField, Tooltip("Row = zeilenweise füllen (Spaltenzahl = Items pro Zeile). Column = spaltenweise füllen (Zeilenzahl = Items pro Spalte).")]
		private GridAutoFlow autoFlow = GridAutoFlow.Row;
		[SerializeField, Tooltip("Von welcher Ecke aus die Items platziert werden.")]
		private GridStartCorner startCorner = GridStartCorner.UpperLeft;
		[SerializeField] private GridRepeatMode columnRepeat = GridRepeatMode.None;
		[SerializeField] private GridTrack columnRepeatTemplate = GridTrack.Fr(1f);
		[SerializeField, Min(1f)] private float columnRepeatMinSize = 120f;
		[SerializeField, Tooltip("Track-Vorlage für automatisch erzeugte Zeilen (bei Row-Flow, wenn mehr Zeilen gebraucht werden als definiert).")]
		private GridTrack implicitRowTemplate = GridTrack.Auto();
		[SerializeField, Tooltip("Track-Vorlage für automatisch erzeugte Spalten (bei Column-Flow, wenn mehr Spalten gebraucht werden als definiert).")]
		private GridTrack implicitColumnTemplate = GridTrack.Auto();
		[SerializeField, Tooltip("Ausrichtung des Item-Inhalts innerhalb seiner Zelle auf der X-Achse.")]
		private GridAlign justifyItems = GridAlign.Stretch;
		[SerializeField, Tooltip("Ausrichtung des Item-Inhalts innerhalb seiner Zelle auf der Y-Achse.")]
		private GridAlign alignItems = GridAlign.Stretch;

		public List<GridTrack> Columns => columns;
		public List<GridTrack> Rows => rows;
		public GridAutoFlow AutoFlow { get => autoFlow; set { autoFlow = value; SetDirty(); } }
		public GridStartCorner StartCorner { get => startCorner; set { startCorner = value; SetDirty(); } }
		public GridRepeatMode ColumnRepeat { get => columnRepeat; set { columnRepeat = value; SetDirty(); } }
		public GridAlign JustifyItems { get => justifyItems; set { justifyItems = value; SetDirty(); } }
		public GridAlign AlignItems { get => alignItems; set { alignItems = value; SetDirty(); } }

		private bool ColumnFlow => autoFlow == GridAutoFlow.Column;

		private struct Placed {
			public RectTransform rect;
			public int col, row, colSpan, rowSpan;
		}

		private readonly List<GridTrack> effectiveColumns = new List<GridTrack>();
		private readonly List<GridTrack> effectiveRows = new List<GridTrack>();
		private readonly List<Placed> placed = new List<Placed>();
		private readonly HashSet<long> occupied = new HashSet<long>();

		private static long CellKey(int col, int row) => ((long)row << 32) | (uint)col;

		// --- Track-Aufbau ---------------------------------------------------

		/// <summary>Feste Spaltenliste (aus <c>columns</c> bzw. repeat). Primärachse bei Row-Flow.</summary>
		private void BuildFixedColumns() {
			effectiveColumns.Clear();
			if (ColumnFlow == false && columnRepeat != GridRepeatMode.None) {
				int count = GridLayoutMath.ResolveRepeatCount(InnerWidth, gap.x, columnRepeatMinSize);
				for (int i = 0; i < count; i++) {
					effectiveColumns.Add(columnRepeatTemplate);
				}
				return;
			}
			if (columns.Count == 0) {
				effectiveColumns.Add(GridTrack.Fr(1f));
				return;
			}
			effectiveColumns.AddRange(columns);
		}

		/// <summary>Feste Zeilenliste (aus <c>rows</c>, mindestens eine). Primärachse bei Column-Flow.</summary>
		private void BuildFixedRows() {
			effectiveRows.Clear();
			if (rows.Count == 0) {
				effectiveRows.Add(implicitRowTemplate);
				return;
			}
			effectiveRows.AddRange(rows);
		}

		/// <summary>Wachsende Zeilen (Sekundärachse bei Row-Flow): definierte Zeilen, danach <c>implicitRowTemplate</c>.</summary>
		private void BuildGrowingRows(int neededRows) {
			effectiveRows.Clear();
			int count = Mathf.Max(1, neededRows);
			for (int r = 0; r < count; r++) {
				effectiveRows.Add(r < rows.Count ? rows[r] : implicitRowTemplate);
			}
		}

		/// <summary>Wachsende Spalten (Sekundärachse bei Column-Flow): definierte Spalten, danach <c>implicitColumnTemplate</c>.</summary>
		private void BuildGrowingColumns(int neededColumns) {
			effectiveColumns.Clear();
			int count = Mathf.Max(1, neededColumns);
			for (int c = 0; c < count; c++) {
				effectiveColumns.Add(c < columns.Count ? columns[c] : implicitColumnTemplate);
			}
		}

		// --- Platzierung --------------------------------------------------

		private void PlaceItems(int primaryCap) {
			placed.Clear();
			occupied.Clear();
			bool columnFlow = ColumnFlow;

			int cursorPrimary = 0;
			int cursorSecondary = 0;

			for (int i = 0; i < children.Count; i++) {
				RectTransform child = children[i];
				FlexElement fe = FlexOf(child);

				int colSpan = Mathf.Max(1, fe != null ? fe.ColumnSpan : 1);
				int rowSpan = Mathf.Max(1, fe != null ? fe.RowSpan : 1);
				int explicitCol = fe != null ? fe.ColumnStart : 0;
				int explicitRow = fe != null ? fe.RowStart : 0;

				int primarySpan = Mathf.Clamp(columnFlow ? rowSpan : colSpan, 1, primaryCap);
				int secondarySpan = columnFlow ? colSpan : rowSpan;
				if (columnFlow) {
					rowSpan = primarySpan;
				} else {
					colSpan = primarySpan;
				}

				int placeCol;
				int placeRow;

				if (explicitCol > 0 && explicitRow > 0) {
					placeCol = Mathf.Max(0, explicitCol - 1);
					placeRow = Mathf.Max(0, explicitRow - 1);
				} else {
					FindFreeCell(cursorPrimary, cursorSecondary, primarySpan, secondarySpan, primaryCap, columnFlow, out placeCol, out placeRow);
					int landedPrimary = columnFlow ? placeRow : placeCol;
					int landedSecondary = columnFlow ? placeCol : placeRow;
					cursorPrimary = landedPrimary + primarySpan;
					cursorSecondary = landedSecondary;
					if (cursorPrimary >= primaryCap) {
						cursorPrimary = 0;
						cursorSecondary++;
					}
				}

				MarkOccupied(placeCol, placeRow, colSpan, rowSpan);
				placed.Add(new Placed { rect = child, col = placeCol, row = placeRow, colSpan = colSpan, rowSpan = rowSpan });
			}
		}

		private void FindFreeCell(int startPrimary, int startSecondary, int primarySpan, int secondarySpan, int primaryCap, bool columnFlow, out int col, out int row) {
			int p = startPrimary;
			int s = startSecondary;
			while (true) {
				if (p + primarySpan > primaryCap) {
					p = 0;
					s++;
					continue;
				}
				int cc = columnFlow ? s : p;
				int rr = columnFlow ? p : s;
				int csp = columnFlow ? secondarySpan : primarySpan;
				int rsp = columnFlow ? primarySpan : secondarySpan;
				if (IsBlockFree(cc, rr, csp, rsp)) {
					col = cc;
					row = rr;
					return;
				}
				p++;
			}
		}

		private bool IsBlockFree(int col, int row, int colSpan, int rowSpan) {
			for (int rr = row; rr < row + rowSpan; rr++) {
				for (int cc = col; cc < col + colSpan; cc++) {
					if (occupied.Contains(CellKey(cc, rr))) {
						return false;
					}
				}
			}
			return true;
		}

		private void MarkOccupied(int col, int row, int colSpan, int rowSpan) {
			for (int rr = row; rr < row + rowSpan; rr++) {
				for (int cc = col; cc < col + colSpan; cc++) {
					occupied.Add(CellKey(cc, rr));
				}
			}
		}

		// --- Auto-Content-Size -------------------------------------------

		private float[] AutoSizes(int axis, IReadOnlyList<GridTrack> tracks) {
			float[] sizes = new float[tracks.Count];
			for (int i = 0; i < placed.Count; i++) {
				Placed p = placed[i];
				int index = axis == 0 ? p.col : p.row;
				int span = axis == 0 ? p.colSpan : p.rowSpan;
				if (span != 1 || index < 0 || index >= sizes.Length) {
					continue;
				}
				sizes[index] = Mathf.Max(sizes[index], PreferredOf(p.rect, axis));
			}
			return sizes;
		}

		protected override void CalculateContentSize(int axis) {
			if (ColumnFlow) {
				BuildFixedRows();
				if (axis == 1) {
					float h = Padding.vertical + gap.y * Mathf.Max(0, effectiveRows.Count - 1);
					for (int i = 0; i < effectiveRows.Count; i++) {
						h += EstimateTrack(effectiveRows[i]);
					}
					m_MinHeight = h;
					m_PreferredHeight = h;
				} else {
					int definedCols = Mathf.Max(columns.Count, 1);
					float w = Padding.horizontal + gap.x * Mathf.Max(0, definedCols - 1);
					for (int i = 0; i < definedCols; i++) {
						w += EstimateTrack(i < columns.Count ? columns[i] : implicitColumnTemplate);
					}
					m_MinWidth = w;
					m_PreferredWidth = w;
				}
				return;
			}

			BuildFixedColumns();
			if (axis == 0) {
				float w = Padding.horizontal + gap.x * Mathf.Max(0, effectiveColumns.Count - 1);
				for (int i = 0; i < effectiveColumns.Count; i++) {
					w += EstimateTrack(effectiveColumns[i]);
				}
				m_MinWidth = w;
				m_PreferredWidth = w;
			} else {
				int definedRows = Mathf.Max(rows.Count, 1);
				float h = Padding.vertical + gap.y * Mathf.Max(0, definedRows - 1);
				for (int i = 0; i < definedRows; i++) {
					h += EstimateTrack(i < rows.Count ? rows[i] : implicitRowTemplate);
				}
				m_MinHeight = h;
				m_PreferredHeight = h;
			}
		}

		private static float EstimateTrack(GridTrack t) {
			switch (t.mode) {
				case GridTrackMode.Pixels: return Mathf.Max(0f, t.value);
				case GridTrackMode.MinMax: return Mathf.Max(0f, t.minPx);
				default: return 0f;
			}
		}

		// --- Arrange ---------------------------------------------------------

		protected override void Arrange(int applyAxis) {
			if (children.Count == 0) {
				return;
			}

			if (ColumnFlow) {
				BuildFixedRows();
				PlaceItems(effectiveRows.Count);
				int neededCols = 1;
				for (int i = 0; i < placed.Count; i++) {
					neededCols = Mathf.Max(neededCols, placed[i].col + placed[i].colSpan);
				}
				BuildGrowingColumns(neededCols);
			} else {
				BuildFixedColumns();
				PlaceItems(effectiveColumns.Count);
				int neededRows = 1;
				for (int i = 0; i < placed.Count; i++) {
					neededRows = Mathf.Max(neededRows, placed[i].row + placed[i].rowSpan);
				}
				BuildGrowingRows(neededRows);
			}

			int colCount = effectiveColumns.Count;
			int rowCount = effectiveRows.Count;
			ApplyStartCorner(colCount, rowCount);

			float[] colSizes = new float[colCount];
			float[] rowSizes = new float[rowCount];
			GridLayoutMath.ResolveTrackSizes(effectiveColumns, InnerWidth, gap.x, AutoSizes(0, effectiveColumns), colSizes);
			GridLayoutMath.ResolveTrackSizes(effectiveRows, InnerHeight, gap.y, AutoSizes(1, effectiveRows), rowSizes);

			for (int i = 0; i < placed.Count; i++) {
				Placed p = placed[i];
				if (p.col < 0 || p.row < 0 || p.col >= colSizes.Length || p.row >= rowSizes.Length) {
					continue;
				}

				float cellX = GridLayoutMath.TrackOffset(colSizes, p.col, gap.x, Padding.left);
				float cellY = GridLayoutMath.TrackOffset(rowSizes, p.row, gap.y, Padding.top);
				float cellW = GridLayoutMath.TrackSpan(colSizes, p.col, p.colSpan, gap.x);
				float cellH = GridLayoutMath.TrackSpan(rowSizes, p.row, p.rowSpan, gap.y);

				FlexElement fe = FlexOf(p.rect);
				float itemW = ResolveItemSize(p.rect, fe, 0, cellW, justifyItems);
				float itemH = ResolveItemSize(p.rect, fe, 1, cellH, alignItems);

				float x = cellX + GridLayoutMath.AlignInCell(justifyItems, cellW, itemW);
				float y = cellY + GridLayoutMath.AlignInCell(alignItems, cellH, itemH);

				if (applyAxis == 0) {
					WriteChildAxis(p.rect, 0, x, itemW);
				} else {
					WriteChildAxis(p.rect, 1, y, itemH);
				}
			}
		}

		/// <summary>Spiegelt die logischen (oben-links) Zellkoordinaten auf die gewählte Start-Ecke.</summary>
		private void ApplyStartCorner(int colCount, int rowCount) {
			bool mirrorX = startCorner == GridStartCorner.UpperRight || startCorner == GridStartCorner.LowerRight;
			bool mirrorY = startCorner == GridStartCorner.LowerLeft || startCorner == GridStartCorner.LowerRight;
			if (mirrorX == false && mirrorY == false) {
				return;
			}
			for (int i = 0; i < placed.Count; i++) {
				Placed p = placed[i];
				if (mirrorX) {
					p.col = Mathf.Max(0, colCount - p.colSpan - p.col);
				}
				if (mirrorY) {
					p.row = Mathf.Max(0, rowCount - p.rowSpan - p.row);
				}
				placed[i] = p;
			}
		}

		private static float ResolveItemSize(RectTransform child, FlexElement fe, int axis, float cellSize, GridAlign align) {
			if (fe != null) {
				FlexSize fs = axis == 0 ? fe.Width : fe.Height;
				if (fs.mode != FlexMode.Ignore && fs.mode != FlexMode.Flexible) {
					float resolved = axis == 0 ? fe.preferredWidth : fe.preferredHeight;
					if (resolved >= 0f) {
						return Mathf.Min(resolved, cellSize);
					}
				}
			}
			if (align == GridAlign.Stretch) {
				return cellSize;
			}
			return Mathf.Min(cellSize, Mathf.Max(0f, PreferredOf(child, axis)));
		}
	}
}
