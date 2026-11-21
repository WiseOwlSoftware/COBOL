# PB5902: the CCVS-85 program SQ101M writes ADVANCING ZERO (wave 20262115, group A, 2026-11-20)

**Not fixed in this wave. The fix would turn the NIST guard red, and a decision about the corpus has to come first.**

The re-probe confirms the note. `advz.cob` (`WRITE PREC AFTER ADVANCING ZERO LINES.`) at `--std 85` exits 0 with no
diagnostic (run `E:/COBOL-coord/scratch/fleet-1985-rulings/w20262115a/runs/advz-001`, tree a2bd139087). `sendz.cob`
(`SEND OUTCD FROM M WITH EMI AFTER ADVANCING ZERO LINES.`) also compiles at 85 (`runs/sendz-002`). That is the SEND
sibling, which the ruling had read but not run.

**The conflict the ruling did not weigh.** The NIST CCVS-85 program `tests/nist/programs/SQ101M.cob` line 113700 writes
`WRITE PRINT-REC BEFORE ADVANCING ZERO.` (test WRT-TEST-GF-25, feature "WRT BEFORE ADV ZERO"). Its comment at lines
078900-079100 describes the related test as "ADVANCING ZERO LINES AFTER PRINTING". `tests/nist/corpus.tsv` row 265 holds
SQ101M as `divergent valid`, and its note cites WRT-TEST-GF-25 as BEFORE ADVANCING ZERO. The NIST golden run compiles
at `--std 85` by default (`CorpusManifest.GoldenRunEdition`, `WISEOWL_COBOL_NIST_STD` default 85). The ruling's fix,
refusing ZERO as integer-1 at 85, would therefore make SQ101M fail to compile, and the NIST differential and the CLI
NIST guard would both turn red. The implementation was written and then withdrawn
(`FigurativeAssociation.ZeroStandsForInteger`, which both ADVANCING screens asked) when this surfaced. No gate ran on it.

**Why this goes back to the ruling and does not land as written.** CLAUDE.md rule 1 makes NIST a regression net and not
an authority, so this does not overturn the reading of §1985:III.2 and §1985:IV.4.2.2.2.3 by itself. CCVS-85 is, however,
the validation suite that was written against X3.23-1985 itself. A validation suite that exercises `ADVANCING ZERO` as a
feature is evidence about the same text, and the ruling (calibration medium) and its refuter did not consider it. Landing
the refusal also needs a corpus decision that no fix lane can make alone: SQ101M would have to leave the green set,
since NIST sources are never edited, and its other WRITE ADVANCING tests would lose their coverage at 85.

**What the decision needs:** (1) the ruling session weighs SQ101M WRT-TEST-GF-25 against its reading. (2) If the refusal
stands, it decides how the NIST corpus records a CCVS-85 program that the 1985 text, read this way, rejects. Once both
are settled the withdrawn shape is ready: one question in `FigurativeAssociation` asked by
`SequentialIoBinder.ScreenAdvancingCount` and `CommunicationBinder.ScreenSendAdvancingLiteral`, a 1985 citation in each
message, and the 85 half of the `pb5289_send_integer_operands_85` witness flipped to a refusal.

**Sites:**
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/SequentialIoBinder.cs` (`SequentialIoBinder.ScreenAdvancingCount`: `case BoundFigurative { Kind: 'Z' }` at every edition)
- `src/WiseOwl.Cobol.Compiler/Binding/Procedure/Verbs/CommunicationBinder.cs` (`CommunicationBinder.ScreenSendAdvancingLiteral`)
- `tests/nist/programs/SQ101M.cob` line 113700, `tests/nist/corpus.tsv` row SQ101M
