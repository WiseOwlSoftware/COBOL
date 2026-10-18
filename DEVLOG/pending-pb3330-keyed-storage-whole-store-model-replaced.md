## PB3330: the owner rules the keyed organizations' whole-store model unacceptable

The operator explained PB1636's last unsigned derivation and quoted the design doc's recorded weak spot: under
`SHARING WITH ALL OTHER`, every changing statement rewrites the whole indexed or relative file, so a bulk load there is
quadratic. The owner answered (2026-10-17): "THis is a completely unacceptable design".

The quoted cost is one symptom of the model, and kb/Work PB3330 records the model as measured on origin/main 20161db10:
- OPEN reads the whole file into memory (`KeyedConnector.ReadImage`), so reading one record means reading the file.
- The file is written back whole: at CLOSE, or after every changing statement under ALL OTHER.
- A file is capped at `RecordFraming.MaxStoreBytes` (about 2 GiB, .NET's `Array.MaxLength`), which DOC-A.1-107
  documents as the boundary.
- A process that dies before CLOSE loses every change since OPEN to every keyed file. §9.1.18.5 rolls back only the
  files under an active APPLY COMMIT.

GnuCOBOL 3.2, read the same day, stores RELATIVE as fixed slots it seeks to in place and INDEXED in Berkeley DB B-trees
or an ISAM library. PB3330 sets the bar for the replacement: record-level reads and writes, O(log n) and O(1) per
statement, a size limit from the file system, crash-consistent statements, coherence across run units at record or page
granularity with today's record locks, measured performance tests and one engine. The design belongs to the Mythos lane.
`DESIGN-runtime-library.md` now names the ruling where it had called a record-level format "the next step if that cost is
ever measured to matter".

The design followed the same day (branch `design-pb3330-keyed-storage-20261017`), as `DESIGN-runtime-library.md` §2.2.1,
from a survey read at the sources (GnuCOBOL 3.2's `fileio.c`, Berkeley DB's reference, IBM VSAM, Micro Focus, ACUCOBOL
Vision, Informix C-ISAM) and a measured prototype (a 4 KiB write 7-8 µs, an fsync 2.9-3.1 ms, today's 64 MiB whole-store
persist 77-92 ms per changing statement). It decides one paged data file of 4 KiB pages (store format 5, no sidecar), B+
trees for the prime key, each alternate key as (key, release ordinal) composites and the relative record number, with the
PB2754 frame codec as the leaf value; shadow paging with a generation ledger and stamped pointers for crash consistency (no
page either meta slot references is ever overwritten, every pointer carries its page's generation, two committed slots
written alternately, freed pages quarantined until promotion, a bounded verification at OPEN under the store mutex, no log
and no rebuild utility); coherence across run units by one meta-page read and a cache drop under the existing statement
mutex, for every sharing mode but NO OTHER; the locks unchanged plus one transaction-liveness byte region; the boundary as
the host's file size capped at 2^32 pages; COMMIT and ROLLBACK as per-run-unit transactions with record-level before-images
kept in the file; and count-based performance witnesses. Every citation passed `cite.py --check` (one inherited quote was
re-derived from §12.4.5.6.4 to §14.9.30.4 GR21 c)). The work is filed as kb/Work PB3331-PB3338, sequenced by `blocked_by`;
PB3330 lands with the last of them, and it carries one owner question (the RELATIVE O(1) bar against one engine). The
DOC-A.1-107 and DOC-A.1-75 rows are marked as the groups' targets and still describe the current compiler.

The branch review (round 1) found five findings, three high, and the owner chose a finisher the same day ("Fable today").
The first draft had rewritten a fresh page in place (a process death mid-statement then lost every statement since the last
promotion), overwritten the committed meta slot in place (a torn promotion had no outcome), run crash recovery at every OPEN
including on a file another run unit held open, and made COMMIT and ROLLBACK whole-file operations (one run unit's ROLLBACK
discarded another's completed statements). The corrected §2.2.1 derives the commit scope from §14.9.7.1, §14.9.36.4 GR3,
§9.1.17 and Annex D.2.5 and a source survey (GnuCOBOL unlocks only; Micro Focus Fileshare rolls back one program's
transaction), and the notes were swept to it.

The review's round 2 found seven more, three of them contradictions the round-1 fixes had introduced, and the owner chose a
finisher again ("Fable today"). The corrected promotion had never rewritten the work slot, so every OPEN after a CLOSE
rejected it and reclaimed the committed tree's own leaves; a rejected history's generations could be stamped again, so a
lost write could pass the stamp check; an INPUT reader of a crashed file walked the tree its OPEN had rejected; a record a
transaction changed without reading it stayed unlocked, so ROLLBACK still overwrote another run unit's change (the harm
round 1 had half fixed); a rollback could re-insert a unique alternate key value another run unit had taken; the group
split regressed a READ ONLY relative writer between two landings; and the deletion list deleted a predicate four sequential
callers need. §2.2.1 now has the retirement statement after every promotion, a generation ceiling and a fence at every
writable OPEN (no (page, generation) pair is ever written twice), coherence that follows page 2 from the adopted meta,
transaction locks on every changed record and every freed unique key value (derived from §9.1.16, §12.4.5.9.4 GR4,
§14.9.51.4 GR11, §14.9.10.4 GR7 and D.2.5, with the commit posture exempt from the lock ceilings), the coherence condition
in the first per-statement-writing group, and §2.2.1.15, a table of every structure's writers and readers for the next
reviewer.

Round 3 found four more (three high), and the owner chose a finisher required to attack its own fixes before returning
("Fable + self-attack"). A dead owner's locks went with its handle while its rollback waited for the next writable OPEN, so a
live writer changed its records in between (both Micro Focus Fileshare and IBM VSAM RLS, read at the sources, keep a dead
holder's records untouchable until the rollback): the DEAD-OWNER STEP now runs at the entry of every statement through a
writable handle. A live run unit continuing a dead statement's history without an OPEN re-stamped its generation: a changing
statement now CLAIMS its generation on page 2 (`stamp mint`) before its first page write. The promoted ledger's own pages are
quarantined, as R4 said, not freed; the DELETE NOTE is cited at rule 15. The self-attack's two tables, §2.2.1.16 (24 crash
points) and §2.2.1.17 (18 two-run-unit interleavings), found five more gaps and closed them: a promotion is restartable
through a PROMOTING flag, reads both committed slots before it writes, and the fence's claim rewrites page 2 to the adopted
meta; a retirement's own reserved pages are fresh; an unacknowledged COMMIT lands whole or not at all.

Notes and docs only. No code change.
