# Adjudicator <<SLICE>> — <<TITLE>> (<<RUN_ID>>)

Read `<<TRIAGE>>/briefs/common.md` FIRST; it holds the authorization, the materials, the verdict vocabulary, the
GNUCOBOL-SIDE filing bar, the checkpoint and stop rules.

## Role
`cobol-adjudicator` (read-only) for slice **<<SLICE>>**.

## Your input (this slice only)
`<<INPUT>>` — <<N_SIGS>> signatures, <<N_ROWS>> programs. <<INPUT_DESCRIPTION>>

## Method
<<METHOD>>

## Allocated to you
- Verdicts file: `<<TRIAGE>>/adj/<<SLICE>>/verdicts-<<SLICE>>.jsonl`
- Probe programs and outputs: `<<TRIAGE>>/adj/<<SLICE>>/probes/` (WSL: `<<WSL_TRIAGE>>/adj/<<SLICE>>/probes/`)
- Records of GNUCOBOL-SIDE verdicts: `<<TRIAGE>>/records/<family>.md` and `.cob`
- Report: `<<TRIAGE>>/adj/<<SLICE>>/report-<<SLICE>>.md` (70 lines or fewer; returned as text too, see common.md "Return").
- No kb/Work ids: you file nothing; `fuzz_triage.py file` allocates and files.
