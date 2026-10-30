## Fuzz night 9 is triaged: one WiseOwl wrong answer (PB4096), two new GnuCOBOL records (PB4094, PB4095)

The ninth differential-fuzz night (run `fuzz-20261029-224533`, qwen3.6:35b, 8 hours, seed 20261030) judged 6,851
programs on main 8803a84ff. Of them, 1,901 survived the harness's own filters, in 324 signatures, and 267 of those
signatures (1,819 programs) went to 13 read-only adjudicator slices. The run's judge was still main's head, so the
recorded results stand and nothing could be fixed-since.

- **WiseOwl: one defect, PB4096 (wrong answer).** TEST-NUMVAL-F returns LENGTH + 1 when a character in error follows
  the exponent's sign: `"12.34E+AB"` gives 10 where §15.95.4 1) b) gives 8, `"1E++2"` gives 6 where it gives 4. The
  exponent arm of `NvfScan` lacks the end-of-text test PB121 gave the significand arm. Filed `owner`, as every
  fuzz-found WiseOwl defect is.
- **GnuCOBOL: two new records, both upheld by an independent refuter.** PB4094: a negative literal exponent overwrites
  the shared literal, so a later `A * -1` or `A - -1` uses the wrong value. PB4095: `-std=cobol2014` refuses
  `MOVE ALL B"1"` to a boolean item. The refuter corrected both records in place (edition numbering, the overclaimed
  reach of PB4094, the full stderr of PB4095).
- **67 signatures are new witnesses** of existing upstream notes; the rest are implementor latitude (75), WiseOwl
  correct (93), uninitialized data (11), not comparable (5) or the model's errors (12). One row is held: the width of
  EXCEPTION-STATEMENT under 2014 cannot be checked against a 2014 text.

`FUZZ-LEDGER.md`, `docs/fuzz-ledger/` and `conformance-suite/fuzz-history.json` carry night 9 (ledger version
118421220031).
