## PB1754 group D (wave 20262139): --suppress ships, a refused UDF argument reports once, MODULE-NAME's keyword is one type, every SR3 walk projects SubscriptLevels, the text runtime hands out ImmutableArray

Group D of wave 20262139 held eleven notes on the module-topology root. Five mechanisms landed on this branch, one was re-probed and
partly discharged, and five are left for a fresh implementer (the group is several mechanisms, not one).

**PB2652 (defect): `--suppress` did not exist.** Re-probe: `cobol pb595_turn_inside_format3_perform.cob --source-format free` printed
`warning COBOL2187`, and adding `--suppress COBOL2187` printed `Unrecognized command or argument '--suppress'`, exit 1, though owner
decision D20 makes COBOL2187 suppressible and the catalog, the descriptor and the docs promised the option. One rule now,
`WarningSuppression`: a name is a code, a catalog descriptor's id or its family key, matched without regard to case; both sinks ask it
before keeping a warning (`DiagnosticBag.Add`, `EditionContext.AddWarningOnce`), so a muted warning is in neither the printed lines nor
the listing's records. The descriptor identity now reaches the record (`Diagnostic.Descriptor`, stamped by EditionContext's descriptor
overloads and `Declined`, which forwarded the code alone). `CompilerDriver.Options.Suppress` carries the names to an expansion's own
compilation and the listing's options section; an unknown name is a CLI argument error. Decisions (kb/Work PB2652 addendum): an error
is never muted; the §2023:4.2.2 conformance-checking suboption (14.8.2, 14.8.3, 9.3.8.2.3) is a separate GAP, deferred to its own
note. Tests: `WarningSuppressionTests` (Conformance: reported without the option; muted by code, lower-case code and id in lines and
records; another name leaves it; the front-end sink mutes COBOL0903; an error is never muted; an unknown name refused) and
`CliParserTests.Suppress_IsRepeatable_AndRefusesAnUnknownName`.

**PB3227 (half): a refused UDF argument drew a second COBOL1506.** Re-probe: `FUNCTION R35TWO(NOSUCH 1)` printed COBOL1639 and
`COBOL1506: ... is not yet supported for user-defined function activation`; `(LEADING 1)` likewise after COBOL1638. `UdfActivate`
now carries a `BoundOperandError` argument as `BoundExprError.Carry` and reports nothing; an operand shape `UdfArg` has no arm for is
Unbuilt (COBOL1756). COBOL1506 names the §2023:14.8.2.1 count rule alone, and that rule is one predicate,
`ParameterConformance.ArgumentCountConforms`, asked by CALL, INVOKE and the function binder. Test:
`UdfInvocationTests.ARefusedArgument_IsReportedByItsOwnRuleAlone`. Still open: the data pass's pass-mode derivation and the
category-word spelling (about 12 `ToString().ToLowerInvariant()` sites).

**PB2742: FUNCTION MODULE-NAME's keyword was a bare int 0..4.** `ModuleNameKeyword` (runtime) is bound once, carried by the bound
node, emitted as the member name and switched on by `ModuleStack.Name`, whose default now throws. The `module_name` golden prints its
`.out` unchanged; `ModuleStackInvariantTests.AValueOutsideTheKeywords_Throws` pins the default.

**PB3217: the SR3 table-level walk had five more copies.** `OccursChainOf`, `PlaceBuilder.SubscriptLevelsWithin`,
`RecordLayout.OccursSubjectOf`, the RENAMES SR3 screen and the SAME AS SR1 screen each re-walked the parents; each now projects
`DataItem.SubscriptLevels`. The drift test matches the walk semantically and a theory pins each respelling.

**PB2776: CA1819 in the text runtime.** Seven public members handed out their backing arrays; each is `ImmutableArray<T>`
(`ReorderGroups` a zero-copy view), and a runtime `.editorconfig` makes CA1819 an error in `Globalization/` and `Collation/`. The
22 other CA1819 sites (types generated programs construct) are the rule's next wave. Runtime major 33 -> 34 (`abi_pin.py bump
--runtime-major`): `CobolModule.Name(int)` and the array members are removed public surface.

**PB2744 (half):** the constructor arm is discharged by PB2756 (481481e500); the census's table-enumeration exclusion stays open.

**Left for the next implementer:** PB2746, PB2763, PB3213, PB3223, PB3230.
