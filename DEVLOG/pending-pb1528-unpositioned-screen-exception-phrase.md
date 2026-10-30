## PB1528: the unpositioned screen ACCEPT / DISPLAY with an exception phrase draws COBOL1707, and §14.5.3.3 keeps device statements from stealing an arm

Wave 20261769 H, kb/Work PB1528 (one mechanism).

**Re-probe.** `ACCEPT SG ON EXCEPTION DISPLAY "E" NOT ON EXCEPTION DISPLAY "N" END-ACCEPT` (and the DISPLAY twin), SG a
screen-name, `--std 2023`: `error COBOL2269: unexpected 'ON'. The ACCEPT statement ends before this token: no general
format of ACCEPT prints it after what is written …`. PB3830 had already folded the note's COBOL0307 + COBOL0901 into one
COBOL2269, but that message is false: the rendered Format 3 (PDF page 607) and Format 2 (page 640) bracket the AT phrase
and the [| ON EXCEPTION | NOT ON EXCEPTION |] group independently, so the unpositioned shape is the screen format and
docs/CONFORMANCE.md §5 promises it the named Annex A.4.2 refusal COBOL1707.

**Root cause.** The grammar bound the exception phrases to the positioning phrase (rule `screenTail`), on purpose: a
free-standing pair let an inner DISPLAY take an enclosing DELETE FILE's NOT ON EXCEPTION. That hazard is §14.5.3.3's
association question ("the next phrase of the most-recently preceding unterminated statement with which that phrase
may be syntactically associated"), and since PB3830 there is one place that answers it for every owner. The fix moves
the coupling there instead of keeping a grammar shape that rejects legal syntax.

**Fix.** `screenTail` is deleted; `acceptStatement` and `displayStatement` write `screenPositionPhrase?` and then their
own bare `conditionalPhrases`. `ConditionalPhraseAssociation` gains the two statements as owners. Their selectors rule
the screen format out where syntax can: a literal, function-identifier, inline-invocation or second operand, UPON, NO
ADVANCING, or ACCEPT's FROM (`Format.Unprinted`). The format is told from Format 1 by its operand's CATEGORY
(screen-name-1 vs identifier-1), which the parser cannot see. So a new `Format.ProvedBy` names the syntax that proves
it (the positioning phrase). Written without that proof the format is only PRESUMED, and a phrase joins the statement
only when no enclosing open statement may take it, the same last-resort rule the edition superset reading already
used (`Reading.Proved` / `Reading.Presumed`). A legal program never loses a phrase this way, and an inner screen
statement that yields its phrase is still refused by name. `AcceptDisplayBinder.ScreenFormatOf` treats an exception
phrase on a screen-name statement as the screen format with or without positioning. When the operand proves to be no
screen-name, the new `DeviceFormatPhrase` names the shape COBOL2269 (Format 1 prints no exception phrase, §14.5.3.3),
never COBOL1707: claiming the declined module would not make that program legal, so blaming the module would mislead.
The first gate caught the need for this: `ParseRecoveryTailTests` pinned `ACCEPT W-A ON EXCEPTION …` to one COBOL2269,
and the first cut answered COBOL1707. The row now expects COBOL2269 naming W-A, and gains a DISPLAY identifier twin
and a DISPLAY literal row.

The structural change touched one more place. Every ACCEPT / DISPLAY now ends in a `conditionalPhrases` context even
when it is empty, and two error-strategy walks that look for "the construct that ended just before the token"
descended into it. Those were the PB2499 mnemonic-suffix hint and the SEARCH ALL second-WHEN hint. Probed red:
`ACCEPT X FROM MYIN OF …` fell to an anonymous COBOL0307. They now descend through the new
`CobolErrorStrategy.LastWrittenRuleChild`, and `UnownedPhraseMessage` keeps `LastRuleChild` because the empty loop is
what it looks for.

**After.** `ACCEPT SG ON EXCEPTION …` / `DISPLAY SG ON EXCEPTION …`: COBOL1707 "'SG' is declared in the SCREEN SECTION,
and the ON EXCEPTION / NOT ON EXCEPTION phrase appears only in the screen format", with no parse error.
`ACCEPT WS-X ON EXCEPTION …` (no screen): COBOL2269 "'WS-X' is not declared in the SCREEN SECTION, so this is ISO
§14.9.1.2 Format 1 (device), which prints no exception phrase …". `DISPLAY "A" ON EXCEPTION …`:
COBOL2269 "this DISPLAY statement specifies a literal operand …". `CALL "X" ON EXCEPTION DISPLAY WS-MSG NOT ON EXCEPTION
… END-CALL` keeps the NOT arm the CALL's and runs it at 85 through 2023.

**Goldens.** `wiseowl/cases/14.9.1-ACCEPT/a42-accept-screen-exception` and `wiseowl/cases/14.9.11-DISPLAY/a42-display-screen-exception`
(refused COBOL1707 at 2002/2014/2023). `cases/14.5.3.3-Implicit-scope-termination/pb1528_device_operand_yields_phrase`
(accepted 85–2023; expected `1 EXC` / `2 EXC X=-` from §14.9.4.4 3) h) 1.). Unit tests in `ScreenFacilityConstructDriftTests`;
the nesting one was seen to fail with the presumed reading disabled. `ConditionalPhraseDriftTests` now classifies
ACCEPT and DISPLAY as "selector" and holds `ProvedBy` selectors to the owner's own syntax too. Rows: witnesses only
(FMT-14.9.1.2, GR-14.9.1.4-24, FMT-14.9.11.2, GR-14.5.3.3-4), no verdict change; GAP 20 → 20.

**Docs.** DESIGN-frontend-grammar §3.16 (the presumed reading, the ACCEPT/DISPLAY selectors, the error-walk
consequence), DATA_MODEL_DESIGN.md's screen paragraph, CONFORMANCE.md §4 item 4 and the §5 A.4.2 row (43 witnesses,
the stale `negative/…/.err` wording corrected).
