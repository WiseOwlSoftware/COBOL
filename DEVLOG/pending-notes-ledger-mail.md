## Notes: the ledger-publish mail goes to a fixed inbox (PB5394)

The operator session found that the loop's supervisor addresses every "Ledger publish owed" mail to the operator inbox whatever account the loop runs on, so on account 1 the mail reaches a session that cannot write account 1's artifact and the publish waited for a manual forward (2026-11-12, 12:04 PST). The lead is filed as kb/Work PB5394 (open, process-only, MINOR) with the two sites and the fix direction: address the message by `mailbox.account_lane` and have the test assert both accounts. No code changed.
