# Sperlich UISystem Layout

Flexbox- and CSS-Grid-style layout for uGUI.

- **FlexContainer / FlexElement** – row/column flow, wrap, justify / align, gap,
  per-element sizing: `px`, `weight`, `ratio`, `auto`, `ignore`, plus min/max and grow/shrink.
- **GridContainer** – track lists (`px` / `fr` / `auto`), row/column gap, cell placement.
- **LayoutTweenExtensions** – animate derived layout values (FlexSize numbers, padding)
  smoothly via PrimeTween `Tween.Custom`.
- Custom UIToolkit inspectors for all three components.

See `LAYOUT_MIGRATION.md` for the history / migration notes.

## Dependencies

| Dependency | Why | Note |
|---|---|---|
| `com.sperlich.editorkit` | inspector theme + widgets | git dependency, editor only |
| `com.kyrylokuzyk.primetween` | animated layout values | also on the Asset Store – install that instead if the git URL fails |
| `com.unity.ugui` 2.0.0 | uGUI / TextMeshPro | |

> Unity does not resolve git-URL dependencies transitively – add
> `com.sperlich.editorkit` and PrimeTween to the consuming project yourself.

The runtime assembly no longer references Rewired or UniTask (those refs were unused
and were removed during the split).

## Installation

Unity > Window > Package Manager > + > Add package from git URL:

```
https://github.com/MrPifo/com.sperlich.uisystem.layout.git
```
