## Entry 3880 — 2026-11-20 16:48 PST — The 1985 name table: one set for condition-names and data-names, and a contained program's names hide its container's in every class

### The re-reads that found it

The source listing's step 6b (kb/Work PB5932) moved the binder's name resolution into the name table, and 30 stored code reads
at `--std 85` named members that no longer exist where the read found them. The store does not call such a read wrong; it
calls it owed a re-read (kb/Work PB5863), and `code_reads.py plan --edition 1985` listed the 30 rows. They were read again, three
slices of ten, one reader and one refuter each, on a compiler built at main 832bc39c61, with the earlier records out of
reach so each read started fresh from the 1985 text. The readers confirmed 29 and called one PARTIAL; the refuters upheld 25,
overturned three and doubted one. Every overturning program was re-run on the same compiler before ruling. The doubted row
(RENAMES SR4, the same-word reading) stays CONFORMS under the recorded determination D-85-RENAMES-SAME-NAME. Two defects were
filed, and this branch fixes both.

### PB5972: condition-names, data-names and record-names are one set at COBOL-85

§1985:IV.4.2.2.1.1 groups the user-defined words into disjoint sets, and its fourth is "condition-names, data-names, and
record-names", whose duplicates qualification makes unique (§1985:IV.4.3.8.1 SRL2.1). The census applied the 2023 exceptions at
every edition, so a level-88 `X` beside a data item `X` of another record was refused COBOL2692 at the declaration (re-probed:
exit 65). `UserWordKinds.MayBeOneWord` now takes the edition: at 85 it admits the same type, a level-number with any type, and two
types of the fourth set. 2002 and 2014 print the 2023 exception list word for word, so their refusal of the pair is their own.

Declaring the pair legal exposed the uniqueness half: each lookup counted only its own class, so an unqualified `IF X` and
`DISPLAY X` each compiled to their own class. At 85 the data-name lookup now counts the condition-names its qualifiers reach in
its tier (a switch's condition-name included), and the condition-name lookup counts the data items; both refusals are COBOL1639
and name the set. A drift test rebuilds the 85 relation from the 1985 text. Goldens in `IV.4.3.8@1985-Uniqueness-of-Reference`:
one accepted program (`X OF F`, `X IN R1`, `X OF R2`) and three refusals.

### PB5973: a contained program's own name hides its container's GLOBAL name whatever the two classes

§1985:X.1.3.8: a reference is "to the object which that program describes", "even when it is a different type of user-defined
word", and §1985:X.1.3.8.2 GR1 applies qualification over the whole set before GR3a picks the nearer declaration. The re-probes
reproduced four faults: `INITIATE R1` in a program declaring `01 R1` ran its container's GLOBAL report (a wrong answer);
`MOVE 'B' TO XR` in a program declaring `RD XR` changed the container's GLOBAL item; `FX OF FREC OF FA` was refused because the
program's own `FD FA` hid the container's GLOBAL FA from the qualifier; `TOT OF CFG` was refused because a program declaring its
own `TOT` dropped the container's sum counter.

The fix asks every class of the set wherever classes are ranked. `DeclaredNearerThan`, the one predicate that hides inherited
names, gains the file-name and report-name tiers, and inherited report-names are hidden by it (`HideShadowedReports`, beside
`HideShadowedFiles`). The data-name, index-name, capacity-register and condition-name lookups ask a nearer file-name or report-name
(`NearerFileOrReportNameHides`, renamed from `NearerFileNameHides`; `NearerNameOfAnotherClassHides`, renamed from
`NearerDataOrConditionNameHides`), and the unidentified wording says "declared as a report-name". A file qualifier is matched
against every GLOBAL file, hidden or not, and every inherited sum counter joins the table, the nearer data-name winning at the
reference. The sibling sweep found one more fault the same mechanism caused: an own RD did not hide a container's GLOBAL
index-name (`SET IX TO 2` compiled). Goldens in `X.1.3.8@1985-Scope-of-Names`: two accepted programs and six refusals, one per
class pair. The note's fix shape, one resolver over the whole set with no per-class questions, is not built; each per-class
question now asks every class, and `GlobalNameClassDriftTests` holds the lists.

### Rows

1985-SR-IV.4.3.8.1-L2.1 and 1985-GR-X.1.3.8.2-1, -2, -3 carry the new tests (record_verdicts --edition 1985); their code reads
are owed a re-read now that the fixes have landed. Two 2023 rows that cited `NearerFileNameHides` (GR-8.4.6.2.1-1, -3) were
re-sited by a retirement batch.

### Gate

`=== BUILD-LOCAL GATE: GREEN — Conformance 28,230/28,230 ran · Unit 42,513/42,513 ran · RepoDrift 190/190 ran ·
Characterization up to date ...` (run 000003-20261121T001908Z, at d83dd1bd18). Two reds on the way, each fixed at its root:
DOC CITATIONS (run 000001) found a quotation of §1985:X.1.3.8 written after a §1985:X.1.3.8.2 citation; Conformance (run 000002)
found pb990-data-name-and-condition-name still expecting COBOL2692 at 85, the refusal PB5972 removed there, so its `refuse-at`
lost 85 and keeps 2002, 2014 and 2023.

### Review round 1

R1-1: hiding an inherited report-name also hid it as the report-name qualifier of LINE-COUNTER and PAGE-COUNTER, so
`PAGE-COUNTER OF R1` in a contained program with its own `01 R1` was refused COBOL2729; `LINAGE-COUNTER OF LF` over a hidden GLOBAL
LINAGE file was refused COBOL0863 the same way (on main since PB5786). A qualifier position is now its own reach,
`NameReach.Qualifier`, which reaches a hidden name (§1985:X.1.3.8.2 GR1), and both counter qualifiers use it. Golden
pb5973-hidden-global-names-qualify-counters-85.

