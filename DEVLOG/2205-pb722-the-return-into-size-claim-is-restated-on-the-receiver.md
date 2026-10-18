## Entry 2205 — 2026-10-18 06:50 PDT — PB722: the RETURN ... INTO size claim is restated on the receiver that can show it

kb/Work PB722 reported that the inventory row `GR-14.9.34.4-5` (RETURN ... INTO) claimed the whole record area and the
current record were "observationally identical", on the strength of PB348's probe `p11`, whose receiver was a
left-justified `PIC X(12)`. Under ISO 14.6.8.5 that receiver lands a 6-byte sender and the whole space-padded area on
the same twelve characters, so the probe could not fail either way. The READ twin (`GR-14.9.30.4-4`) was measured on a
JUSTIFIED receiver and got the right answer; the RETURN arm was not.

Re-probe on a fresh build: the sentence was already gone from the row (PB348's landing restated it) and the size half
conforms, `pb339_into_current_record_2023` leg G printing `G=[    K01PQRST]` for an 8-byte released record into
`PIC X(12) JUSTIFIED RIGHT`, where the whole 20-byte area would print twelve spaces. Two residues of the same
inherited claim remained and are fixed here:

- `GR-14.9.34.4-5`'s notes listed "the RECORD IS VARYING size matches 13.18.43.4 GR16" among five obligations "already
  verified conforming". A `record_verdicts.py` batch (dry-run first: one row, verdict unchanged, GAP 66 -> 66) now says
  four were verified, and that the fifth rests on a JUSTIFIED receiver, with the 14.6.8.5 and 13.18.32.4 GR1/GR2
  citations each run through `cite.py --check`.
- `kb/Work/PB348.md`, the owning note, listed the size half among the obligations that "DO hold and were verified" from
  `p11`. It now says it was not verified there, why `p11` could not fail, and names the discriminating golden leg.

Sweep: no other inventory row in sections 13 and 14 pairs an "identical / indistinguishable / no observable" claim with
a sender-size or record-area claim; `GR-13.18.43.4-16` (the third row over the fact) now carries notes and the same
golden; the other repo mentions of "observationally identical" (`COBOLNET_FILES_DESIGN.md`, `IntoSenderDriftTests`,
`kb/Work/PB339.md`) already qualify it as true only for a LEFT-justified receiver. No compiler behaviour changed and no
row changed verdict; PB722 is flipped to landed with `closes_rows: []` and its reason.

The abnormal path, which the campaign (PB3353) requires of every group: the RETURN arm had no unsuccessful-RETURN
witness where READ has `l1_read_into_unsuccessful`. The new golden `14.9.34-RETURN/pb722_return_into_unsuccessful`
pins, from 14.9.34.4 GR3 to GR5 b), that the implied MOVE precedes the NOT AT END arm, that an at-end RETURN leaves a
JUSTIFIED receiver untouched while the record area still holds the earlier record (so an unconditional move would
show), and that item identification of `TE(IDX)` happens on the success path and not on the at-end path. It is cited
on `GR-14.9.34.4-4` and `-5` through a witness-only batch. Writing leg C showed that a declarative's RESUME AT NEXT
STATEMENT inside an output procedure ends the SORT instead of returning to the procedure; that is correct (RESUME is a
not-normal completion, 14.6.13.1.2 1), and 14.9.40.4 GR17 terminates the SORT, kb/Work PB1177), so the golden pins it
rather than working around it.

Branch review round 1 found the abnormal-path coverage still partial: RETURN's own exception conditions
(EC-FLOW-RETURN, EC-SORT-MERGE-RETURN) had no witness on the INTO arm, and none anywhere without a declarative. Added
the golden `14.9.34-RETURN/pb722_return_into_flow_ec` (both conditions with a declarative and RESUME; the INTO receiver
keeps its value, neither AT END nor NOT AT END runs) and `ReturnIntoExceptionPathTests`, whose six facts pin the arms a
corpus golden cannot (it must exit 0): the unhandled fatal termination and the declarative-completes-normally
termination for both conditions' shapes, the checking-OFF continuation (the store reports the end again, AT END runs,
the receiver is untouched, exit 0), and item identification without a declarative on a success RETURN (fatal) versus an
at-end RETURN (nothing raised). Everything conformed, so still no compiler change.

