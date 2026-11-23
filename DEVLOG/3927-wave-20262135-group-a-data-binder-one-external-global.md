## Entry 3927 — 2026-11-22 14:55 PST — Wave 20262135 group A (data binder): one external/global attribute, the range-collation fact set, frontend descriptors without a severity, Cst reads as extension members; PB3229: a diagnostic names a category or a usage in the standard's words, never the enum member's; Wave 20262139 group A: the two predecessor branches merged, their review findings answered

### Wave 20262135 group A (data binder): one external/global attribute, the range-collation fact set, frontend descriptors without a severity, Cst reads as extension members

The group's first run was killed by a Windows reboot; this branch merges its pushed work (worktree-wf_1cea1387-608-1) and
finishes it. Six of the eleven notes are settled here; five go back to the wave.

**PB3212 (landed).** Whether a record has the external attribute, or is a global name, was a separate predicate at five
sites, and the narrower copy (`ExternalItemIdentity`, clause and type only) refused an item of an `FD … IS EXTERNAL`
record as another external file's RELATIVE KEY (COBOL1624), though §2023:13.18.22.4 GR4b makes every record of an external
file external. A record joins its file only through `FileModel.AddRecord`, which sets `DataItem.RecordOfFile`; the model
answers `HasExternalAttribute`, `IsGlobalName` and `IsInFileSectionRecord` once, and the file-scan siblings read the
pointer. Golden `cases/13.18.22-EXTERNAL/pb3212-external-fd-item-relative-key` (85..2023).

**PB2733 (retired).** 868f612503 (PB5979) had already deleted the `DataBinder.WrittenText` forwarder.

**PB3214 (landed).** The THROUGH range carriers of `IN alphabet-name-1` (§2023:14.7.8 GR2) were an undeclared accumulator
appended in the entry walk and in procedure binding. They are now the fact set `DataBindState.RangeCollations`, whose
contract admits the entry walk and ProcedureBound, declared by the ProcedureBinding pass and read whole into
`DataBindResult.RangeCollations` in ordinal order; the parallel id list is deleted. Behavior-neutral: the 705
range/ALPHABET conformance cases pass on the built tree.

**PB2624 (landed).** Ten parse-hint descriptors declared `Warning` and were reported as errors, and `GuessCobolIntent`
typed each sentence again beside its template. The frontend `DiagnosticDescriptor` is now `(Code, MessageTemplate)` with
one composition, `Format`; it has no severity because every frontend descriptor is an error by its emit path. The hint
record composes its message from the template and its arguments. Output text is unchanged; `FrontendDiagnosticDescriptorTests`
pins the template shapes. The merged R2 findings (the operator fold, the conditional-phrase rules, the reference-format,
COPY and MERGE gates) had been unified by earlier landings; the validation passes' two report mechanisms is a separate
mechanism, handed back as a lead.

**PB2642 (retired).** 56312e1996 (PB2643) had already made `DeEditFloat` validate by the `FormatFloatCore` round trip.

**PB2731 (landed).** DESIGN-frontend-grammar now takes DESIGN-architecture-review §8.1 as the boundary: binders read the
generated contexts through `frontend.parse`, and a rule-bearing read is an extension method in `Cst/` (a C# 14 `extension` block was tried and dropped: the
inventory's two code-location resolvers disagree on its members). The wrapper
structs `DataDescriptionCst`, `DataDescriptionClauseCst` and `DataReferenceCst`, their implicit conversions, the
`Context` escape hatch and the unread `BaseName` are deleted; nine call sites read the contexts.

**Not done here.** PB2646 (national codec out of CobolBits) and PB2655 (pre-analyzed EditMask) are written to land after
open extract notes (PB2302, PB2327); PB2724 (one EXTERNAL and one GLOBAL clause rule), PB3231 (the resolver's written-
reference model) and PB2622 (diagnostic text in the catalog row) are each a mechanism of their own and go back to the
wave.

**Gate.** Five reds were fixed on the way, each a check doing its job: three 1985 code reads and six inventory rows had
been hand-edited after PB3212 deleted the members they named (restored, and re-sited through `record_verdicts.py`
batches that retire the old witness by name), three PB3212 members needed classifying in the backend contract,
`RangeCollationFields` had grown a pair on a tolerated layering row (it now names identities only), the inventory's two
code-location resolvers disagree on C# 14 `extension` blocks (classic extension methods instead), and a stale
citation-baseline entry was dropped. Gate run 000006 at 68ccde0cb8: `BUILD-LOCAL GATE: GREEN — Conformance 29,084/29,084
ran · Unit 42,689/42,689 ran · RepoDrift 230/230 ran · Characterization 37/37 ran`. No host I/O file changed, so no
Linux gate ran.

### PB3229: a diagnostic names a category or a usage in the standard's words, never the enum member's

Wave 20262135, group D (PB1754 campaign). The group's eleven notes are eleven mechanisms; this branch lands the first
in harm order, PB3229, and hands the other ten to a fresh agent.

**Re-probe.** The note's UDF scenario no longer reproduces (PB1400 and PB3192 already name a BY VALUE argument's class
through `IntrinsicArgumentRules.ClassNameOf`). The mechanism still did: `01 E PIC ZZ9 SIGN LEADING.` printed
`COBOL2422 … this entry is of category numericedited (PICTURE ZZ9)`. A sweep found 18 more message sites that
interpolated a `PicCategory` member's C# name ("numericedited", "pointer" for a data-pointer, "Alphanumeric") and five
that did the same with a `Usage` member ("PACKED", "Comp5").

**Fix.** `ItemCategory.CategoryName` is the one spelling of a §2023:8.5.2.1 Table 2 category. It gains a `PicCategory`
overload for a value without a PICTURE (a literal, a constant, a function result), and its `Table16Operand` overload is
now public. Every binder message reads it, and every usage word reads `UsageFamilies.UsageWord`.
`SpecialNamesBinder.Literals` also said "class" where it meant category. `IntrinsicArgumentRules.Name` lost its
`ToLowerInvariant` fallback: the switch covers every `CobolClass` member, and a new member throws until it is spelled.

**Drift and golden.** `ClassSpellingDriftTests` gains a category sweep and a usage sweep, each with a theory that pins the
forms the sites were written in. Exception text and `string.Create` identity keys are exempt. The golden
`13.18.52-SIGN/pb3229-sign-numeric-edited-category` is refused at all four editions with `this entry is of category
numeric-edited`.

### Wave 20262139 group A: the two predecessor branches merged, their review findings answered

This branch merges the unlanded w20262135 group-D branch (PB3229) and group-A branch (PB3212, PB3214, PB2624, PB2731) and
answers the five findings their branch reviews raised.

**PB3229 R1-1 (a crash).** The FROM-phrase function-class screen of WRITE, REWRITE and RELEASE read a function result's
category from `MoveTable16.SenderPosition`, a Table 16 position that answers `Group` for an alphanumeric group (the
§2023:14.9.25.4 GR4 group move). PB3229 routed the message through `ItemCategory.CategoryName`, which throws for `Group`,
so `WRITE R FROM FUNCTION GRPF` over a function returning a group crashed; main had printed COBOL1914 "of category group".
Both were wrong: §2023:8.4.3.2.4 GR1 gives the function's temporary item its RETURNING item's class and category, and
§2023:8.5.2.1 makes an alphanumeric group's category alphanumeric, so the function is an alphanumeric function and SR4
admits it. The screen now asks `IntrinsicResultType.OperandCategory`, the one operand-category reader. Golden
`14.9.51-WRITE/pb3229-write-from-group-function` (2014, 2023) writes and reads back `[ABCDE12345]`.

**PB3229, second review round (a strongly-typed result).** That reader answers Alphanumeric for any picture-less group,
the §2023:15.3 AR2 reading of a function argument, so a function returning a strongly-typed group was admitted where main
had refused it. §2023:8.5.2.1 makes such a group's class and category its type-name, so the screen asks
`StrongTypeModel.StrongGroupType` first and refuses with "of category T". Refusal goldens
`14.9.51-WRITE/pb3229-write-from-strong-group-function` (2002, 2014, 2023) and
`14.9.35-REWRITE/pb3229-rewrite-from-strong-group-function` (2014, 2023).

**PB3212 R1-1 (test coverage).** Four tests pin what the external-identity change decides: a RELATIVE KEY in a
non-external FD record (COBOL1624), the external record's key at different positions in two programs (COBOL1575), a READ
of an unwritten relative record through the FD-record key (INVALID KEY, status 23, §2023:14.9.30.4 GR29), and a PADDING
CHARACTER item of an external FD record, which §1985:VII.2.7.3 SR3 still bars from the File Section (COBOL6848): the
review's guess that the change admits it was wrong.

**PB3212 R1-2 and R1-3 (duplication).** `DataItem.IsInFileSectionRecord` reads the one section fact, `Section`, so a
record of a repeated FD answers the same to the TYPE/SAME AS screens and the I-O-CONTROL placement rules. Six readers that
scanned `file.Records` for membership ask `FileModel.HoldsRecord` (the record's own `RecordOfFile`), and the ASSIGN USING
own-record walk asks `RecordLayout.IsInRecordOfFile`.

**Not done.** PB2628, PB2655, PB2637 and PB2747 are each a mechanism of their own and stay open for the planner.

**Gate.** Run 000001-20261122T211818Z at b9c0051055: `BUILD-LOCAL GATE: GREEN — Conformance 29,104/29,104 ran · Unit
42,711/42,711 ran · RepoDrift 231/231 ran · Characterization 37/37 ran`. The Linux gate's unit (42,843) and conformance
(29,104) legs were GREEN; its self-test leg failed `scripts/hooks/test_forbidden_commands.py` on two `git push <url>`
cases that pass on Windows, a tooling defect of the Linux clone untouched by this branch.


**The 1985 code reads the move owed (finisher).** The landing refused the branch (`code_read_landing.py`, kb/Work PB6056, exit
6): three stored 1985 reads named the members it folded into one fact, `DataBinder.IsInExternalRecord` (1985-SR-VI.5.8.3-8,
1985-GR-VIII.4.4.4-2) and `DataBinder.IsGlobalRecord` (1985-SR-VI.5.8.3-9). Re-read at pin 524fb73a18 as slice `1985-c08-pb2628`
against `DataItem.HasExternalAttribute` and `DataItem.IsGlobalName`: the first reads' probes re-run at `--std 85` print what they
recorded, and an adversarial pass on the FD half now read off `RecordOfFile` (a second FD record's counter, an SD record's, a
record of the other, non-external or non-global FD) upheld all three; `code_reads.py import --replace`, 3 CONFORMS, 3 UPHELD.
