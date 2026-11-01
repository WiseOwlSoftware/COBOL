      *>--- conformance-case
      *> id: pb4384-exec-sql-semicolon-in-block
      *> rules: [GR-4.2.10-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4384 - a semicolon inside an embedded SQL block is text of the block, not a COBOL separator, so
      *> it draws no separator diagnostic of its own. The block is the input of a precompiler (4.2.14: "This
      *> permits an implementation to require components outside the COBOL implementation, such as
      *> precompilers"; cite.py OK 4.2.14) and this implementation has none, so it is refused by name, once
      *> (COBOL5818; 4.2.10 3); docs/CONFORMANCE.md section 3.2). 8.3.5 2), "The COBOL characters comma and
      *> semicolon, immediately followed by a space, are separators" (cite.py OK 8.3.5 2)), is a rule of COBOL
      *> text; the semicolon of A;B is followed by B, so if the block's text were judged as COBOL it would draw
      *> COBOL2631. Expected: COBOL5818 at the EXEC of line 23 and nothing else. It fails if the semicolon draws
      *> COBOL2631. The count is pinned by EmbeddedPrecompilerBlockTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4384R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           EXEC SQL SELECT A;B INTO :W-X FROM T END-EXEC.
           STOP RUN.
