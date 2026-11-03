## PB4454: a contained program's I-O on a GLOBAL file reaches the file's own clause operands (RELATIVE KEY, ASSIGN USING, VARYING DEPENDING ON, LINAGE, FILE STATUS) through one list, bridged like a global root

**What was wrong.** A program contained in the one that describes a GLOBAL file runs input-output statements on it whose
operands are not global: the file's own clauses name them in the container (ISO §8.4.5: an implicit reference resolves in the
element that holds the clause; §13.18.27.4 GR2 lets the contained program name only the global names). Since PB2375 the
contained unit's `PlaceTable` held the containers' GLOBAL items only, so `places.Of(item)` for the RELATIVE KEY
(`KeyedIoEmitter.Rrn`, `SortEmitter.EmitRelativeKeyStore` and the GIVING store), the RECORD VARYING DEPENDING ON item
(`SequentialIoEmitter.VaryingDepending`), the ASSIGN USING item (`ExecutingAssignment`) and the LINAGE operands
(`LinageOpExpr`, a sibling the note did not list) threw `InvalidOperationException` ("no Place was built for data item 'RK'");
before PB2375 the same source produced C# that failed with CS0120. Re-probed on this build: all five reproduced
(`rel.cob`, `asg.cob`, `lin.cob`, `srt.cob`, scratch `w20261839a`). The note's "SORT keys" do not exist: an SD cannot be GLOBAL
(COBOL1970). Only FILE STATUS worked, by a one-off re-anchoring of a container-built place (`ProgramEmitter.AnchorPlace`,
`EmitterState.InheritedStatusPlace`) that handled a plain item and returned null for any other shape.

**The shape (one mechanism for every file-clause operand).** The note asked for FILE STATUS's anchor mechanism for every
operand. A total re-anchoring of an arbitrary place (a window, an ODO wrap, a RENAMES composition) would have carried every
Place subtype and every position inside it, so the other way round was taken: the operands are bridged and placed exactly
like a global root, which already works for every storage shape, and FILE STATUS moves onto the same path.
- `FileModel.ClauseOperandItems` is the one list: ASSIGN USING, FILE STATUS, RELATIVE KEY, VARYING DEPENDING ON and the
  LINAGE operands' items.
- `DataBinder.GlobalFileOperandRoots` takes the roots of those items outside any global record, for each GLOBAL file;
  `GlobalPlaceableItems` (the contained unit's table) and `InheritStep.ContainerBridgedRootsOf` (the C# bridges in `Merge` and
  the member-name reservation in `Seed`) read it. The roots are NOT offered as names (`ContainerGlobalRootsOf` still feeds
  `OfferContainerGlobals`): the contained program cannot write `RK`.
- Deleted, with every caller: `ProgramEmitter.AnchorPlace`, `EmitterState.InheritedStatusPlace` (and its two `Clear` sites),
  `AccessPath.Reroot`, `RootFieldSegment.Anchor`; `PlaceRenderer.Anchored` takes a non-null anchor (bridges only);
  `SequentialIoEmitter` lost its `CallUnitState` parameter; the `ProgramEmitter.cs:p` exemption in
  `PlaceStorageFormSwitchDriftTests` went with the switch it excused.
- `FileClauseOperandDriftTests` is red until a data-item property added to `FileModel` is on the list or named
  record-resident. DESIGN.md (the GLOBAL FD inheritance paragraph and the USE GLOBAL dispatch paragraph), INTERPROGRAM_DESIGN.md and
  DESIGN-backend-abstraction.md L7 describe the current shape.

**Goldens** (`conformance-suite/cases/13.18.27-GLOBAL`, accepted 2002-2023, the rules' band): `pb4454_contained_io_on_global_file_clause_operands`
(random WRITE, duplicate-key WRITE '22', random READ found and '23', sequential WRITE and READ NEXT storing the relative key,
AT END '10', variable-length WRITE of 3 and 5 bytes and the '44' of 9, READ restoring the length, a LINAGE page of 4 with
footing 3 read from container items, a SORT of one relative file to another storing the GIVING key),
`pb4454_contained_io_on_global_file_assign_using` (OPEN and SORT USING by the content of the container's item, '35' for a
missing file) and `pb4454_global_file_operand_beside_intermediate_same_name` (two levels out, a RECURSIVE container, an
intermediate program with its own `RK`). Every value was derived from the cited rule before the run, and every citation
passed `cite.py --check`. No refused-below negative: the change adds no rule and no gate; ASSIGN USING at 85 is already
refused (COBOL0900).

**Rows.** None closed: every rule the goldens cite was already CONFORMS (`closes_rows: []` with its reason).

**Self-review.** Architecture: one list, one bridge path, no special case left for FILE STATUS; the model holds no access text.
Full code: ODO DEPENDING ON of a global record is refused for a non-global item (COBOL2788), a GLOBAL report's SOURCE items
are read in the container (probed), so no sibling operand remains; `SD ... IS GLOBAL` is refused. Performance: one list per
container per contained unit at bind time. Duplication: the root-bridge enumeration had two copies of `foreach CallGlobalRoots`
(Seed reservation, Merge bridges); both now read `ContainerBridgedRootsOf`.

**Gate.** `=== BUILD-LOCAL GATE: GREEN — Conformance 21,032/21,032 · Unit 39,468/39,468 · Characterization 37/37 cases ran (skipped 0)`,
run `TestResults/build-local/000001-20261103T052821Z`; the nine new golden cases (three tests at 2002, 2014, 2023) are `Passed` in its
`Conformance.trx`. `scripts/semgrep/verify.py`: PASS, no rule count increased.
