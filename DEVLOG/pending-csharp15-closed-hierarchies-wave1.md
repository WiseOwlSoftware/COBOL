## C# 15 `closed` hierarchies, wave 1: the bound tree and the binding model's Place and AccessPath families are closed

**Owner, 2026-11-24:** "I approve closed". The C# 15 evaluation and the check the owner asked for are in kb/Work PB6168.

**What changed.**
- `closed` replaces `abstract` on 20 records in `Binding/Bound/` and `Binding/Model/Place.cs` and `AccessPath.cs`: the
  eight bound roots, `ReceiverBracketKey`, `InitializeAction`, `TableSortStorage`, `ValidateStep`, `Place`,
  `PlaceDecorator`, `WindowCoding`, `CellWindowCoding`, `AllCount`, `AccessSegment`, `RootSegment` and `ContainerAnchor`.
  A switch over these hierarchies that names every case now needs no default arm, and a missing case is a compile error
  (CS8509 under `TreatWarningsAsErrors`).
- The repository tools that parse C# declarations learned the keyword: `scripts/spec/code_location.py`,
  `scripts/spec/drift_rules.py` (three patterns), `scripts/arch/source_units.py`, `scripts/prune_worktrees.py` (two
  patterns) and `scripts/gen-vault-reference.ps1`. Without it, the spec-traceability resolver lost every `closed` type
  (`EveryCSharpFilesSymbols_AgreeWithThePythonResolver` failed in the pre-landing check).
- DESIGN-architecture-review §8.5 and §5.5 name `closed` as the mechanism. The union-type plan is withdrawn, and unions
  stay a candidate only for a sum with no common base type.

**Behavior-neutral, measured before the change** (main 9e65164eb, throwaway worktree):
- The generator output was byte-identical, 95 of 95 files.
- The build had 0 warnings.
- `WiseOwl.Cobol.Tests.Unit` passed 43,099 of 43,099.
- `IsAbstract` holds in Roslyn 5.12 and in metadata.

**Filed:**
- PB6169, wave 2: remove the now-provable `throw` arms, then close the remaining hierarchies with judgment for the
  runtime ABI.
- PB6170: the C# modifier vocabulary is written seven times across five tools.
