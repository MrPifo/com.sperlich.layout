namespace Sperlich.UISystem {

	public enum GridTrackMode {
		/// <summary>Feste Pixelgröße.</summary>
		Pixels = 0,
		/// <summary>Anteil am freien Restplatz (CSS 'fr').</summary>
		Fraction = 1,
		/// <summary>Größe aus dem Inhalt der einzelligen Items in diesem Track (CSS 'auto').</summary>
		Auto = 2,
		/// <summary>minmax(min, max) — Min-Seite in Pixeln (0 = Content), Max-Seite als Pixel- oder fr-Wert.</summary>
		MinMax = 3,
	}

	public enum GridAutoFlow {
		/// <summary>Zeilenweise füllen: erst Spalten auffüllen, dann in die nächste Zeile. Anzahl Spalten = Items pro Zeile.</summary>
		Row = 0,
		/// <summary>Spaltenweise füllen: erst Zeilen auffüllen, dann in die nächste Spalte. Anzahl Zeilen = Items pro Spalte.</summary>
		Column = 1,
	}

	/// <summary>Von welcher Ecke aus die Items platziert werden (entspricht Unitys GridLayoutGroup 'Start Corner').</summary>
	public enum GridStartCorner {
		UpperLeft = 0,
		UpperRight = 1,
		LowerLeft = 2,
		LowerRight = 3,
	}

	public enum GridRepeatMode {
		None = 0,
		/// <summary>repeat(auto-fill, ...) — füllt so viele Tracks wie passen, auch leere.</summary>
		AutoFill = 1,
		/// <summary>repeat(auto-fit, ...) — wie AutoFill, aber leere Tracks am Ende kollabieren.</summary>
		AutoFit = 2,
	}

	public enum GridAlign {
		Start = 0,
		End = 1,
		Center = 2,
		Stretch = 3,
	}
}
