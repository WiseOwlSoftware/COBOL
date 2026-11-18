## Lead: the doc comments still say "directive encounter" after the DirectiveEncounter -> StateEncounter rename (step 4a, review round 2, R2-1, low)

Source: review-LS4a round 2, `E:/COBOL-coord/scratch/reports/w20262104d-PB5925-report.md` line 63. No wrong output; a comment that misleads a reader.

**Sites:** `src/WiseOwl.Cobol.Frontend/Preprocessor/ConditionalCompilationResult.cs#KeyImplicitOps` (line 40), `ConditionalCompilationProcessor.cs` (lines 93, 155),
`src/WiseOwl.Cobol.Frontend/Pipeline/Frontend.cs` (lines 131, 339), `Preprocessor/ImplicitFormatOps.cs` (line 16),
`Preprocessor/ExceptionPerformDirectiveScope.cs` (line 22).

Round 1 (R1-1) renamed the keying from "directive encounter" to "state encounter" (the implicit ops are keyed to the next place the conditional-
compilation state is encountered, which a directive is one of). `git grep -n -i "directive encounter\|first directive written" -- src` on origin/main
0d4042678a still finds seven sites that describe the keying as to directives: `KeyImplicitOps` says each op is keyed "to the first directive written on
a LATER line (a directive occupies its own line, so no directive shares an op's line)"; the processor, `Frontend.cs` and `ImplicitFormatOps.cs` say
"directive encounter(s)". (The seventh, `ExceptionPerformDirectiveScope.cs:22`, is a sibling the lead did not list: "keyed to its directive encounters".)

Fix: reword each to the StateEncounter term the code now uses and re-read the `KeyImplicitOps` sentence against what the key really is. A comment
fix only; sweep `docs/` and `DESIGN-listing.md` for the same words in the same change.
