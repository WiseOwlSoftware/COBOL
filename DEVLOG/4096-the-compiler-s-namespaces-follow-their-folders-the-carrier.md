## Entry 4096 — 2026-11-28 01:46 PST — The compiler's namespaces follow their folders; the carrier, the ALL-literal factory and the picture gate move down a layer; the reference-format conversion is four types

Wave 20262168 group F, finished by wave 20262182 group A (cluster PB2119, the architecture review's R3 move/rename and
extract waves). Twelve notes land: the renames and the three moves, then PB2328, the ReferenceFormatProcessor extract.

**The replay.** Group F's nine commits were cut from 92fddd91d3; main had moved 289 commits on. They were cherry-picked onto
66dad0cfd6. Twenty-seven files conflicted, all on `using` blocks and qualified names. Each took main's side, and the same
rename was applied again by script: namespace = root + folder (Binding/SymbolTable keeps `Binding`), the prefix list,
the child-namespace qualification, then a missing-`using` loop until the build was 0/0. That also covered the files main
added since (BitImageCodec, ImageInitializer, NumericDisplayForm in Binding/Model). Main's edges file was re-keyed by the
same script. Group F's census measured its own tree, so it was dropped, and a census of this head was recorded and the
file ratcheted to it. Four leftovers of the old names in main's newer text were fixed: a pinned `BoundAllLiteral.Of`
entry, and `ReferenceFormatProcessor.DebugLineCarrier` in the lexer and three docs.

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

**The edges file and the census.** A census recorded on the code head (`bf3660ef65bc`) measures the new names; the edges
file is re-keyed (each pair to the namespace that declares its types now, a split row starting at its parent's counts)
and ratcheted to it: the row `Frontend.Parsing → Frontend.Preprocessor` is deleted, the pairs `BoundAllLiteral →
ConcatFolder` and `ReportSectionBinder → VersionConformancePass` are gone, the CodeGen rows split by folder, and the
open removers' **Removes** lines name the re-keyed rows. ArchitectureEdgesDriftTests and ArchCensusDriftTests pass (11);
`work.py check` passes.

**PB2328, the extract.** The census measured `ReferenceFormatProcessor` at 1,115 lines and 35 members in four partials,
with seven nested types. It is now four types, as DESIGN-architecture-review §8.3 names them. `LogicalLineConverter` is
the static entry: the `NormalizeToFreeForm` family (the names are kept, because the mapped overload already is the seam),
the format segments, the carried comment-entry candidate's conversion and the comment-entry paragraph helpers.
`FixedFormConverter` and `FreeFormConverter` are the two classifiers, one file each. `LogicalLineBuilder` is the one state
owner both feed, and its private members now include the literal state and the program-text scan. `TextSite` and
`CommentEntryText` are their own files. Bodies moved verbatim. Members another type now calls became internal and are
qualified at the call (`LogicalLineConverter.WordEnd`, `LogicalLineBuilder.FirstNonSpace`). Every caller changed, and no
alias remains. The two drift tests that pinned the old file names now name the new files, and the walker test lists its
four files explicitly instead of a glob that would match nothing after a rename. `LogicalLineConversionBoundaryDriftTests`
is new: it pins one entry (only the converter makes a classifier), one state owner (only the classifiers and the
candidate conversion make a builder), one scan, and no `ReferenceFormatProcessor`. The moved comments carried nine
`§2023:6.5 N)` designators that the citation audit kept as baseline debt. They now read `§2023:6.5 GRn`, each checked
with cite.py, and the baseline is nine entries shorter. 77 inventory witnesses were re-pointed by record_verdicts batches
(39 at 2023, 11 at 2002, 27 at 1985): rows changed 39/11/27, GAP 1 -> 1, 1130 -> 1130, 0 -> 0. Current-state docs follow
the rename, and the six open notes that name the old files have an addendum giving the new sites.

**Neutrality.** `python scripts/arch/compare_oracle.py --base 66dad0cfd6` on the replayed head and again after the
extract: `=== ARCH-ORACLE: IDENTICAL — 23742 cases (baseline 66dad0cfd670, new abb42aa2ddc8) ===` and `... new
abb42aa2ddc8-dirty) ===`. `python scripts/semgrep/verify.py`: PASS, no rule's count grew.

**Self-review.** Architecture: each move goes down a layer the edges file allows; no new cross-layer pair (the ratchet
admits none). Code: the moved members keep their bodies verbatim. Duplication: the carrier test now has one body.
Performance: none of it is on a hot path.
The extract's self-review also checked that no forwarder or alias of `ReferenceFormatProcessor` remains (the new drift
test asserts it) and that the moved scan and literal state have one home (the builder).

**Gate.** Run 000002 was RED before any test ran: the new drift test is a tree-wide sweep and lacked its
`[Trait("Sweep", "tree")]`. After the fix, run 000003-20261128T092631Z at 8d2ec452a3: `=== BUILD-LOCAL GATE: GREEN —
Conformance 29,548/29,548 ran · Unit 43,114/43,114 ran · RepoDrift 260/260 ran · Characterization 37/37 ran (skipped 0) ===`.

