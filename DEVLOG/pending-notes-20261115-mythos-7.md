## Four notes and two finding files from the Mythos lane's batch-5 rulings of 2026-11-15

Filed in kb/Work (documents only, no compiler source, no case, no inventory record). Each defect note carries the rule text with its `cite.py --check` line, the repro program in full, expected and observed at `--std 85` (and `--std 2023`), and the leads.

- PB5809 (defect): at `--std 85` STOP with a symbolic-character (`STOP STAR`) is refused, though `§1985:VI.6.24.3 SR1` bars from STOP only a figurative constant that begins with ALL; the other formats that take only `literal` are a lead not probed.
- PB5810 (defect, MAJOR): `RECORD VARYING ... DEPENDING ON` a table element is accepted and the length is then ignored at run time (a WRITE gives 00 where an elementary item gives 44), at every edition.
- PB5811 (defect, MAJOR): at `--std 85` an indexed READ of a record whose OCCURS DEPENDING ON count is past its maximum sets status 34 and a permanent error, a 2023 rule 1985 does not have; the sequential READ and SORT USING reach the same code (a lead not probed).
- PB5812 (adjudication, open): do the 1985 USE precedence rules reach across programs (a containing program's USE GLOBAL on the file against the contained program's own USE ON INPUT)?

Two dated finding files beside existing notes: `kb/Work/PB5780/2026-11-15-a-second-row.md` (the row `1985-GR-IV.4.3.8.4.4-2` joins the reference-modified comparison mechanism) and the source copied unchanged as `kb/Work/PB5683/2026-11-15-rulings-code-reads-batch-5.md`.

Mechanism 5 of the batch (`1985-GR-IX.2.3.4-2`, `1985-GR-IX.4.9.4-14`, `1985-GR-IX.4.9.4-17`) is REJECTED by the ruling: the behaviour is fixed by the owner's decisions R47 and R52 and documented in `docs/conformance/7/DOC-A.1-31.md`, so no note is filed for it; its lead (the key consequence that entry does not state) lives in the ruling copied under PB5683.

**The rate so far at 1985:** 56 of 684 rows read are contradicted (the sample 60, the first population 504, this batch 120), about 8 %; 5 undecided on open readings (PB5789, PB5799, PB5800, PB5805, PB5812).
