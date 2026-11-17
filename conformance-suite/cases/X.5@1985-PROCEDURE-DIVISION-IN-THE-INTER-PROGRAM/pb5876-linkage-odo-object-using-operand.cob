      *>--- conformance-case
      *> id: pb5876-linkage-odo-object-using-operand
      *> rules: [1985-GR-X.5.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control of pb5876-linkage-odo-object-not-referenceable:
      *> L2, the record of L1's OCCURS DEPENDING ON object LN, is an
      *> operand of the USING phrase, so §1985:X.5.1 GR4 a admits LN and
      *> DISPLAY L1 is legal. The caller passes LN = 3, so L1 is its
      *> first three characters (§1985:VI.5.8.4 GR2 b, the number of
      *> occurrences is the value of data-name-1): "S L1=ABC".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1.
          05 W1E PIC X OCCURS 5 TIMES.
       01 W2.
          05 W2N PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       P0.
           MOVE "ABCDE" TO W1.
           CALL "PB5876T" USING W1 W2.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 L1E PIC X OCCURS 1 TO 5 DEPENDING ON LN.
       01 L2.
          05 LN PIC 9.
       PROCEDURE DIVISION USING L1 L2.
       P1.
           DISPLAY "S L1=" L1.
           EXIT PROGRAM.
       END PROGRAM PB5876T.
       END PROGRAM PB5876B.
