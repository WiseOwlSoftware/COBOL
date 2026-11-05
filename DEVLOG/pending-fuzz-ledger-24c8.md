## Re-render the fuzz ledger at version 24c8d12f80a5

The session probe reported the fuzz ledger owed a re-render: main's notes rendered version 24c8d12f80a5, but
`FUZZ-LEDGER.md` on main still carried ab9d4b61c372. `python scripts/fuzz_history.py render` rewrote it. The GnuCOBOL
bug count rose from 133 to 135, and the filing-day table now shows 43 notes on 2026-11-03 and 6 on 2026-11-04. Nothing
else changed. After this lands, the Mythos lane, which owns the Artifact, publishes the page and runs
`fuzz_history.py mark-published`.
