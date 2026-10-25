## PB3724 and PB3725: a notes-only gate waits for a full gate slot, and merge times went unrecorded

A two-file notes branch waited more than 10 minutes for one of the 3 gate slots, with later tickets passing it,
to run about a minute of script self-tests (PB3724). The PROGRESS line read merged 0 while three pull requests had
merged, because only the enqueue unit runs `landing_measure.py complete` and PB3639's lane lands during every unit
(PB3725, also a paragraph in PB3722). The operator completed the records by hand. Both are tier 1 of PB1957.
