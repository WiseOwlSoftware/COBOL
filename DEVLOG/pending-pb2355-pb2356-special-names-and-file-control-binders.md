## PB2355, PB2356 — DataBinder extract steps 5 and 6: SpecialNamesBinder and FileControlBinder

Steps 5 and 6 of the eleven-step `DataBinder` extract (docs/rearchitecture/DESIGN-architecture-review.md §8.3 (1), census
R0-0001; kb/Work PB2355, PB2356), run as one group on PB2296 step 1's branch (the owner's 2026-11-02 rule: several steps
in parallel, each on its own partials). Both are behavior-neutral by contract, so the proof is the §4 oracle:
`python scripts/arch/compare_oracle.py` answered `=== ARCH-ORACLE: IDENTICAL — 16143 cases ===` against main's
2e1a4eed8377 after step 5 and again at the final head.

**SpecialNamesBinder (PB2355).** `DataBinder.Switches.cs` (2,414 lines, the SPECIAL-NAMES paragraph and the
OBJECT-COMPUTER alphabets and classification, ISO §12.3.6-§12.3.7) is a class of its own, made once per unit as
`DataBinder.SpecialNames`, over six partial files by clause family (the core walk and the name tables;
`.Alphabets`; `.Literals`, the shared literal-operand screens; `.Classes`, CLASS and SYMBOLIC CHARACTERS; `.Currency`;
`.Locales`). It owns the state it declares and asks the data binder only for what is not SPECIAL-NAMES (the §8.3.2.2
declaration funnel, the constant readings, the method tier of a lookup). Every reader asks `Data.SpecialNames`;
there is no forwarder on `DataBinder`. What the old partial held that is not SPECIAL-NAMES stayed on the data binder in
`DataBinder.Configuration.cs`: the contained-unit seed `InheritConfiguration` (which now asks
`SpecialNamesBinder.InheritFrom` for its half), the REPOSITORY specifier screens and the configuration-literal constant
readings. `DropShadowedConstants` asks `SpecialNames.HideShadowedSymbolics()` instead of reaching into the binder's
private set. The design had put the I-O-CONTROL partial under this binder too; it is §12.4.6, the input-output
section, so it went with step 6 (the design's own module-topology rows 10 and 15 had it so).

**FileControlBinder (PB2356).** The FILE-CONTROL SELECT entries (`BindFileControl`), the RECORD KEY and ALTERNATE RECORD KEY
source phrase, the key-level and file-level COLLATING SEQUENCE clauses with their post-build resolution, and the
ORGANIZATION, ACCESS MODE, SHARING and LOCK MODE mappers left `DataBinder.cs`; the I-O-CONTROL paragraph (the SAME
clauses and APPLY COMMIT) moved from `DataBinder.IoControl.cs` to `FileControlBinder.IoControl.cs`. The binder is made once
per unit as `DataBinder.FileControl` and owns `SelectedFiles`, `SuppressWhenOperands` and `ApplyCommit`; the files
themselves stay on the data binder, where the FD, the report section and the file resolution reach them. The COLLATING
SEQUENCE alphabet class test (`CollatingAlphabetFault`), shared by the file clause, PROGRAM COLLATING SEQUENCE and
SORT/MERGE, is an alphabet question and lives in `SpecialNamesBinder`. Not moved: the post-build half (`ResolveFiles` and its
FILE STATUS, LINAGE, ASSIGN USING and key resolutions) and the FD/SD binding, named in §8.3 (1) as the next FILE step.

**Guards and bookkeeping.** `ConfigurationBindersBoundaryDriftTests` (new) pins both binders: the data binder declares no
member (name and parameter types) a binder declares, which is the rule against forwarders, and each binder is made in one
place; it was seen red on the first run, when `SpecialNamesBinder.Bind` and `DataBinder.Bind(ProgramUnitContext)`
collided, which is why the binder's entry point is `BindSpecialNames`. Six existing source-scan drift tests that read
`DataBinder.Switches.cs` now read the `SpecialNamesBinder*.cs` partials through the new `SpecialNamesSource` helper.
The emitters' and the statement binder's reads of the binders' state are tolerated edge pairs onto `SpecialNamesBinder`
and `FileControlBinder` in `architecture-edges.json`, each with the removers of its pair onto `DataBinder`
(`removedBy` PB2361 and its siblings); they fall when the backend input stops carrying the binder. 182 inventory rows had
their code-location witnesses re-pointed through `record_verdicts` (three batches; verdicts unchanged, GAP 19 to 19). The
frozen evidence that cites `DataBinder.Switches.cs` carries a `superseded_by` marker. The design docs name the new
homes (DESIGN-architecture-review §8.3 (1), DESIGN-module-topology rows 10 and 15, FILES_DESIGN, DESIGN, DATA_MODEL_DESIGN).

**What the run found.** `wiseowl-cobol-raw-diagnostic-code-literal` went from 225 to 226 in `scripts/semgrep/baseline.json`:
the `COBOL1512` literal in the LOCK MODE rule moved with its code and is now matched, where the old site's
interpolated string with `…` in it never was. `COBOL1512` has no catalog descriptor and three rules share it
(LOCK MODE with sequential access, a lock phrase under LOCK MODE AUTOMATIC, OPEN SHARING ALL OTHER without a LOCK MODE),
so the finding is a defect in the diagnostic catalog, not in this move; it is a lead for the fix lane.
The gate's verdict on the code head 2f23f8d93: `=== BUILD-LOCAL GATE: GREEN — Conformance 20,975/20,975 · Unit 39,404/39,404 · Characterization 37/37 cases ran (skipped 0) ... (run 000003-20261102T232400Z) ===`.
Two reds on the way, both mine: gate 1 stopped on the evidence-supersession audit (the frozen record cites the deleted
file), gate 2 on `ReportItemReferenceTests`, which names `ResolveClauseOperand` by its visibility.
