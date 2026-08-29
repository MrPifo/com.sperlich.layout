# Sperlich Layout

Flexbox- and CSS-Grid-style layout for uGUI.

- **FlexContainer / FlexElement** – row/column flow, wrap, justify / align, gap,
  per-element sizing: `px`, `weight`, `ratio`, `auto`, `ignore`, plus min/max and grow/shrink.
- **GridContainer** – track lists (`px` / `fr` / `auto`), row/column gap, cell placement.
- Custom UIToolkit inspectors for all three components.

Layout changes snap immediately. There is no tween / animation layer and no PrimeTween
dependency – animate the container's `Padding` / `Gap` (or an element's `Width` / `Height`)
from your own code and call `RequestRebuild()` if you want soft transitions.

See `LAYOUT_MIGRATION.md` for the history / migration notes.

## Dependencies

| Dependency | Why | Note |
|---|---|---|
| `com.sperlich.editorkit` | inspector theme + widgets | git dependency, editor only |
| `com.unity.ugui` 2.0.0 | uGUI / TextMeshPro | |

> Unity does not resolve git-URL dependencies transitively – add
> `com.sperlich.editorkit` to the consuming project yourself.

The runtime assembly does not reference Rewired, UniTask or PrimeTween (Rewired / UniTask
refs were unused and removed during the split; the PrimeTween animation layer was removed
on request).

## Installation

Unity > Window > Package Manager > + > Add package from git URL:

```
https://github.com/MrPifo/com.sperlich.layout.git
```
