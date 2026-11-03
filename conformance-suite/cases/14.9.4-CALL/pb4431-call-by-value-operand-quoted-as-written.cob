*>--- conformance-case
*> id: pb4431-call-by-value-operand-quoted-as-written
*> rules: [SR-14.9.4.3-22]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4431 - ISO 14.9.4.3 SR22: "If identifier-4 or its corresponding formal parameter is specified with a
      *> BY VALUE phrase, identifier-4 shall be of class numeric, object, or pointer." A PIC X(4) item written
      *> qualified (A OF G) is refused, and the diagnostic quotes the operand as the program wrote it, qualifier and
      *> spaces included: 'A OF G'. Fails if the CALL is accepted, or the operand is quoted glued ('AOFG').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4431EXPL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 A PIC X(4).
          05 N PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           CALL "S1" AS NESTED USING BY VALUE A OF G
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. S1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LV PIC 9(4) USAGE BINARY.
       PROCEDURE DIVISION USING BY VALUE LV.
       P.
           GOBACK.
       END PROGRAM S1.
       END PROGRAM PB4431EXPL.
