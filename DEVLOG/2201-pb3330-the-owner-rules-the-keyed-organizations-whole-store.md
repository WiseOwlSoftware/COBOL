## Entry 2201 — 2026-10-18 04:54 PDT — PB3330: the owner rules the keyed organizations' whole-store model unacceptable

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

Round 4 found three more (one high), and the owner chose the same finisher with the same self-attack rule. An owner killed
AFTER another writer's entry probe and BEFORE that writer's lock tests had its region locks released with its handle, so the
tests passed and the next entry's rollback overwrote a completed statement: no arrangement of probes closes a window between a
probe and a test of a different lock, so a transaction's locks are now PAGES OF THE FILE — one HOLDINGS tree, written by the
statement that makes the change — and a writer that meets a holding probes its owner at the test itself (IBM's retained
locks, kept by the medium). A first change refused part-way kept its liveness byte while page 2 still offered its id, and a
NO OTHER connector's CLOSE re-stamped a refused statement's page: every identifier the format mints is now CLAIMED in the
store's memory before any page carries it (and on page 2 under the mutex), a transaction id is DRAWN by its byte, and a
refused first change gives the byte back. The self-attack's two new tables — a holder's death at every point of another
statement (11 rows) and every statement refused part-way with its continuers (14 rows) — found that a COMMIT refused after
its removal statement reported failure with the transaction gone: a COMMIT is now decided by its promotion's one durable
write (`committing` in the claim and the slot; the retirement removes the entry), and a promotion's restart rule reads both
slots for a flag left by a dead or a refused promoter (landed: complete; unlanded: run again, or unwind a COMMIT).

Round 5 found two more (both medium), and the owner chose the same finisher again. A COMMIT whose sync after the slot
write was refused could be answered '30' while another connector completed it, so the fatal path's ROLLBACK restored
nothing: a COMMIT is now answered only by that sync, a refused one is REVERSED under the COMMIT's own mutex hold before
it answers, and a refused reversal marks the file so that every later ROLLBACK answers by its own sync. A unique key value
freed by a second change of the same record was in no before-image and leaked past COMMIT and ROLLBACK: every holding is
now an entry of its owner's UNDO tree, which the HOLDINGS tree indexes by identity, and a drift test keeps the two key
sets equal. The owner stopped the review rounds that night, before round 5 was reviewed.

On 2026-10-18 the Mythos lane decided the route, and the branch's finisher merged origin/main and wrote it into PB3330.
The prose review rounds are over: the method is SIMULATOR-FIRST. The deterministic fault-injecting page medium of
§2.2.1.12 is the first deliverable of PB3331, built before the engine's logic, and the crash rules, the crash points and
the two-run-unit interleavings become its executable cases; the design is corrected from what they find. The core engine
(PB3331-PB3336) goes to the Mythos lane's Opus waves. The transaction slice (PB3337, PB3338 and PB3340) took fifteen of
the twenty-one review findings, and it is HELD until the core has landed and its simulator proves the crash and coherence
rules on the real engine; its scope, and the cross-file COMMIT question round 5 left, are the owner's decision. PB3330
also gains the owner's standing test rule (success and abnormal cases for SORT and file I/O) and the engine's own cases,
which the planner writes into every group of the cluster.

The branch review of that route (round 6) found nine places where the notes did not yet carry it. The hold was a line
of prose the planner never reads; PB3337, PB3338 and PB3340 now carry `status: owner`, which the planner does read, and
a dry run plans PB3331-PB3336 and lists the three as waiting. PB3331 itself now says its first commit is the simulator
and the cases written from the design text, with the held slice's cases written as pending. Two standing requirements
that bound every core group to COMMIT and ROLLBACK rows are scoped to the rows each group owns. The transaction cost
witness moved from PB3336 to PB3337, and PB3333 rewrites the relative half of the file-sharing documentation row in the
same landing that changes the relative mutex rule. Four test paths were left from before main's corpus move, and one
dead-owner test still described round 3's region locks; both are corrected.

Notes and docs only. No code change.

