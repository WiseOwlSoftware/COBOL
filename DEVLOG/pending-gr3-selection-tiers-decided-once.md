## The USE and PERFORM exception selection tiers are decided once and rendered once

kb/Work PB2723 (wave 20262139, group E, the PB1754 R2 lane). §2023:14.9.28.4 GR17 makes a Format-3 PERFORM's WHEN match
the USE rule: "The rules for determining a match are specified in General rules 3a to 3g of the USE statement". The code
generator wrote the §2023:14.9.49.4 GR3 tier tests twice. `EcEmitter.EmitDispatchSelector` (the USE Format-3 selector
`__EcDispatch`) used five `Tier(...)` lambdas with private level predicates, and `ControlFlowEmitter.ClassifyOperand` (the
WHEN matcher) used a level switch whose comment said it mirrored the first "so the two never drift". Nothing tested that,
and the two copies already disagreed on a name the catalog does not resolve. The open-mode keyword was mapped to a
string by the PERFORM binder and back to the `FileOpenMode` ordinal by `ControlFlowEmitter.ModeOrdinal`, while the USE
binder mapped it to the ordinal directly. The two operand lists were also two types: `BoundWhenOperand` and a
`(string, FileModel?)` tuple.

Re-probe at 239834a8bb: all three copies were present as the note describes. The edge disagreement could not be
reached, because both binders admit a name only through `EcNameResolution.TryResolve`.

The fix:
- `BoundExceptionSelector` is now the one operand type for both lists. It carries an `ExceptionSelectionTier` (GR3 a to
  g, in order), which the binder decides once from the catalog level through `ForName` or `ForFile`. A level-1 name
  paired with a file fails loudly.
- `ExceptionOpenMode.IndexOf` is the one keyword-to-ordinal mapping. `BoundExceptionMatch.OpenMode` (a string) became
  `ModeIndex`.
- `ExceptionSelectionEmitter` is the one renderer of each tier's C# test, the open-mode test and the banners.
  `EmitDispatchSelector` iterates the tiers, and `EmitMatcherArms` sorts by the bound tier.
- `ClassifyOperand`, `ModeOrdinal` and `BoundWhenOperand` are deleted.
- `ExceptionSelectionTierDriftTests` pins the single renderer and the deleted names. The backend contract rows were
  renamed, and `docs/CONDITIONS_EXCEPTIONS_DESIGN.md` was corrected: it had placed the bare-file tier after file+L3.

The emitted C# is unchanged. The existing goldens (`pb367b_use_selection_tiers`,
`pb5825_when_exception_open_mode_opened` and the `14.9.28-PERFORM` F3 cases) cover the behaviour, so no golden was
added. No inventory row changed.

PB2723 stays open for four parts that are separate mechanisms: the grammar rule pairs, the three "nothing
selected" encodings, the GOBACK exception cost and the Tier-A REDEFINES alias rule. See
`kb/Work/PB2723/2026-11-22-gr3-selector-unified.md`.
