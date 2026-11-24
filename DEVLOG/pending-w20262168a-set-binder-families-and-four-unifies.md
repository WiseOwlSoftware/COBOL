## SetBinder becomes the SET format dispatch over seven family binders; four census clone pairs become one body each; the RuntimeApi ratchet reaches zero

Wave 20262168 group A (kb/Work cluster PB2119: PB2155, PB2314, PB2157, PB2158, PB2162, PB2302, PB2156 landed or retired; PB2399,
PB2416, PB2305, PB2418 left open for a continuation), carried on top of closed-hierarchies-wave1 (PB6168) and
worktree-wf_dc6f75bb-a8b-25 (PB5922), merged at 325c5e2f38. Every change is behavior-neutral: the §4 oracle
(`scripts/arch/compare_oracle.py --base 325c5e2f38`) answered IDENTICAL over 23,639 cases after each mechanism.

**The unifies (census R0-0053 to R0-0056).** Each pair was triaged by what its bodies bind, not by their shape.
- PB2156: LEAP-SECOND, PROPAGATE and REF-MOD-ZERO-LENGTH each copied one `{ ON | OFF }` collection loop; it is now
  `OnOffDirectiveStage.Collect`, and each stage keeps its keyword, registry row and event type. TURN and FLAG stay apart: they
  report a malformed operand themselves.
- PB2157: `SymbolTable.TryResolve` and `TryResolveCondition` share one method-overlay precedence (§2023:11.7.4 GR5),
  `TryNearestTable<T>`; the table selector is a static lambda, so the hot lookup allocates no delegate.
- PB2158: INITIATE and TERMINATE bind through `ReportWriterBinder.BindReportNameSeries` with a `ReportNameVerb` per verb.
  GENERATE stays apart (a report-name or a detail group, one statement, not a series).
- PB2155: SET Formats 8 and 9 bind through `BindSetPrototypedPointer` over a `PrototypedPointerFormat` per carrier. PB817 had
  made the compare one rule; now the statement is one too. Every message is unchanged, character for character.
- PB2162 retired: `RuntimeApi.CodomainConst` was deleted by ef68c6d1ac (PB3829), so R0-0060 has no pair left.

**PB2314, the SetBinder extract.** SetBinder had 1,778 lines on this tree. It now holds only the dispatch (249 lines). The
formats moved, member for member, to `SetContentBinder`, `SetLocaleBinder`, `SetPointerBinder`, `SetAddressBinder`,
`SetIndexBinder`, `SetCapacityBinder` and `SetConditionBinder`. Format 1's tail became `SetIndexBinder.BindIndexAssignment`,
and the receiver helpers became `SetFormatSelection.Receivers`. `SetBinderFamilyDriftTests` pins the boundary: SetBinder
declares only the dispatch, and no member has two homes. The SET drift tests now read every `Set*Binder.cs`. Three
`record_verdicts` batches moved the code-locations with the members (58, 13 and 7 rows in the 2023, 2002 and 1985 stores; GAP
unchanged). Every comment, doc and grammar comment that named `SetBinder.<member>` now names the member's new owner.

**PB2302 step 1.** The guard's own rule counted 30 bare runtime names (the ratchet table allowed 36). All of them now go through
a new section of `RuntimeApi`: emitted-text composers, plus three compile-time rule calls and `DecSidiDigits`.
`RuntimeApiGuardTests` lost its table and now forbids every bare access.

**Self-review** (architecture, code, performance, duplication): one mechanism per job. No forwarder is left behind; the format
entries `BindSetProgramPointer`/`BindSetFunctionPointer`, `BindInitiate`/`BindTerminate` and the directive stages' `Process` each
name their per-format data, which the inventory and the statement binder read. No hot path gained an allocation. The shape
siblings were checked and recorded as apart (TURN/FLAG, GENERATE, Format 7, `InspectReplaceKindText`).

**Gate:** see the landing's record.
