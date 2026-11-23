## Corrections (clerk, 2026-11-22, review R2-1 and R2-2 of the cause adjudication, report w20262140b)

Re-verified on main ae1136f781 against `conformance-suite/measurements/gnucobol.json`:
- `2026-11-22-cause-adjudication.md` line 12 says "the file-status buckets are one to three tests each". Of the 26 output `gnucobol-nonconformance` records, IX.1.3 holds 6, VII.4.3 6, VIII.4.3 5 and VIII.4.5 5 (IX.4.5 holds 1). The finisher re-ran two tests per bucket; the rest rest on run.py first-difference lines (reasons.tsv), a diff reading but not the one the sentence implies.
- The Sites line (line 16) still says "the 351 causes and citations"; the file carries 273 (108 gnucobol-nonconformance, 165 non-portable-test; 211 unclassified of 484 failures).
