## Two orchestrator leads filed (PB4038, PB4039) and a PB4011 recurrence

The 2026-10-28 operator session left three leads at quit. Each was checked against the register first.

- **PB4038.** A loop unit never checks its own handoff against `handoff.schema.json`, and `units/common.md` states
  only the summary's cap. On 2026-10-28 an enqueue unit wrote a 426-character `next_unit_reason`, over the
  schema's 400. The supervisor set the handoff aside and counted FAILURE 1 of 3 against a unit that had done its work.
  The sweep of `E:\COBOL-coord\logs` found 40 more invalid handoffs, all from failed meter units that wrote
  `meter_reading: null`.
- **PB4039.** `impact_store.vanished_after_the_glob` stubs five `Path` calls, but its self-test arm checks only
  three. Removing the `lstat` or `is_dir` stub would leave the self-test green. This is the low review lead R1-1 on #734.
- **PB4011 recurrence.** #734's finisher wrote the same space-separated `Head:` header that PB4011 records. It parsed
  correctly only because no prose `head <sha>` came before it.
