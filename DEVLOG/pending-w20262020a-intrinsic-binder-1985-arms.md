## COBOL-85 arms of the function-identifier, EVALUATE, DEBUG-ITEM, INSPECT sizes and the INTO/FROM citations (PB5163, PB5183, PB5209, PB5297, PB5425, PB5352)

Wave 20262020 group A. This branch finishes w20262014a (whose implementer died on an API 403): it merges
`worktree-wf_7021f6f6-0af-1`, which carries PB5230 (the repository drift tests leave Unit), PB5270, PB5309 and the w20262013a
Report Writer group (landed separately as PR 1599 and merged back from main). The merge took main's side of the dead finisher's
uncommitted PB5378 halves, because main had already landed PB5378's review answers; its shared C# lexer
(`scripts/csharp_lexer.py`) passes its seven cases and `buildgraph.py --self-test` (36 arms). One merge fix-up: main's PB5196
drift test reads the 2002 and 1985 texts from Unit, so `scripts/build_targets.json` declares both (BUILD TARGETS was RED, now
GREEN). Every note below was re-probed on the branch's own build first; each reproduced.

**PB5163.** `FUNCTION RANDOM ()` and `FUNCTION CURRENT-DATE ()` compiled at `--std 85`. The edge is 2002, not 2023: ISO/IEC
1989:2002 8.4.2.2.1 brackets the argument inside the parentheses and its SR4 NOTE writes `FUNCTION RANDOM ()`. New construct
`function-empty-argument-list-2002` (COBOL0900), gated by recognition in `VersionConformancePass.ParseArm.VisitFunctionCall`.

**PB5209.** `FUNCTION MAX (TE (ALL) (2:1))` was refused COBOL2363 at every edition: `TryBindAllArgument` gave up on any suffix
after the subscripts. It now takes the modifier through the one reference-modification tail (`ScreenRefModCount`,
`ReadScreenedRefMod`, `RefModView`) over the element's place, so the backend's enumeration modifies each occurrence
(§1985:IV.4.3.8.4.4 GR3). Goldens at 85 and in the 2023 tree.

**PB5183.** At 85 an EVALUATE object no WHEN phrase reached was never evaluated, against §1985:VI.6.13.4 GR1 ("operates as if each
selection subject and selection object were evaluated"). New `DialectBehavior.EvaluateObjectsInWhenPhraseOrder` (2002, VCR row
7.68): where it is inactive, `EvaluateBinder.Bind` holds the truth of each pair whose object references a function at the head of
the statement (`SendingValueTemp.MaterializeTruth`). Design D5 of docs/CONDITIONS_EXCEPTIONS_DESIGN.md records it.

**PB5297.** `MOVE "A" TO TE (DEBUG-SUB-1)` outside a debugging section compiled: the position is evaluated into a temporary by a
pre-op still pending when the funnel asked `ReferencesDebugItem`, which now asks those pre-ops too (COBOL6442).

**PB5425.** The 1985 INSPECT size rules skipped `W3 (2:)` and `FUNCTION UPPER-CASE (W3)`. `IntrinsicBinder.KnownWidth`, the one
static size reader, now reads an open reference modification's remaining positions and a function's fixed length, from a new
`IntrinsicSig.Length` column (`IntrinsicResultLength`: CHAR 1, CURRENT-DATE and WHEN-COMPILED 21, LOWER-CASE, UPPER-CASE and
REVERSE argument-1's length). Six refusals (COBOL0846), one control, `IntrinsicResultLengthTests`.

**PB5352.** A COBOL-85 READ INTO refusal cited ISO §2023:14.9.30.3 SR1. New `EditionCite` (a rule's 2023 citation and its 1985
one per I-O module); `IntoPhraseRules` and its twin `FromPhraseRules` cite the admissibility rule, its arms and the MOVE rule as
the edition numbers them. Three 85 refusals pin the message; `EditionCiteTests` pins the selection.

**Inventory.** 1985 rows re-verdicted CONFORMS through five `record_verdicts --edition 1985` batches (w20262020a-pb5163, -pb5209,
-pb5183, -pb5297, -pb5425): 1989-FMT-A.2.35.2, 1985-FMT-IV.4.3.8.3.2, 1985-SR-IV.4.2.1-4, 1985-FMT-IV.4.3.8.4.2,
1985-FMT-IV.4.3.8.5.2, 1985-GR-VI.6.13.4-1, 1985-SR-XV.3.2.3-11, 1985-SR-VI.6.18.3-7, -8 and -9 (1985 GAP 279 to 269). PB5373
stays open: its remaining rows wait for PB5374's tooling and PB5308.

**Self-review** (architecture, full code, performance, duplication): each rule is asked at the one place its siblings are (the
parse arm for the format gate, the reference-modification tail, the statement funnel, the one size reader, one citation type
for both phrase records); the EVALUATE hoist adds code only to an 85 pair whose object references a function; no second copy of
a rule was added. The gate's verdict is in the branch's report.
