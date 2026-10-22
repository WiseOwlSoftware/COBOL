## Entry 2357 — 2026-10-22 12:13 PDT — The fuzz generator's first-attempt validity filed as PB3523

On the second overnight fuzz run (4,931 programs), 47 % ended BOTH_REJECT: neither WiseOwl nor GnuCOBOL accepted the generated program. Every such program used all three repair rounds, and about 65 % of the night's model generations went to programs that never compiled. The owner's instruction was to update the prompt so the model generates correct COBOL at the first attempt, with fewer or no repairs. PB3523 holds the measurement, the implementer scope (the clause's general format in the prompt, repair rounds recorded, an early stop on a repeated diagnostic, a before-and-after bake-off) and the goldens.

