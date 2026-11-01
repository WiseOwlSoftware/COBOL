## Entry 2923 — 2026-11-01 01:13 PDT — PB4272: the backend seam, the model, the binders and the driver reference no Roslyn

Wave 20261800 C, the PB2293 campaign lane. The `ICodeGenBackend` contract was Roslyn-shaped, so a direct-IL backend
could not implement it without loading Roslyn, and PB4273's model assembly could not be built without a Roslyn package.

**Re-probe (2e48863d5).** A grep over `src/WiseOwl.Cobol.Compiler` outside `CodeGen/` found the four uses the note names
(`BackendArtifact`'s `IReadOnlyList<Microsoft.CodeAnalysis.Diagnostic>`, `CompilerDriver.cs:214`'s
`Microsoft.CodeAnalysis.DiagnosticSeverity` filter, `DataItem.cs`'s `SyntaxFacts` identifier-character calls and
`StatementBinder.cs`'s unused `using Microsoft.CodeAnalysis.CSharp`), plus one the note missed:
`CsNames.CSharpReservedKeywords`, which PB4265 had read from `SyntaxFacts.GetReservedKeywordKinds`. PB4265 had already
removed the `@keyword` spelling, so the model's only remaining Roslyn question was the identifier character classes.

**The fix.**
- The seam has its own diagnostic, `BackendDiagnostic(Code, Severity, Message, SourceLocation? Location)`. It uses the
  compiler's existing severity enum and renders through one shared diagnostic-line function, `Diagnostic.Render`, which
  the front end's `Diagnostic.ToString` now calls as well. `RoslynBackend.Neutral` translates Roslyn's diagnostics. It
  takes the mapped position from Roslyn's own formatter rule, or no position when the diagnostic has none, and drops
  hidden diagnostics. `CompilerDriver` now reads only `IsError`. The architecture edges file allows
  `compiler.codegen -> frontend.diagnostics` for this, with the reason in DESIGN-architecture-review §8.1.
- `BackendOptions.WriteSource` is deleted. No caller ever passed false, and the `.g.cs` belongs to the Roslyn backend,
  which always writes it.
- The model now states what it used to ask Roslyn. `CsNames.IsIdentifierStartCharacter` and
  `CsNames.IsIdentifierPartCharacter` hold the CLS and C# identifier classes, and `CsNames.CSharpReservedKeywords` lists
  the 81 keywords.
- The unused import in `StatementBinder.cs` is gone.
- The factory's `bindHost` parameter stays. It goes with the `CSharpEmitter` shim, which is PB2290's mechanism, and
  PB2290 is blocked by PB2103.

**Drift tests.** `RoslynFreeSeamDriftTests` checks four things. Each identifier class matches `SyntaxFacts` for all
65,536 UTF-16 code units. The neutral diagnostics produce exactly the text of Roslyn's `Diagnostic.ToString`, tested
with an in-source error and warning and an error with no position (CS5001). A token scan refuses any
`Microsoft.CodeAnalysis` use in a compiler source outside `CodeGen/` or in the seam file, and a planted self-test shows
the scan finds every form of use while ignoring mentions in comments and strings. `CsNameReservationDriftTests` now
checks that the keyword set equals Roslyn's, element by element.

**Behavior neutrality.** `scripts/arch/compare_oracle.py`: `=== ARCH-ORACLE: IDENTICAL — 15712 cases (baseline
2e48863d5802) ===`. No goldens: no user program's behavior changes, and no inventory row is involved
(`closes_rows: []`).

**Docs.** DESIGN-backend-abstraction §2.1 (the contract block, and the statement that the seam is Roslyn-free) and §2.4,
DESIGN-codegen-backend §2.2, DESIGN-external-repository's keyword-seed paragraph and DESIGN-architecture-review §8.1 now
describe the current code. PB4273 records that this landed.

