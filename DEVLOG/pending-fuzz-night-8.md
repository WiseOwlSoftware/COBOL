## Fuzz night 8: no WiseOwl defects, five GnuCOBOL upstream records, and a night cut short by a reboot

Fuzz night 8 (`fuzz-20261028-210601`, seed 20261029, judge built from main 1498935b6) stopped at 02:29 when the machine
rebooted, after about 5.4 of its 8 planned hours. It judged 4350 programs; 1276 survived the filters in 272 signatures.
Every program is judged and written as it completes, so the partial night was triaged as it stood.

Twelve read-only adjudicator slices (Workflow `fuzz-triage`) decided every surviving signature: 87 WiseOwl correct, 62
latitude, 55 GnuCOBOL-side, 20 not comparable, uninitialized or model error. None is a WiseOwl defect. Every citation the
verdicts carry passed `cite.py --check`.

The independent refuter re-ran the five new GnuCOBOL-side records under cobc 3.2.0 and upheld all five, with corrections
to citations and wording. `fuzz_triage.py file --write` filed them as `kind: upstream` notes PB4040 to PB4044:

- TEST-NUMVAL-F returns 0 for a sign after the significand;
- a comma insertion symbol left of every digit position is replaced by a space (`PIC ,999`);
- SET condition-name TO TRUE is refused when the 88 VALUE is SPACE and the item is numeric-edited;
- the INVOKE statement is refused at `-std=cobol2014`;
- NOT before a condition that carries its own NOT is refused.

It also added the night's program ids as witnesses to 35 existing upstream notes and held one family (FUNCTION 15.31's
width under 2014), which the adjudicator could not check against the 2014 text.

`fuzz_triage.py history --write` recorded the night (ledger version 786bf648b722). It recorded the run's planned 8 hours,
not the 5.4 it ran, and the launcher log gives no sign that the run ended; that is PB4045.
