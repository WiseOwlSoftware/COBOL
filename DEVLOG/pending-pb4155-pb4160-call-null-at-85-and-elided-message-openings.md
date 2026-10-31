## PB4155, PB4160: a CALL argument's NULL at COBOL-85 is the undefined reference, and no diagnostic opens with an elided statement

Two mechanisms in the CALL binder's neighbourhood, landed together.

**PB4155.** `CALL "X" USING NULL` at `--std 85` drew COBOL0899, the Format 2 argument diagnostic, instead of the
§8.4.2.1 undefined reference (COBOL1639) that PB1673 gave every other NULL read. §8.9 reserves NULL from COBOL-2002 only, so at 85 the
word names nothing declared. The re-probe showed four positions, not one: the bare argument, BY CONTENT and BY VALUE drew COBOL0899, and BY
REFERENCE NULL drew COBOL1677 (the §14.9.4.3 SR3 address screen). `CallBinder.NullSpellingOf` names the predefined NULL an argument writes in any of
those places, and `BindCall` asks `UnreservedKeywordReference.IsFreeAt("NULL", edition)` once per argument, ahead of the Format 1
narrowing, and reports through `UnreservedKeywordReference.Report`. At 2002+ nothing changes (§8.4.3.10.3 SR1 a) admits NULL as an argument only
in a program-prototype format CALL). Golden: `8.3.2-COBOL-words/pb4155-call-null-undeclared-at-85`.

**PB4160.** 96 source lines of diagnostic text opened `VERB ... PHRASE`, in CALL, INSPECT, READ, START, WRITE, OCCURS, CODE-SET, MULTIPLY, DIVIDE and in places
the note had not listed (INVOKE USING, ALLOCATE, INITIALIZE, PERFORM TIMES, ACCEPT/DISPLAY exception phrases, CLASS and SYMBOLIC CHARACTERS IN,
the SEARCH shape messages of the parse layer, the §14.5.1 scope-terminator message, the COBOL2894 warning for `SET ... TO ENTRY`). The console renders the
ellipsis as '.', so a programmer read `CALL . USING BY VALUE`. Each message now names the phrase as written: `CALL USING BY VALUE 3`
(which also ended the doubled `BY CONTENT BY CONTENT NULL`), `OCCURS ASCENDING KEY IS 'K' on 'T'` (from `OccursKey.Descending`),
`COLLATING SEQUENCE OF IK IS 'X'`, `SET 'PP' TO ENTRY`, `DIVIDE REMAINDER with 2 GIVING receivers`. A mid-sentence ellipsis is the general
format's own notation and stays.

The shape that makes the next case automatic is `tests/_shared/ElidedMessageOpening.cs`, one definition of the head with two readers. Its
`Source` pattern is `DiagnosticOpeningElisionDriftTests`, over every `Error(`, `Report(`, `Refused(`, `Warning(`, `InitializeErrorAction(` and
`EditionDiagnostic(` call in `src/`; it replaces the SET-only `ElidedEcho` of `SetDiagnosticNamesReceiversDriftTests`, which keeps the receiver-naming
half. Its `Rendered` pattern runs in `ConformanceSuiteTests` over the RENDERED text of every negative golden, so a head that string-building
assembled from a label is seen as well. The first draft of the source pattern used two adjacent unbounded classes and backtracked catastrophically on a
fifty-file scan; the leading-argument group is now one alternation under one star. Goldens:
`14.9.4-CALL/pb4160-verb-phrase-diagnostics-name-the-phrase` (twelve diagnostics) and `13.18.38-OCCURS/pb4160-clause-diagnostics-name-the-phrase`
(five). Existing tests that pinned the old text moved with it (`SEARCH NOT AT END`, `SEARCH KEY:`, `SET 'PP' TO ENTRY`, three `diagnostics.json` expects).

Left unfixed and reported: the runtime messages of `SetEmitter` / `ControlFlowEmitter` (`LandAmount(..., "SET ... TO", ...)`, snapshot-pinned) and
`FileRegistry.MisroutedVerb` open with an ellipsis too, but they are exceptions of the running program, not compile diagnostics.
