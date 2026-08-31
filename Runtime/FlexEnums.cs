namespace Sperlich.UISystem {

	public enum FlexDirection {
		Row = 0,
		RowReverse = 1,
		Column = 2,
		ColumnReverse = 3,
	}

	public enum FlexWrap {
		NoWrap = 0,
		Wrap = 1,
		WrapReverse = 2,
	}

	public enum JustifyContent {
		Start = 0,
		End = 1,
		Center = 2,
		SpaceBetween = 3,
		SpaceAround = 4,
		SpaceEvenly = 5,
	}

	public enum AlignItems {
		Start = 0,
		End = 1,
		Center = 2,
		Stretch = 3,
	}

	public enum AlignContent {
		Start = 0,
		End = 1,
		Center = 2,
		SpaceBetween = 3,
		SpaceAround = 4,
		SpaceEvenly = 5,
		Stretch = 6,
	}

	public enum AlignSelf {
		Auto = 0,
		Start = 1,
		End = 2,
		Center = 3,
		Stretch = 4,
	}

	/// <summary>
	/// Steuert die automatische Größenanpassung von Kind-Elementen innerhalb eines <see cref="FlexContainer"/>.
	/// </summary>
	public enum FlexChildSizing {
		/// <summary>Keine automatische Größenanpassung: Kinder behalten ihre eigene definierte Größe.</summary>
		None = 0,
		/// <summary>Hauptachse: Bei Row wird die gesamte Zeilenbreite (X) gleichmäßig aufgeteilt, bei Column die gesamte Spaltenhöhe (Y).</summary>
		MainAxis = 1,
		/// <summary>Kreuzachse: Bei Row wird die gesamte Zeilenhöhe (Y) gestreckt, bei Column die gesamte Spaltenbreite (X).</summary>
		CrossAxis = 2,
		/// <summary>Beide Achsen: Kinder füllen sowohl die gesamte Hauptachse als auch die Kreuzachse vollständig aus.</summary>
		Both = 3,
	}
}
