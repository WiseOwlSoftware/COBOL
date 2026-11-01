      *>--- conformance-case
      *> id: pb4341-exec-sql-block-in-branches
      *> rules: [GR-4.2.10-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4341 (review R1-1, R1-2) - an embedded SQL block that is the only statement of a branch,
      *> and one whose SQL text holds a qualified name, are each refused by name, once. ISO/IEC 1989 defines no
      *> EXEC construct: the block is the input of a precompiler, which 4.2.14 lets an implementation require
      *> ("This permits an implementation to require components outside the COBOL implementation, such as
      *> precompilers"; cite.py OK 4.2.14), and this implementation has none (4.2.10 3); docs/CONFORMANCE.md
      *> section 3.2). Each block stands for the statements its precompiler writes, so the IF, the EVALUATE and
      *> the inline PERFORM around them are whole: 14.9.19.3 1), "Statement-1 and statement-2 represent either
      *> one or more imperative statements or a conditional statement" (cite.py OK 14.9.19.3 1)). The period in
      *> T.A is followed by no space, so it is no separator period (8.3.5 3), "The COBOL character period, when
      *> followed by a space, is a separator"; cite.py OK 8.3.5 3)): it is text of its block. Expected:
      *> COBOL5818 at each EXEC and nothing else. It fails if a branch's block is read as nothing (a false
      *> END-IF or WHEN OTHER diagnostic, or the later blocks lost), or if T.A ends its block early (COBOL0001
      *> near SQL). The count is pinned by EmbeddedPrecompilerBlockTests.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4341R2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           IF W-N = 0
               EXEC SQL COMMIT END-EXEC
           ELSE
               EXEC SQL ROLLBACK END-EXEC
           END-IF.
           EVALUATE W-N
             WHEN 1
               EXEC SQL OPEN C1 END-EXEC
             WHEN OTHER
               EXEC SQL CLOSE C1 END-EXEC
           END-EVALUATE.
           PERFORM UNTIL W-N > 3
               EXEC SQL FETCH C1 INTO :W-X END-EXEC
               ADD 1 TO W-N
           END-PERFORM.
           EXEC SQL SELECT T.A INTO :W-X FROM T END-EXEC.
           DISPLAY W-X.
           STOP RUN.
