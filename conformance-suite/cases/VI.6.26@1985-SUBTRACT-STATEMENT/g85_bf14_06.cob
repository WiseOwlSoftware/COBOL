      *>--- conformance-case
      *> id: g85_bf14_06
      *> rules: [1985-GR-VI.6.26.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.26.4 GR2 - format 2: the operands before FROM are summed,
      *> the sum is subtracted from identifier-2 or literal-2, and the
      *> result is the new content of each identifier-3. L1 fails if C
      *> (after FROM) changes or a receiver is skipped; L2 if the literal
      *> operands are not all subtracted from 20; L3 if C, the minuend and a
      *> receiver, is stored before the difference reaches Y; L4 if a
      *> numeric edited receiver does not get the negative difference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1406.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       01 B PIC 9 VALUE 2.
       01 C PIC 99 VALUE 10.
       01 X PIC 99 VALUE 0.
       01 Y PIC 99 VALUE 0.
       01 ED PIC -9.
       PROCEDURE DIVISION.
       P1.
           SUBTRACT A B FROM C GIVING X Y.
           DISPLAY "L1 " X " " Y " " C.
           SUBTRACT 1 2 3 FROM 20 GIVING X.
           DISPLAY "L2 " X.
           SUBTRACT A B FROM C GIVING C Y.
           DISPLAY "L3 " C " " Y.
           SUBTRACT 5 FROM 3 GIVING ED.
           DISPLAY "L4 " ED.
           STOP RUN.
