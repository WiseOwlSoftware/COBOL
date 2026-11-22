## Wave 20262135 group A (data binder): one external/global attribute, the range-collation fact set, frontend descriptors without a severity, Cst reads as extension members

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
generated contexts through `frontend.parse`, and a rule-bearing read is a C# 14 extension member in `Cst/`. The wrapper
structs `DataDescriptionCst`, `DataDescriptionClauseCst` and `DataReferenceCst`, their implicit conversions, the
`Context` escape hatch and the unread `BaseName` are deleted; nine call sites read the contexts.

**Not done here.** PB2646 (national codec out of CobolBits) and PB2655 (pre-analyzed EditMask) are written to land after
open extract notes (PB2302, PB2327); PB2724 (one EXTERNAL and one GLOBAL clause rule), PB3231 (the resolver's written-
reference model) and PB2622 (diagnostic text in the catalog row) are each a mechanism of their own and go back to the
wave.
