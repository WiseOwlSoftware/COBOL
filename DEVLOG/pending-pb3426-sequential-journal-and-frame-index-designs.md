## PB3426: the tier-2 design memos — a SEQUENTIAL file under APPLY COMMIT (2.2.2) and the sequential frame index (2.2.3)

The owner's "begin" (10:12 PDT) started kb/Work PB3420: the operator's loop runs tier 1 of the SORT and file I/O plan on
PB3353, and the Mythos lane owes the two tier-2 design memos. Both are now sections of
`docs/rearchitecture/DESIGN-runtime-library.md`, written before their implementers and followed by them (CLAUDE.md rule 2),
with every citation checked by `cite.py --check` (28 of 28 after the correction).

**2.2.2 — the transaction journal (PB3340).** Derived from the whole list of a sequential file's changes — an append, an
in-place same-size REWRITE, an OPEN OUTPUT's truncation, a DELETE FILE, and a creation (§14.9.27.4 GR17, GR18) — and from
the ORDER they run in, a transaction has two phases: BASE, the committed file with appends past its committed length L and
rewrites below it, undone by the first image of each rewritten ordinal and a truncation to L; and REPLACED, after the first
capture (the file copied whole to a before file) or from the start when the file was absent, in which nothing more is
journaled and the rollback restores the before file or removes the file. The transaction HOLDS THE END OF THE FILE (a
sequential record's identity is its ordinal, so only a truncation keeps every other connector's identities true, and every
live transaction's rollback truncates to its own L; §9.1.16's latitude, '51' with RETRY to another connector's WRITE), the
rewritten ordinals, and the whole file once REPLACED. Three transient files per transaction in the file's `.wolk-*` lock
directory — the holder (`SidecarLockFiles`' primitive on every host: the liveness, since the file itself may be removed or
not yet exist), the readable record (header, `R`/`K`/`D` records with CRCs, a torn tail ending them) and the before file —
an entry mutex in the same directory (`EntryMutexByte` through `SidecarRegionLocks` on every host), five write-ahead rules,
one kept handle under commit control for the §9.1.15 file lock (closed by a DELETE FILE, opened by a creation; the capture
is a copy, never a rename), COMMIT in 2.2.1.10's three regions with the directory synced before the decision, ROLLBACK as
one idempotent settle run by the owner or the next entry, LINE SEQUENTIAL and the print stream covered. **The section was
corrected the same day after the branch review** (kb/Work PB3426 R1-1: a REWRITE journaled after a capture and re-applied
over the restored file; R1-2: creation omitted, so a rolled-back created file was left empty instead of removed), and the
correction is held by an EXECUTABLE MODEL the owner's rule of 2026-10-17 requires from round one:
`scripts/models/sequential_journal_model.py`, a self-test every gate and CI run, executes the protocol over a medium that
refuses, kills or loses power at every operation with a second run unit at every operation boundary — 195,400 runs at
depth 4 (2.3 million at depth 6 by hand), RED with either finding put back. Re-deriving under the model also removed a
rename the kept handle made impossible on Windows, moved the liveness and the mutex off the file's region (which a DELETE
FILE removes), and made a refused sync of a handed-over record fatal. **One owner decision is open, recorded in PB3340:**
kb/Work PB802's "no sidecar of any type" (the attribute catalog) may or may not cover a transient journal; 2.2.2.11 states
the memory-only alternative and what it loses, and nothing of 2.2.2 is implemented until the owner answers.

**2.2.3 — the frame index (PB3350).** One walk, two products: `CountFrames` returns a count and stores nothing; `FrameIndex`
keeps one checkpoint per 4096 frames plus a window of at most 4096 exact starts, built for free by the forward walk, so the
backward READ and OPEN REVERSED hold O(records / 4096 + 4096) memory instead of one offset per record, a whole-file backward
pass stays O(records) head reads, and OPEN REVERSED pays one pass instead of two. A trailing length word (a format change),
a walk from 0 per step (quadratic) and the lazy whole index (today's defect) are rejected with their reasons.

PB3340 and PB3350 point at their sections; PB3426 is the analysis note of the memos, landed with them. Branch
`notes-pb3426-io-design-memos-20261019`: docs, the register and the model script, no compiler source; the Mythos lane
dispatched nothing.
