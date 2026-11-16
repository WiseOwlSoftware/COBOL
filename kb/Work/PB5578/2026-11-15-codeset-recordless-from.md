**2026-11-15, the golden-2002-8 integrator (gl2002 wave u, slug g2002u-13-16-49-select-when, a writer's lead): a CODE-SET file with NO record description entries converts the whole WRITE FILE ... FROM record, binary items included; row added to `inventory_rows`.**

Row added: `2002-GR-13.16.13.3-4` (never adjudicated at 2002; the writer's case `g2sw02_select_when_storage_sections_no_effect` landed in golden-2002-8 for `2002-GR-13.16.49.3-2` with a binary-free leg; the repro below is its first draft).

**Sites (writer's lead):** `src/WiseOwl.Cobol.Compiler/Binding/Model/FileModel.cs:575` (`TranslatesByRecord` is false for a record-less FD with no FORMAT and no SELECT WHEN), so `src/WiseOwl.Cobol.Compiler/CodeGen/Verbs/SequentialIoEmitter.cs:623` (`EmitCodeSetRegistration`) registers whole-record conversion instead of a plan from the FROM identifier's description (`RecordPlans.Of`), the engine this note's landing built.

**Rule (checked with `python scripts/spec/cite.py --check`, verdict OK).**

- `§2002:13.16.13.3 GR4`: with no record description entries the description used is the FROM identifier's, and "is used for code-set conversion of each data item described with usage display" (only display items are converted).

**Repro, re-run on the golden-2002-8 build (main 5e49d46678).** `E:/COBOL-coord/scratch/gl2002/integrate-8/repro/codeset/codeset_recordless_from_binary.cob` (from `lane1/out/g2002u-13-16-49-select-when/defects/codeset-recordless-from-converts-nondisplay/`, with `expected.out` beside it), command `cobol.exe codeset_recordless_from_binary.cob --std 2002 --run`. `FD F CODE-SET IS A-EBC RECORD CONTAINS 6 CHARACTERS.` has no 01; `WRITE FILE F FROM WS-REC` writes `WS-KEY PIC X`, `WS-TXT PIC X(3)` and `WS-BIN USAGE BINARY-SHORT VALUE 16705` (X'4141').

Expected `MEDIUM 5 065` / `MEDIUM 6 065` (WS-BIN not converted); observed `MEDIUM 5 193` / `MEDIUM 6 193` (converted as display), the other nine lines as expected; the writer saw the same at `--std 2023`, with or without the SELECT WHEN on the FROM item.
