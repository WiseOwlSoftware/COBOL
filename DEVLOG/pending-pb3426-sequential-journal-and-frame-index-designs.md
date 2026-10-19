## PB3426: the tier-2 design memos — a SEQUENTIAL file under APPLY COMMIT (2.2.2) and the sequential frame index (2.2.3)

The owner's "begin" (10:12 PDT) started kb/Work PB3420: the operator's loop runs tier 1 of the SORT and file I/O plan on
PB3353, and the Mythos lane owes the two tier-2 design memos. Both are now sections of
`docs/rearchitecture/DESIGN-runtime-library.md`, written before their implementers and followed by them (CLAUDE.md rule 2),
with every citation checked by `cite.py --check` (26 of 26).

**2.2.2 — the transaction journal (PB3340).** Derived from the whole list of a sequential file's changes (an append, an
in-place same-size REWRITE, an OPEN OUTPUT's truncation, a DELETE FILE), the state at the last COMMIT is the committed length
L, the first before-image of each rewritten ordinal, and one before file for an OPEN OUTPUT or DELETE FILE; a rollback
restores the before file, re-applies the images and truncates to L, idempotently. The transaction HOLDS THE END OF THE FILE —
a sequential record's identity is its ordinal, so only a truncation keeps every other connector's identities true, and only
an END holding makes a truncation an undo (§9.1.16's latitude; another connector's WRITE answers '51' with RETRY, §14.9.51.4
GR16). The journal lives in the file's `.wolk-*` lock directory as a transient file (the format is headerless by design;
memory-only undo cannot roll back a killed run unit, §9.1.18.5), with four write-ahead rules, one kept handle under commit
control for the §9.1.15 file lock and the liveness byte, COMMIT in 2.2.1.10's three regions (the decision byte's sync is the
one durable write), ROLLBACK as one idempotent sequence under the file's mutex, the dead-owner step at every writable OPEN,
LINE SEQUENTIAL and the print stream covered, and a crash-and-interleaving table over an `IHostMedium` seam so every row is a
test (the owner's simulator-from-round-one rule). **One owner decision is open, recorded in PB3340:** kb/Work PB802's "no
sidecar of any type" (the attribute catalog) may or may not cover a transient journal; 2.2.2.11 states the memory-only
alternative and what it loses, and nothing of 2.2.2 is implemented until the owner answers.

**2.2.3 — the frame index (PB3350).** One walk, two products: `CountFrames` returns a count and stores nothing; `FrameIndex`
keeps one checkpoint per 4096 frames plus a window of at most 4096 exact starts, built for free by the forward walk, so the
backward READ and OPEN REVERSED hold O(records / 4096 + 4096) memory instead of one offset per record, a whole-file backward
pass stays O(records) head reads, and OPEN REVERSED pays one pass instead of two. A trailing length word (a format change),
a walk from 0 per step (quadratic) and the lazy whole index (today's defect) are rejected with their reasons.

PB3340 and PB3350 point at their sections; PB3426 is the analysis note of the memos, landed with them. Branch
`notes-pb3426-io-design-memos-20261019`, docs and register only; the Mythos lane dispatched nothing.
