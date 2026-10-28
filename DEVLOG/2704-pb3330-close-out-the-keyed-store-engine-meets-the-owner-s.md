## Entry 2704 — 2026-10-28 13:15 PDT — PB3330 close-out: the keyed store engine meets the owner's bar, and the docs stop calling it unfinished

PB3330 was the campaign lead the owner opened on 2026-10-17. It covered the whole-store model, which read every relative and
indexed file into memory at OPEN and wrote it back whole, at CLOSE and after every changing statement under `SHARING WITH ALL
OTHER`. The note was never a group of its own. It landed when its last group landed, and every group now has: PB3331 to
PB3338, PB3340 and PB3382, with PB3492's keyed CAPTURE beside them. This branch re-probed each line of the bar on a Debug
build of origin/main 1da5e0d6b, flipped the note and corrected the docs that still described the campaign as in progress.

**The re-probe.** The 90 count-based performance witnesses pass: a load is linear through NO OTHER and through ALL OTHER, an
OPEN reads the header, a random READ reads one path, an exception path costs no more than its success, and a transaction
costs its own records. The three `WholeStoreBanDriftTests` cases pass, and none of the whole-store names (`ReadImage`,
`DecodeStore`, `MaxStoreBytes`, `KeyedStoreTable`) is left in `src/`. DOC-A.1-107 puts the boundary at the host's maximum file
size, capped by the format at 16 TiB.

The harm the note was opened for was a WRITE that reported '00' and was gone after a process died. A program wrote three
INDEXED records under ALL OTHER and two RELATIVE records, then blocked on ACCEPT and was killed before its CLOSE. A second
program read all five back. Under `APPLY COMMIT ON KF RLF`, the same program was killed after a COMMIT and one more RELATIVE
WRITE. An OPEN I-O run unit rolled the dead transaction back (§9.1.18.5, checked), so REL 0007 was gone and the committed
records remained. An OPEN INPUT reader that ran before any writable entry still read REL 0007. That is the determination
the design writes in 2.2.1.10 ("As built" (2)), not something this branch introduced. Because the D.2.5 text that
determination cites names the IGNORING LOCK phrase, this branch reports it as a lead for adjudication and leaves it unfixed.

**The docs.** In DESIGN-runtime-library.md 2.2.1, the STATUS paragraph still listed PB3338 and PB3340 as "still the target".
It now says every group has landed. 2.2.1.10 said the facility was "declined today at bind" through a pass that no longer
declines it; it now says the groups reversed the decline, and that with no active APPLY COMMIT clause COMMIT and ROLLBACK
have the effect of CONTINUE (§14.9.7.4 GR1 / §14.9.36.4 GR1, both checked). The cross-file COMMIT question is recorded as
answered per file (PB3337, PB3454), and witness (d) no longer describes `ReadImage` as present. The summary at the top of
docs/CONFORMANCE.md called Annex A.4.3 Partial while §5 claims it (since PB3492); it says Claimed now. Plan §0's two owner-decision
bullets no longer call PB3340 open, and DESIGN-USER-DOCUMENTATION.md notes that the keyed-file chapter's prerequisite has landed.

No code changed, so no golden was added and no inventory row moved: `closes_rows: []` with its reason.

