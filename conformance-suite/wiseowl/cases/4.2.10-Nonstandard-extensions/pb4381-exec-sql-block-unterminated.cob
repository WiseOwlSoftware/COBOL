      *>--- conformance-case
      *> id: pb4381-exec-sql-block-unterminated
      *> rules: [GR-4.2.10-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4381 - an embedded SQL block left without its END-EXEC is refused as that fault (COBOL5842),
      *> and the blocks of the source that do close stay refused by name, each once, as before. ISO/IEC 1989
      *> defines no EXEC construct: the block is the input of a precompiler, which 4.2.14 lets an implementation
      *> require ("This permits an implementation to require components outside the COBOL implementation, such
      *> as precompilers"; cite.py OK 4.2.14), and this implementation has none (4.2.10 3); docs/CONFORMANCE.md
      *> section 3.2). The sentence of the second block ends at its period before any END-EXEC: 8.3.5 3), "The
      *> COBOL character period, when followed by a space, is a separator" (cite.py OK 8.3.5 3)), so the block's
      *> text ends there. EXEC is no reserved word of any edition (8.9), but this source declares no data item
      *> named EXEC, so every EXEC of it opens a block. Expected: the refusal of an embedded block at the EXEC of lines 27 and 31,
      *> COBOL5842 at the EXEC of line 29, and nothing else. It fails if the unterminated block makes the
      *> source read as COBOL throughout (the two closed blocks then draw "cannot parse construct near SQL"
      *> again), if the unterminated block is lost (no COBOL5842), or if a closed block is reported twice. The
      *> count is pinned by EmbeddedPrecompilerBlockTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4381R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           EXEC SQL SELECT A INTO :W-X FROM T END-EXEC.
           DISPLAY W-X.
           EXEC SQL SELECT B INTO :W-X FROM T.
           DISPLAY W-X.
           EXEC SQL COMMIT END-EXEC.
           STOP RUN.
