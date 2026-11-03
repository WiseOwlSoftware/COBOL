## The GnuCOBOL differential's six flips are adjudicated and its baseline is re-recorded

The nightly differential had been red since 2026-11-01 (run 41679939118 on 11-03): six per-case flips against the
baseline, three of them kb/Work PB4346's. A local run at main 3e62b1816 reproduced exactly the six. Each was adjudicated
against the ISO text (PB4346 holds the record), and none is a compiler defect:

- `run_extensions:2172`, `run_misc:5307`: COBOL0899 on `CALL "x" USING ... OMITTED`. The printed §14.9.4.2 Format 1
  (PDF p618) has no OMITTED; it belongs to Format 2 (program-prototype), and 2002's §14.8.4.1 agrees.
- `syn_misc:3951`: COBOL5962, a REPOSITORY `PROGRAM` that §12.3.8.3 SR14 cannot answer (PB2103's resolver).
- `syn_misc:7168`: COBOL0900 at `--std 85` for a sentence before the first paragraph-name, new in 2002 (Annex F.2 item 90).
- `syn_file:431` and `run_fundamental:2072`: the two compilers now agree.

`gnucobol_differential.py --write-baseline --record` moved exactly those six rows. Running cobc 3.2 under its ISO
dialects on minimal repros written for the purpose showed that three of the GnuCOBOL acceptances are its own
under-rejects, filed as upstream records PB4499 (OMITTED without a prototype), PB4500 (SR14 silent, and `-Wprototypes`
gated on `-Winitial-value` in `tree.c`) and PB4501 (`-std=cobol85` with no leading paragraph-name), unreported until the
owner approves.

The six superseded branches the probe listed as DECISION are recorded ABANDONED in their notes (PB4453, PB4462, PB4297,
PB4432, PB4464, PB4467): each one's work is on main.
