# Sperlich UI Layout — Migrationsleitfaden

`FlexContainer` und `GridContainer` ersetzen Unitys `HorizontalLayoutGroup`,
`VerticalLayoutGroup`, `GridLayoutGroup` **und** `ContentSizeFitter`. Sie erben
nicht von diesen Komponenten, implementieren aber dieselben generischen Interfaces
(`ILayoutGroup` / `ILayoutElement`), laufen also im normalen `LayoutRebuilder`-Zyklus.

Diese Umbauten macht der User manuell im Editor (Projektregel: Claude editiert keine
`.prefab`/`.unity`). Unten steht, welche alte Einstellung auf welches neue Feld abbildet.

---

## HorizontalLayoutGroup / VerticalLayoutGroup → `FlexContainer`

| Alt (Unity)                                   | Neu (`FlexContainer`)                                   |
|-----------------------------------------------|--------------------------------------------------------|
| `HorizontalLayoutGroup`                       | `direction = Row`                                       |
| `VerticalLayoutGroup`                         | `direction = Column`                                    |
| (rückwärts anordnen)                          | `direction = RowReverse` / `ColumnReverse`             |
| `Spacing`                                     | `gap.x` (Row) bzw. `gap.y` (Column)                     |
| `Padding`                                     | `padding` (gleiches `RectOffset`)                       |
| `Child Alignment` (z.B. Upper Center)         | Hauptachse → `justifyContent`, Kreuzachse → `alignItems`|
| `Control Child Size / Width`                  | Kind bekommt `FlexElement` mit `Width = Pixels/Percent/Auto` |
| `Use Child Scale`                             | nicht unterstützt (Layout ignoriert Scale wie CSS)     |
| `Child Force Expand / Width`                  | Kind: `FlexElement.Width.grow = 1` (oder `Flexible`)   |
| `ContentSizeFitter` (Preferred Size)          | entfällt — `FlexContainer` meldet selbst `preferredWidth/Height` |
| `ContentSizeFitter` nur auf einer Achse       | eigenes `FlexElement` auf dem Container mit fixer Größe auf der anderen Achse |

### Kind-Größen (`FlexElement`)

* Ohne `FlexElement` → Kind wird mit seiner *preferred size* platziert (`grow 0`, `shrink 1`).
* `FlexMode.Pixels` / `Percent` / `Auto` / `Aspect` → feste bzw. inhaltsabhängige Basisgröße.
* `FlexMode.Flexible` (Legacy) *oder* `grow` > 0 im Advanced-Bereich → wächst in den freien Platz.
* `shrink` (default 1) → schrumpft bei Platzmangel; `0` = nie schrumpfen.
* `min` / `max` → Clamp in px oder %.
* `alignSelf` → überschreibt `alignItems` des Containers für dieses eine Kind.
* `order` → visuelle Reihenfolge unabhängig von der Hierarchie.

### Wrap

`wrap = Wrap` aktiviert Zeilenumbruch (wie `flex-wrap`). Erst dann wirkt
`alignContent` (Verteilung mehrerer Zeilen im Kreuzraum).

---

## GridLayoutGroup → `GridContainer`

`GridLayoutGroup` wird im Projekt produktiv nirgends genutzt (nur Testszenen), daher
ist das hier vor allem für Neuaufbauten gedacht.

| Alt (Unity `GridLayoutGroup`)                 | Neu (`GridContainer`)                                   |
|-----------------------------------------------|--------------------------------------------------------|
| `Cell Size` (fix)                             | `columns` = `GridTrack.Pixels(w)` je Spalte, `rows`/`implicitRowTemplate` = `Pixels(h)` |
| `Spacing`                                     | `gap`                                                   |
| `Padding`                                     | `padding`                                               |
| `Constraint = Fixed Column Count`             | `autoFlow = Row` + feste Anzahl `columns` (= Items pro Zeile) |
| `Constraint = Fixed Row Count`                | `autoFlow = Column` + feste Anzahl `rows` (= Items pro Spalte) |
| `Constraint = Flexible`                       | `columnRepeat = AutoFill/AutoFit` + `columnRepeatMinSize` (≈ `repeat(auto-fill, minmax(min, 1fr))`) |
| `Start Axis` (Horizontal / Vertical)          | `autoFlow` (Row / Column)                               |
| `Start Corner` (Upper/Lower · Left/Right)     | `startCorner` (UpperLeft / UpperRight / LowerLeft / LowerRight) |
| `Child Alignment`                             | `justifyItems` (X in der Zelle) / `alignItems` (Y in der Zelle) |
| responsive Spaltenzahl von Hand               | `columnRepeat` erledigt das automatisch anhand der Breite |

**Auto-Flow:**
* `autoFlow = Row` — zeilenweise; `columns`/`columnRepeat` = feste Spaltenzahl, Zeilen wachsen nach (`implicitRowTemplate`).
* `autoFlow = Column` — spaltenweise; `rows` = feste Zeilenzahl (mind. 1), Spalten wachsen nach (`implicitColumnTemplate`). `columnRepeat` wird ignoriert.
* `startCorner` spiegelt nur die Anzeige-Reihenfolge; Track-Definitionen bleiben in Oben-Links-Logik.

### Track-Typen (`GridTrack`)

* `Pixels(px)` — feste Größe.
* `Fr(weight)` — Anteil am Restplatz (CSS `fr`).
* `Auto()` — Größe aus dem Inhalt der einzelligen Items im Track.
* `MinMax(minPx, maxFr)` — `minmax(minPx, maxFr fr)`.

### Item-Platzierung

* Standard: automatisch, zeilenweise, nächste freie Zelle.
* `FlexElement.columnSpan` / `rowSpan` — Zelle über mehrere Tracks.
* `FlexElement.columnStart` / `rowStart` (1-basiert, 0 = auto) — explizite Startzelle.
* Ein Kind braucht **kein** eigenes Grid-Component — dieselbe `FlexElement`-Komponente
  trägt Flex- *und* Grid-Angaben.

---

## Animation

Entfernt. Das Package hatte einen optionalen PrimeTween-Animations-Layer
(`animateChanges` am Container, `LayoutTweenExtensions`, `IAnimatableLayout`).
Der wurde ausgebaut, damit das Package **keine** PrimeTween-Abhängigkeit mehr hat.

Layout-Änderungen snappen jetzt sofort. Weiche Übergänge macht der Aufrufer selbst:
`Padding` / `Gap` am Container (oder `Width` / `Height` am `FlexElement`) von aussen
tweenen und `RequestRebuild()` aufrufen.

---

## Bewusste v1-Vereinfachungen

* Grow/Shrink: Single-Pass mit Min/Max-Clamp statt iterativer CSS-Freeze-Konvergenz.
  Bei mehreren gleichzeitig geclampten Nachbarn kann die Verteilung minimal abweichen.
* Grid: fr-Verteilung berücksichtigt Floors erst *nach* der Aufteilung — ein sehr großer
  `minmax()`-Floor kann die Zeile überlaufen lassen.
* Grid: spannende Items vergrößern `Auto`-Tracks nicht.
* Keine string-basierte `grid-template`-Syntax — Tracks werden typisiert im Inspector gepflegt.

---

## Verifikation

* **EditMode-Tests**: `Window > General > Test Runner > EditMode` → `Sperlich.UISystem.Layout.Tests`
  (deckt Grow/Shrink, Line-Wrapping, Grid-Track-Sizing ab).
* **Manuell**: kleine Scratch-Szene mit 3 Buttons unter einem `FlexContainer`,
  gegen Maus / Keyboard / Controller und mehrere Canvas-Auflösungen prüfen
  (Controller-First-Anforderung des UI-Systems bleibt bestehen).
