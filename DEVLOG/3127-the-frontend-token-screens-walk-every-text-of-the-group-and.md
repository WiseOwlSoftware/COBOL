## Entry 3127 — 2026-11-04 18:34 PST — The frontend token screens walk every text of the group, and position a report by the token's region (PB4649)

PB4649 was a lead from PB4483's review: a COBOL-2023 word-character removal (COBOL0902, a warning under `--permissive`) in a
repository skeleton printed when its expansion was placed in an assembly of its own and never when it was placed in the group's.

**Re-probe** (`99022cc7e`, `--std 2023 --permissive --source-format free`): a skeleton HOLD declaring `01 ABC` + U+30FB `PIC X.` expanded
over a repository class BOX (placement (a)) printed `CLIENT1.cob(8,5): warning COBOL0902 ... [in the expansion 'HOLD-BOX']`; the same
skeleton expanded over the client's own LBOX (placement (b)) printed nothing.

**Root.** PB4483 put every parse-tree screen on `GroupBindContext.Texts`, but `BinderDriver.Bind` still ran the two frontend token
screens, `LiteralScreen.Run` and `WordCharacterScreen.RunOnTree`, over the written tree alone; and both positioned a report with
`sink.At(token.Line, token.Column)`, which carries no `DiagnosticRegion`, so even over an expansion's tree the report would have been
printed at a position in a file it never was in and the group could not have withdrawn it.

**Fix.** (1) `ExpansionToken` moved from `Compiler/Oo` to `Frontend/Common` (public) and gained `ExpansionToken.CursorOf(IToken?)`, the ONE
token-to-cursor rule (it was `DiagnosticCursorAt.CursorOf`, deleted; `DiagnosticCursorAt.At(IToken)` and the binder driver's local ask the
new one). (2) `WordCharacterScreen` keeps a `DiagnosticCursor` in its `ScreenedWord` and `LiteralScreen` positions its three reports by
`CursorOf`. (3) `BinderDriver.Bind` runs both over each member of `ctx.Texts`; `RunOnTree` builds its once-per-word set per call, so the
rule is per text, as PB4483's R1-1 made `ParseArm`'s. The literal screen reports nothing of a repository text today (its edition gates
are introductions, errors under every flag, and its syntax rules are errors that a published skeleton passed under the same state); it
is walked so that a warning-severity gate needs no new site. DESIGN-external-repository section 10.5 and DESIGN-frontend-grammar updated.

**Sweep.** `grep -rnE "\.At\([^)]*(Line|Column)" src` leaves only directive-line positions (`TurnState`, `DirectivePlacementPass`),
which are not text of an expansion; `LiteralScreen.Run` and `WordCharacterScreen.RunOnTree` have no other caller in the compiler.

**Witnesses.** `ExternalRepositoryExpansionTests.ARemovedCharacterOfAnExpansionText_IsReportedAtTheEntry_InEveryPlacement_AndOncePerText`
(placement (a), the import, placement (b), and the word also written in the client; red when the call walks the written tree alone),
`DiagnosticRegionTests.AFrontendTokenScreen_AttributesADiagnosticOfAnExpansionsText_ToItsRegion` (both screens, an error and a withdrawn
warning), and two guards in `GroupTextScreenDriftTests` (the screen calls take `text`; no `sink.At(token.Line, token.Column)`).
No inventory row is owned (`closes_rows: []` with its reason).

