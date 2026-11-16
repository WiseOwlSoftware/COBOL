## Three notes and three finding files from the Mythos lane's batch-10r rulings and an oracle flake, 2026-11-16

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed, and a Sites block.

- PB5860 (defect, MAJOR): a variable-length record sequential file written with ADVANCING, or under LINAGE, cannot be read back through the file description that wrote it: OPEN INPUT answers 39 and the READ 47 (every edition); decided as a bound on the fix: every file a program can write it can read back, by one method stated once in the register.
- PB5861 (defect, MINOR): at `--std 85` an abbreviated combined relation condition with NOT NOT before a relation character (`IF A > 1 AND NOT NOT < 5`) is refused, where the 1985 text reads the first NOT as the logical operator and `NOT <` as the relational operator; the landed kb/Work PB5123 made the refusal.
- PB5862 (defect, MAJOR, `process_only`): `ArchOracleDriftTests.AnObservation_IsPortable_AndRepeatable` fails without a regression: two observations of one case disagree.

Three dated finding files beside existing notes: `kb/Work/PB5123/2026-11-16-the-refusal-is-wrong-at-1985.md`, `kb/Work/PB5683/2026-11-16-a-second-read-finds-what-the-first-missed.md`, and the ruling copied unchanged as `kb/Work/PB5683/2026-11-16-rulings-code-reads-batch-10r.md`.
