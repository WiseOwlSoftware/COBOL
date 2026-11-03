## Lead filing for thirteen merged reports of waves 20261828 to 20261845: six notes filed, stale names fixed

The clerk read the "New defects" and non-blocking review LEAD lines of the reports for PB4449, PB4442, PB4281, PB4283, PB4454, PB2296, PB4456, PB4455, PB4459, PB4464, PB4463, PB4466 and PB2357. Most leads were already owned by notes filed earlier the same day (PB4469-PB4488); six had no owner.

Filed (kb/Work):
- PB4489: a paired conditional phrase repeated after a left statement is silent (`ConditionalPhraseAssociation`, the PB4449 walk does not count it).
- PB4490: `CobolErrorStrategy.BeginsAStatement` caches the set `StatementBlockStart` already caches.
- PB4491: the signature graph reads an elementary restricted-pointer formal by class and a group member by name (PB4442 residue; a design decision that reverses PB2495).
- PB4492: no golden pins the property-accessor and interface-pair arms of D-RP1 (PB4442).
- PB4493: `Frontend/Diagnostics/DiagnosticDescriptors.cs` is a second descriptor registry beside `DiagnosticCatalog`.
- PB4494: a bare verb's "cannot parse construct" position differs between `FREE.` and `DISPLAY.` (PB4283).

Stale names fixed in place: `ReferenceResolver.ReportScope` is `EnclosingReport` in PB1292, PB1431, PB1456 and the inventory notes of SR-13.18.53.3-4, SR-8.4.3.14.3-1 and SR-8.4.3.15.3-2; `DESIGN-external-repository.md` (the pair-mode relation reads the signature class only under `pointersBySignature`), `DESIGN-architecture-review.md` (the OBJECT and FACTORY halves take `ConfigurationSource.ClassHalf`, not `Own`); PB4449 and PB4442 and `DESIGN-frontend-grammar.md` point at their new notes.

Dropped, with why:
- Owned by an existing note: the COBOL5996 and COBOL1503 dead arms, the untested codes, the band descriptors and COBOL5979 (PB4480); COBOL0806 and COBOL1564 (PB4481); the stale code prose and `CONDITIONS_EXCEPTIONS_DESIGN.md:614` (PB4482); item-108 position (PB4485); F.2 citations (PB4486); `RunTextStages` pins (PB4487); COBOL-WORDS and COPY (PB4488); the repository-expansion duplicate warning and doc column claim (PB4469, PB4483); constructs.json citations naming members (PB4467); `GetText()` in other binders (PB4252); the ~45 `private` to `internal` widenings (PB2361 "Also remaining").
- Already fixed on main: the carried IF/ELSE and recovery-record leads (PB4447, PB4449 goldens); OPTIONS FLOAT-BINARY (PB4464); the 19 raw `Edition.Error` codes (PB4463 deleted the literal-code overload; `grep` finds none); the golden's 29) a) citation (PB4471); `PatchEvery` and the `MakeStale` docstring (PB4470); the lane reviewer's scratch directory and the hook for the session scratchpad (PB4473); PB914 and PB2023 (landed by PB4463); PB4185 site 2 (landed); GR-12.3.8.4-10/-11 (already CONFORMS); semgrep blindness (`verify.py` PASS: PB4462 counts the partially read spans); `ConcernOwned` and the tokenizer holes (the list is gone with PB2296 M7); the `_callInheritedStatusPlace` mention (a dated survey of a removed field).
- Left as the reporting finisher decided: COBOL1688 quotes the declared name of a qualified argument (PB4431 records it as the item's name, not a node); the "no end-to-end repro for a sub-compilation-only error" remark of PB4456 (a testing observation, the relabel is proved at its own level).
