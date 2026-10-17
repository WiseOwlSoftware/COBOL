## PB3237: DELETE FILE ran its NOT ON EXCEPTION imperative after a USE declarative had handled the EC-I-O-WARNING its '05' raised

kb/Work/PB3237 (an R2 duplication finding). `DELETE FILE` of an absent file is successful with status '05' (ISO 14.9.10.4 GR14), and with `>>TURN EC-I-O-WARNING CHECKING ON` that status raises EC-I-O-WARNING (9.1.13.1). A USE declarative, or a PERFORM WHEN phrase, that handles it must stop the statement's NOT phrase from running ("the imperative-statement in that phrase is not executed", 14.6.13.1.4 2)/3), both checked with `cite.py --check`). kb/Work PB1120 had gated every other I-O NOT phrase on the hook's `UseHookResult.NotPhraseGate`; `KeyedIoEmitter.EmitDeleteFile` discarded the hook result and carried its own copy of the phrase-pair dispatch (a bare `else` / `Successful(st)`).

Re-probe on a fresh build: the report reproduced exactly (`DECLARATIVE RAN FS=05` / `NOT ON EXCEPTION RAN FS=05` / `AFTER DELETE FS=05`).

Fix shape: the pair dispatch now exists once, `SequentialIoEmitter.EmitPhrasePair(phraseCond, phrase, notPhrase, successCond, notGate)`. `EmitInvalid` (INVALID KEY on WRITE / REWRITE / DELETE / START) became a call to it, and DELETE FILE renders ON EXCEPTION / NOT ON EXCEPTION through it with the hook's gate. The NOT arm is `else if (Successful(st) && __iorN == DispatchResult.Normal)`; with no EC-I-O mask the gate is null and the text is the plain success test. The structural guard `NotPhraseGateDriftTests` fails when `KeyedIoEmitter` or `SequentialIoEmitter` reads a NOT phrase without reaching `EmitNotPhrase` or `EmitPhrasePair` (watched to fail on a planted ungated read). `docs/COBOLNET_FILES_DESIGN.md` names the single renderer.

Sibling sweep: every NOT-phrase read in `CodeGen/` was listed. The I-O emitters now all reach a gated renderer. `CallEmitter` (PB606), `StringEmitter` (overflow) and `SortEmitter.EmitReturn` (an SD file has no I-O status or EC-I-O hook) are other mechanisms and were left.

Goldens in `conformance-suite/cases/14.9.10-DELETE/`: `pb3237_delete_file_handled_warning_by_use_declarative_skips_not_on_exception`, `..._by_when_phrase_skips_not_on_exception`, and the control `pb3237_delete_file_unhandled_warning_runs_not_on_exception` (an unhandled warning still runs NOT ON EXCEPTION). The note claimed no inventory row, so it closes none.

Self-review (architecture, full code, performance, duplication; plus silent-failure and comment checks on the diff): the one duplicated dispatch is removed rather than patched; no hot-path allocation change (emit-time only); no diagnostic code used.

Gate (run 20261017T222844Z-6be823): `=== BUILD-LOCAL GATE: LEG 1 ONLY (batched gating, PB2515): GREEN` — Conformance 11,680/17,329, Unit 33,562/33,936, Characterization 5/36 cases ran; the three new goldens and `NotPhraseGateDriftTests` passed in leg 1. `semgrep/verify.py` PASS with no rule's count raised.
