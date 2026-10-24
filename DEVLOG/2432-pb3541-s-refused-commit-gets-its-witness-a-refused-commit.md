## Entry 2432 — 2026-10-23 20:09 PDT — PB3541's refused COMMIT gets its witness: a refused COMMIT deactivates nothing, and item 8's ROLLBACK restores the file before it deactivates the clause

PB3541 landed through the loop's branch (entry 2426), which carried this branch's work-in-progress commits but not its
branch-review fix R1-1. This landing adds that one witness and nothing else:
`TransactionControlTests.A_refused_commit_deactivates_nothing_and_item_8_restores_the_file_then_deactivates_the_clause`
(RELATIVE and INDEXED). A clause naming F is retired, the medium refuses the COMMIT at F's first page write, and the test
asserts the refused COMMIT deactivated nothing: F is still under commit control and the live clause saved nothing (the
deactivation of §14.9.7.4 GR3 and the save are the COMMIT's effects, and a refused COMMIT made nothing permanent). The
§14.6.13.1.3 item 8 ROLLBACK, due because F is still *"a file subject to an active APPLY COMMIT clause"*, then restores F,
the other controlled file G and the live clause's data items (§14.9.36.4 GR3), and only after that deactivates the retired
clause (§14.9.36.4 GR6). The test fails on a mutant that runs `TransactionControl.DeactivateRetired` before the refusal
check: F would be released first, item 8 would find it under no clause, and its changes would stay in the open transaction
for the run unit's end to make permanent. The PB3541 note's Landing section names the witness.

