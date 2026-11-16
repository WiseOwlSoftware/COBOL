## Entry 3760 — 2026-11-16 06:47 PST — Two process notes from the Mythos lane's findings of 2026-11-16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Both are process defects (`process_only: true`, no harm flag, severity MAJOR, area tooling/inventory) in the code-read store, each with a Sites block.

- PB5863: a fix that renames a member a stored code read cites has no stated remedy, and an implementer improvised a false re-import; decided: the read is withdrawn, the rule is written where an implementer reads it, the tool withdraws unresolved reads in one command and refuses an impossible pin.
- PB5865: the code-read store refuses a truthful DOCUMENTED-NON-SUPPORT read and a DOUBTFUL with no corrected verdict, and would count the first as a contradiction; decided: the defect fields are required only of a verdict that does not resolve, and a read confirms when its verdict is the one the row shares.

