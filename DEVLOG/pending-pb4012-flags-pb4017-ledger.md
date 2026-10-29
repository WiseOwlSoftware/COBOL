## PB4012's harm flags corrected, PB4017 filed, and the fuzz ledger regenerated after PB3996 and PB3997 landed

PB4012 (`--permissive` refuses OMITTED in a Format 1 CALL since #743) was filed with `process_only: true`, but a program that
compiled under `--permissive` is now refused. It now carries `rejects_legal_source: true`, and the body says "legal" means
legal under the owner's PB162 permissive contract, not under ISO.

PB4017 records the round-7 lead of PB4010's review: no self-test runs `main_verdict` end to end, and the extract's
completeness is checked only against its own list. The fix is an end-to-end case on a ScratchRepo holding the real `scripts/`.

The fuzz ledger is the first use of PB4010's sync rule. After #747 landed, `fuzz_history.py status` read PUBLISH OWED, and a
render at main showed main's FUZZ-LEDGER.md was stale: PB3996 and PB3997 landed (#743, #742) after PB4010's branch was cut, so
the file still showed them open. Regenerated here, it reads 24 compiler bugs found and 24 fixed, ledger version e301185cecb2.
After this lands, the Artifact is published from main's copy and marked.
