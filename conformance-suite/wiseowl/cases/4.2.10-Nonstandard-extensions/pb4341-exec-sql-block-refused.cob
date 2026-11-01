      *>--- conformance-case
      *> id: pb4341-exec-sql-block-refused
      *> rules: [GR-4.2.10-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4341 - an embedded SQL block is refused by name, once per block. ISO/IEC 1989 defines no EXEC
      *> construct: EXEC SQL ... END-EXEC is the input of a vendor's precompiler, which 4.2.14 lets an
      *> implementation require ("This permits an implementation to require components outside the COBOL
      *> implementation, such as precompilers"; cite.py OK 4.2.14), and this implementation has none, so no
      *> support is claimed (4.2.10: documentation "shall identify nonstandard extensions for which support is
      *> claimed"; cite.py OK 4.2.10 3); docs/CONFORMANCE.md section 3.2). Expected: COBOL5818 at each EXEC
      *> (lines 23, 27, 31 and 32 of this file), and nothing else: the parse goes on after each END-EXEC, as if
      *> the precompiler had replaced the block. It fails if the block draws "cannot parse construct near 'SQL'"
      *> (the message of a typo), if a word of the SQL text that is a COBOL verb (DELETE) resumes the parse as
      *> a statement, or if the recovery from a block in WORKING-STORAGE loses the rest of the program (each
      *> later block would go unreported). The count is pinned by EmbeddedPrecompilerBlockTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4341R1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
           EXEC SQL INCLUDE SQLCA END-EXEC.
       01 W-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           EXEC SQL
               SELECT A, B INTO :W-X, :W-N FROM T WHERE C = 'Q'
           END-EXEC
           DISPLAY W-X.
           EXEC SQL DELETE FROM T END-EXEC.
           EXEC CICS RETURN END-EXEC.
           STOP RUN.
