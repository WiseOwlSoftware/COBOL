## File PB4945: RERUN's five 1985 syntax rules are never checked

A note-only change. Wave 20261914, while deciding PB4919's grammar candidates, found that the 1985 RERUN clause is parsed
and ignored. A RERUN on a non-sequential file, RECORDS or CLOCK-UNITS without implementor-name-1, two RERUN clauses naming
the same file-name-2, and two CLOCK-UNITS clauses all compile clean under `--std=1985`, against §1985:VII.2.12.3 SR1-SR5.
PB4945 screens each rule in the binder and sweeps the other I-O-CONTROL clauses for the same gap.
