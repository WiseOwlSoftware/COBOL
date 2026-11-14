---
order: 26
---
> ⚖ **DETERMINATION — `FUNCTION EXCEPTION-LOCATION`'s third part, "an implementor-defined identifier of the source
> line that contains the beginning of the statement" (§2023:15.30.3 r2b3 / §2023:15.31.3 r2b3)** (2026-08-18; kb/Work PB63,
> RV-15.31.3-2 r2b3; revised 2026-08-18 by kb/Work PB82). **The identifier is the line number of the statement's
> first token in the source file that PHYSICALLY holds it, counted from 1** — the line the user edits, recovered
> through the preprocessing chain's origin map (§2023:7.2 text manipulation changes the resultant line count: COPY
> incorporates lines, REPLACE statements vanish, fixed-form continuations join). For a statement in the main source
> file the identifier is the bare number (`PB63LOC; IN-PARA OF ONLY-SECT; 42`); for a statement inside
> COPY-incorporated library text it is the copybook's file name (no directory) followed by the line in parentheses
> (`PB82LOC; ; pb82loc_proc.cpy(2)`), the same `file(line)` shape the compiler's own diagnostics print. The
> clause's NOTE disclaims stability across compilations; WiseOwl COBOL's value is stable for one compiler version and
> one set of source files. Pinned by `2023/pb63_exception_location_procedure_field` (no COPY) and
> `2023/pb82_exception_location_source_line` (a COPY, a REPLACE and a continuation before each RAISE; a RAISE
> inside a procedure copybook).
