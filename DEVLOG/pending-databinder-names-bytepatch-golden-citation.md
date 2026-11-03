## PB4468, PB4470, PB4471: the DataBinder extract's stale names get a drift test, the byte-patch loop has one home, and a golden cites the random-access WRITE rule

Three process-only notes from the PB2296 / PB4454 / PB4455 reviews, one commit each.

**PB4468 (stale `DataBinder.Name` references).** Re-probe on the tree after #1054: every site the note lists was still stale. Instead of
patching the list, each `DataBinder.Name` in `src`, `tests` and `docs` was resolved against `typeof(DataBinder)`'s declared members,
nested types and partial files (reflection, not a regex of declarations: the first regex pass missed names it believed were declared).
That found about 110 more, nearly all in comments, crefs, grammar comments and three message strings: `BindMethodData`,
`BindPropertyClauses` and `ActiveMethodDeclares` (now `OoDataBinder`), the SPECIAL-NAMES binds (`SpecialNamesBinder`), the file-control
binds (`FileControlBinder`), `GlobalConstantsToInherit` and the constant operand readers (`ConstantEntryBinder`), `VisibleFiles`
(`DataBindState`), `TypeDecls` / `Conditions` / `IndexNames` / `LocalStorageRoots` (`ForestSlice`), `FilesByName` (`FileSlice`),
`UsageWord` (`UsageFamilies`), the removed `ResolveIndexItems` (`DataBinder.UsageInheritanceWalk`, the pass `UsageInheritancePass`), and
the cref `DataBinder._lapsedTypeMembers` (`ClauseFacts.LapsedTypeMembers`). `tests/version-matrix/constructs.json` carried five more; its
generated `ConstructRegistry.g.cs` was regenerated. The current design documents were repointed the same way (a script with one owner
table, run over `src`, `tests` and `docs`); dated narrative (`PHASE-*`, `pb59-*`, `CRITIQUE-*`, `SURVEY-*`, the architecture-review and
module-topology drafts, the plan's history) was left as the record it is. The PB2361 Sites line now names the files that exist.
PB4468 asked whether CS1574 polices the cref: it does not, documentation generation is off. `DataBinderNameReferenceDriftTests` is the
policing now (every `DataBinder.Name` in `src` and the construct catalogue is a member, a nested type or an existing partial's name or
stem). Its first run, after the sweep, still caught a removed partial's file name in `IntegerOperandPass` (reworded) and showed that comments name a
partial by its stem (`DataBinder.GroupValue`), which the rule therefore admits. Tests and `docs` are outside its scan: the two drift tests that
write a forbidden name on purpose must be able to.

**PB4470 (`PatchEvery`, the `MakeStale` wording).** `tests/_shared/ByteImage.cs` holds the one same-length byte-patch loop
(`ReplaceEvery`, `PatchFile`). `RuntimeVersionSkew.Patch` and the repository witnesses `RenameNamedArgument`, `RedirectRedefinition` and
`BreakIndexRow` call it; `PatchEvery` is deleted, no forwarder. The loop scans forward past each write (the old one restarted at byte 0),
and throws on an unequal or unchanged patch. `ByteImageTests` pins the count, the absent case, the forward scan, the image end, the
refusals and the write-back-only-on-change rule. The `MakeStale` docstring, the REPO-7 Fact comment, the two test names that said "older
runtime" and the sibling comment in `ExternalRepositoryExpansionTests` now say another major (`OtherMajor` is the next, the previous at an
all-nines major; REPO-7 and REPO-12 fire on any major that differs).

**PB4471 (golden citation).** `pb4454_global_file_operand_under_unnamed_root` writes a RANDOM relative file opened I-O, so its WRITE is
governed by 14.9.51.4 GR 29) b) and 32), not the sequential branch 29) a). Both quotes were `cite.py --check`ed (OK 29, OK 32); the
comment says the key is the caller's input and is not overwritten; the `rules:` header adds `GR-14.9.51.4-32`. Expected output unchanged
(`RK=0002`). The other four `29) a)` goldens cite it for sequential files, so none changed.

**Rows.** None re-verdicted (`closes_rows: []` with a reason on all three notes). **Self-review.** Architecture: the owner table in the
sweep is the only mapping, and the drift test is the structure that keeps it true. Full code: each replacement was read against the member's
declaration; the three message strings (`IntrinsicArgumentRules`, `IntegerOperandPass`) changed text only in the type prefix. Performance:
none reached. Duplication: one patch loop. **Gate:** `=== BUILD-LOCAL GATE: GREEN — Conformance 21,280/21,280 · Unit 39,566/39,566 ·
Characterization 37/37 cases ran (skipped 0)` (run `000001-20261103T123720Z`); `semgrep verify` PASS.
