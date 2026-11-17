      *>--- conformance-case
      *> id: pb5876-linkage-odo-object-not-referenceable
      *> rules: [1985-GR-X.5.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.1 GR4: Linkage items may be "referenced within the
      *> Procedure Division of that program if, and only if," they meet
      *> the rule's legs. §1985:IV.4.4.1: a reference may be implicit,
      *> and "Such an implicit reference occurs if and only if the data
      *> item contributes to the execution of the statement." LN, the
      *> OCCURS DEPENDING ON object of L1's table, fixes the length of
      *> L1, so DISPLAY L1 references LN; LN's record L2 is no USING
      *> operand, so the program is refused with COBOL2746 (kb/Work
      *> PB5876). It used to compile and display L1 as empty, LN read
      *> from storage the caller never passed. The control,
      *> pb5876-linkage-odo-object-using-operand, names L2 in USING.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W1.
          05 W1E PIC X OCCURS 5 TIMES.
       PROCEDURE DIVISION.
       P0.
           MOVE "AAAAA" TO W1.
           CALL "PB5876S" USING W1.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5876S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L1.
          05 L1E PIC X OCCURS 1 TO 5 DEPENDING ON LN.
       01 L2.
          05 LN PIC 9.
       PROCEDURE DIVISION USING L1.
       P1.
           DISPLAY "S L1=" L1.
           EXIT PROGRAM.
       END PROGRAM PB5876S.
       END PROGRAM PB5876A.
