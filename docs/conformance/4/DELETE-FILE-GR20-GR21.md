---
order: 14
---
> ⚖ **DETERMINATION — the DELETE FILE statement's GR20/GR21 are internally contradictory in the printed
> standard (§2023:14.9.10.4)** (2026-08-29; kb/Work PB141). Both general rules open "If the execution of the DELETE
> FILE statement is successful" (verified against the PDF text layer, page 638 — not a transcription error),
> yet GR20 c) transfers control to the ON EXCEPTION phrase while GR21 b) says "The ON EXCEPTION phrase is
> ignored" — two contradictory outcomes for one successful execution. GR20's CONTENT is plainly the
> UNSUCCESSFUL-execution rule: its d) invokes §2023:9.1.12 "Input-output exception processing" (the unsuccessful-I-O
> machinery), it is the only home for the unsuccessful case (the GR list ends at GR21), and the twin Format-1
> rules pair success/failure the same way. WiseOwl COBOL reads GR20 as governing an UNSUCCESSFUL execution (a
> non-'0x' status: EC per §2023:9.1.13.1 set when enabled, then ON EXCEPTION as the handler, else exception
> processing) and GR21 as the successful case (NOT ON EXCEPTION; ON EXCEPTION ignored) — the reading every
> surveyed implementation takes. Pinned by `2023/pb141_delete_file_ec` and `2023/pb140_delete_file_gate`.
