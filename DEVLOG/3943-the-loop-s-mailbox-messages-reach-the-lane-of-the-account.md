## Entry 3943 — 2026-11-22 17:41 PST — The loop's mailbox messages reach the lane of the account running it (PB6094)

Account 1 ran the orchestrator loop today. Its supervisor's "ledger publish owed" message and its land lane's "landing
stalled" alert both went to the `operator` inbox, which is account 2's. Account 2 cannot publish account 1's ledger
artifact, so it had to relay both. `mailbox.py send --to account` now resolves to the sending process's
`account_lane`. The supervisor and the land lane's alerts use it (`notify_operator` is renamed `notify_account`).
Mailbox self-test 50/50 OK; `land_lane/test_tick.py` PASS, 30 cases.

