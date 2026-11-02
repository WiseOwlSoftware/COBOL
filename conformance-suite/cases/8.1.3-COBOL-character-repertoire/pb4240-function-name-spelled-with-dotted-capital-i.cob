      *>--- conformance-case
      *> id: pb4240-function-name-spelled-with-dotted-capital-i
      *> rules: [GR-8.1.3.2-3, GR-8.1.3.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4240, review finding R2-1 - an intrinsic function
      *> name (8.11) is a word the standard defines, no user-defined
      *> word, so it is matched on the basic letters alone (8.1.3.2 3)
      *> a); the Annex C fold of 4) governs user-defined words only,
      *> owner decision 2026-10-31). FUNCTION İNTEGER, written with
      *> U+0130, names no intrinsic function. It fails when the
      *> function catalog is searched by the fold, which reads the
      *> word as INTEGER and compiles the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4240F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(4).
       PROCEDURE DIVISION.
           MOVE FUNCTION İNTEGER(1.5) TO N.
           STOP RUN.
