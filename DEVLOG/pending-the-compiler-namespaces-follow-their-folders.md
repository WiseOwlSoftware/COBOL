## The compiler's namespaces follow their folders; the carrier, the ALL-literal factory and the picture gate move down a layer

Wave 20262168 group F (cluster PB2119, the architecture review's R3 move/rename and extract waves). Ten notes land; the
eleventh, PB2328 (the ReferenceFormatProcessor extract), is a separate mechanism and continues on this branch.

**The root flip and the folder rule (PB2415, PB2239-PB2242, PB2244, PB2245; PB2243 resolved).** The owner's answer to
PB2118 question 1 (2026-10-07, DESIGN-architecture-review §8.2 shape A) makes every namespace root its project name, and
§8.2 rule 1 makes every namespace the root plus its folder path. Re-probe: the census at 9e65164eb measured the
disagreements the notes name (Binding/Procedure/Verbs, CodeGen/DataDivision, CodeGen/Roslyn, CodeGen/Verbs, Oo under the
root `WiseOwl.Cobol`, Frontend/Common's three `WiseOwl.Cobol.Common` files, Parsing's two `WiseOwl.Cobol.Frontend.Generated`
files) plus two the notes predate (Binding/SymbolTable and Listing, `WiseOwl.Cobol.Listing.Building`). The compiler's
`RootNamespace` is now `WiseOwl.Cobol.Compiler` and each compiler file declares root plus folder; Listing follows the same
rule (`WiseOwl.Cobol.Compiler.Listing`). Binding/SymbolTable keeps `WiseOwl.Cobol.Compiler.Binding`: a namespace named
`SymbolTable` hides the `SymbolTable` type it holds (CS0118), so the folder or the type needs a new name, a decision left
to its own note. The generated parser's `AntlrNamespace` becomes `WiseOwl.Cobol.Frontend.Parsing` (the name
DESIGN-module-topology §2.2 already gave it), so the authored superclass and partial in Parsing/ agree with their folder.
Every caller changed, `using` lines and qualified names alike (about 1,100 files, most of them one line); no alias.

**The debugging-line carrier (PB2409).** `ReferenceFormatProcessor.DebugLineCarrier` is the frontend.common type
`DebugLineCarrier` (`Text`, `StartsAt`), so the parser's `DebuggingLineRewriter` reads no preprocessor type. Its three
private position tests (two in CopyProcessor, one in TextWord) are now the one `StartsAt`. Comments that named a
`DebuggingLineDriftTests` that does not exist now name `DebuggingLineRewriterTests`, whose every case finds its line by
the `DEBUG_LINE` token the carrier lexes as.

**The ALL-literal factory (PB2419).** `BoundAllLiteral.Of(string)` and `.Of(ConcatFolder.Folded)` named `ConcatFolder`
from inside the bound tree; they are `ConcatFolder.AllLiteral(string)` and `ConcatFolder.AllLiteral(Folded)`.

**The picture shape's edition gate (PB2421).** `VersionConformancePass.PictureConstructId` is `PicInfo.ConstructId` in
compiler.model, so the report SUM-counter screen in `ReportSectionBinder` no longer reads the validation layer.
`EditionGateArmDriftTests` reads the member as part of the bound arm, whose gate it is. It is a METHOD: as a property it
joined `PicInfo`'s synthesized record text, which `StrongTypeModel`'s type-identity key compiles into the program, and
the arch oracle caught it (33 of 23,644 cases emitted a different strong-type key) before the gate did.

**The edges file and the census.** A census recorded on the code head (`a27353bac97b`) measures the new names; the edges
file is re-keyed (each pair to the namespace that declares its types now, a split row starting at its parent's counts)
and ratcheted to it: the row `Frontend.Parsing → Frontend.Preprocessor` is deleted, the pairs `BoundAllLiteral →
ConcatFolder` and `ReportSectionBinder → VersionConformancePass` are gone, the CodeGen rows split by folder, and the
open removers' **Removes** lines name the re-keyed rows. ArchitectureEdgesDriftTests and ArchCensusDriftTests pass (11);
`work.py check` passes.

**Neutrality.** `python scripts/arch/compare_oracle.py` against the base 92fddd91d3e9: `=== ARCH-ORACLE: IDENTICAL — 23644
cases (baseline 92fddd91d3e9, new 90b4b81168ea) ===`. `python scripts/semgrep/verify.py`: PASS, no rule's count grew.

**Self-review.** Architecture: each move goes down a layer the edges file allows; no new cross-layer pair (the ratchet
admits none). Code: the moved members keep their bodies verbatim. Duplication: the carrier test now has one body.
Performance: none of it is on a hot path.
