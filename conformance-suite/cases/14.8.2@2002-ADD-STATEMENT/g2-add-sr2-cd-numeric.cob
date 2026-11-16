      *>--- conformance-case
      *> id: g2-add-sr2-cd-numeric
      *> rules: [2002-SR-14.8.2.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.2 SR2 - "Identifier-1 and identifier-2 shall
      *> reference a numeric data item."  The accepted leg: both are
      *> numeric items of a record under a CD entry (§2002:A.1.1.2).
      *> Format 1 ADD CA CB TO CN: §2002:14.8.2.3 GR1, the addends
      *> (CA 10, CB 5) are summed and the sum added to CN (3): 18.
      *> Format 2 ADD CA CN GIVING CG: §2002:14.8.2.3 GR2, 10 + 18 = 28.
      *>   F1 0018
      *>   F2 0028
      *> Fails if numeric CD items are refused as ADD operands or the
      *> sums are wrong.  The refusal legs are
      *> g2-add-sr2-cd-alnum-addend
      *> and g2-add-sr2-cd-alnum-receiver.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADS2K.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT.
       01  CREC.
           05 CA PIC 9(4).
           05 CB PIC 9(4).
           05 CN PIC 9(4).
           05 CG PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE 10 TO CA
           MOVE 5 TO CB
           MOVE 3 TO CN
           ADD CA CB TO CN
           DISPLAY "F1 " CN
           ADD CA CN GIVING CG
           DISPLAY "F2 " CG
           STOP RUN.
       END PROGRAM G2ADS2K.
