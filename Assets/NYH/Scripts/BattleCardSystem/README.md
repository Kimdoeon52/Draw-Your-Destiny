# BattleCardSystem Structure

`BattleCardSystem` is organized by feature flow first, and by layer second.

## Folder Map

- `Runtime/Session`: battle lifecycle, mode switching, turn flow
- `Runtime/Cards`: battle card pile, costs, rewards, runtime card system
- `Runtime/Deck`: persistent deck setup, mulligan results, replacement selection
- `Runtime/Board`: board state, coordinates, move/attack queries, targeting queries
- `Runtime/Units`: unit spawning helpers
- `Runtime/Actions`: game actions, performers, action factory/validation
- `Presentation/UI`: scene-facing controllers and hand/HUD/mulligan UI
- `Presentation/Targeting`: battle card targeting flow and targeting state
- `Presentation/Preview`: board preview, highlight, and targeting preview rendering
- `Presentation/DeckReplacement`: deck replacement UI flow and preview helpers
- `Domain/Data`: battle card data/catalog assets
- `Domain/Effects`: battle effect definitions and resolvers
- `Domain/Models`: runtime battle card/unit models
- `AI`: battle AI components

## Boundary Notes

- `CoreCardSystem` integration remains at the battle entry points such as `BattleCardSystem`, `BattleUIController`, `BattleSessionController`, and `BattleCardViewAdapter`.
- `AI` stays as its own top-level area and should depend on runtime session/unit services rather than presentation code.
- Namespace is intentionally still `NYH.BattleCardSystem` for this reorganization pass.
