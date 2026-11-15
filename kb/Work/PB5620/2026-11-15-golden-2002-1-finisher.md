**2026-11-15, golden-2002-1's conflict finisher: 2002-SR-14.8.47.2-21 closes on this note's fix.**

The golden lane had added `2002-SR-14.8.47.2-21` to this note's open claim (the dated `2026-11-15-golden-2002-1.md`
beside it), and this note's landing released the claim. Re-probed on golden-2002-1 with main merged in, `cobol.exe <repro>
--std 2002 --run`: `g2wr21m.cob` (MANUAL, the 2002 Format 1 order, WITH LOCK after the END-OF-PAGE phrases) prints
`W 00 N=2`; `g2wr21a.cob` (its AUTOMATIC twin) is refused `COBOL5993` at the WITH LOCK, SR21's refusal and no longer the
parser's. The row is recorded CONFORMS (`record_verdicts.py --edition 2002`) on four goldens in
`conformance-suite/cases/14.8.47@2002-WRITE-STATEMENT/`: `g2_write_sequential_lock_phrase_manual` and `_automatic_neg`
(Format 1), `g2_write_random_lock_phrase_manual` and `_automatic_neg` (Format 2). Citation checked with
`python scripts/spec/cite.py --check "§2002:14.8.47.2 SR21" "neither the WITH LOCK phrase nor the WITH NO LOCK phrase shall be specified"` (OK).
