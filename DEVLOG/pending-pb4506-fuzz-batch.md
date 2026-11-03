## PB4506: the differential fuzz can write all its programs as one Message Batch, then judge them locally

The fuzz wrote, compiled and corrected one program at a time, because its local model often needed correction rounds.
A Claude writer needed none in the 20-program calibration, so the owner asked for stages: write the programs, then
compile and run them, then analyze.

`scripts/fuzz_batch.py submit` builds one Message Batches request per program from the same plan the judge reads
(`differential_fuzz.prompt_plan`, `seed_at`, `first_messages`), so request `fz00042` and judged row `fz00042` name the
same clause. `collect` writes each reply to `replies/<id>.json`, and `differential_fuzz.py --writer files --replies DIR`
judges them with corrections off, through the unchanged compile, run and verdict path. The batch is billed to an API key
read from a file.

Validity, the owner's measure of what the Claude writer gains over the local model, is counted as programs WiseOwl
compiles: 47.1 % (3,225 of 6,851) for the local model on 2026-10-29 (its WiseOwl-only refusals were all WiseOwl-correct at triage).
