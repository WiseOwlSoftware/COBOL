## The 3,000-program Sonnet fuzz batch: seven WiseOwl defects filed, and one performance lead

The first large Claude-written differential-fuzz batch (kb/Work PB4506: 3,000 programs written by Sonnet 5.5 through one
Message Batch, judged locally against a judge built from main 3c8b2845c, triaged by 14 Opus adjudicator slices, all billed
to the expiring API credit; about $59 to write and $67 to triage) found seven WiseOwl defects. Each was adjudicated
against the standard and its reduced repro re-run on the fresh judge:

- **PB4511** (wrong answer): TEST-NUMVAL / TEST-NUMVAL-C report the position of the C or D of an incomplete CR/DB.
- **PB4512** (wrong answer): INITIALIZE ... TO VALUE edits an edited item's VALUE a second time.
- **PB4513** (wrong answer): Report Writer with overlapping controls corrupts the user's control item at a break.
- **PB4514** (wrong answer): de-editing a floating '+' item drops its minus.
- **PB4515** (wrong answer): an all-'*' / all-'Z' picture whose stored value is zero after truncation is edited as nonzero.
- **PB4516** (wrong answer): FUNCTION EXP10(-6) stores 0.000000 or 0.000001 depending on the statement's shape.
- **PB4517** (rejects legal source): PRESENT WHEN with a function-identifier is refused as not yet implemented.

**PB4518** is the PB2293 wave-2 lead: OoDriver's element binders rebuild the storage-claim scan the bind session already holds.

Validity, the owner's question (the share of programs WiseOwl compiles, as the 47.1% baseline of 2026-10-29 counted it):
68.0% for Sonnet against 47.1% for the local model (§13.18 49.3% vs 26.9%, §14.9 70.6% vs 32.2%, §15 74.1% vs 62.7%,
§8.8 69.4% vs 48.5%), with no correction round for Sonnet; both compilers rejected 15.0% against 36.6%.
