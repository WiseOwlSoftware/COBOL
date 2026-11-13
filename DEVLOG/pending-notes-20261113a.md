## Notes batch 20261113a: PB5462 filed, the NIST console-output basis is a third normalizer no parity table holds

One note is filed from the branch review of the PB5460 fix (wave 20262015 group A, verdict CLEAN), which recorded two leads the fix did not take up.
- PB5462 (LOW, test-harness, process-only): `NistOutputs.StdoutBasis` in `NistRunner.cs` and its shell twin in `scripts/guard-verdict.sh` normalize console output by a rule that differs from `Normalize` / `guard_normalize_into`, and no parity table holds it (lead R1-1).
- The same note carries lead R1-2: the shell's `COMPUTED=` mask has no `/g` and covers the first operand on a line, where the C# mask covers every one.
- Every statement about the code is the reviewer's and the implementer's reading, a claim until the fixer re-reads it. No wrong verdict is known; the note files the harm of PB5460 waiting again.
- The shape the note names is one table, `tests/nist/normalization-cases.json`, read by every normalizer either arm applies.
