## Entry 3367 — 2026-11-09 00:41 PST — PB4995: a Message Batches run records its billing account on each batch, so one pass can spend two accounts' credit

The owner chose to run the whole 1985 recheck pass (1,040 requests at effort `max`, about $255) on account2's API credit.
Its canary had been billed to account1, and `message_batches.BatchRun` kept ONE account per run in `state.json`:
`api_key.read_run_key` refused every later command naming another account, because a batch belongs to the account that
created it and only that account's key reads it back.

That rule is true of a batch, not of a run. Each batch record (and a create's intent) now carries the `api_account` that
created it, and the run-level field is gone. `BatchRun.pinned_account` is the account of an uncollected batch: while
one exists every command must bill it (`read_run_key`, unchanged); once every batch is collected a command may name
another account, and an unnamed one bills the account of the run's newest batch. `collect` no longer retrieves a batch
it already collected, which another account's key could not read. `cost_by_account` prices every recorded reply under
its batch's account, and the `status` line of `audit_batch.py` and `transcribe_pdf.py` prints each account's spend
(`billing_line`). The transcriber's self-test bills one run over two planted accounts' fake servers (`FakeBatches`
takes an id prefix, as two accounts' batch ids never collide).

The 23 run state files under `E:/COBOL-coord/scratch` were moved to the new shape once (the run account copied onto each
batch, and the older bare-page usage keys keyed under the batch holding the page); every run's recorded cost is the same
to the cent. The recheck was then submitted on account2 (`msgbatch_018ansVms2yKV3PJYVWHXWZR`, $320 cap).

