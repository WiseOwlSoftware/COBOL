      *>--- conformance-case
      *> id: g85_f19_02
      *> rules: [1985-GR-VII.4.3.4-21]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.4.3.4 GR21 - an unavailable OPTIONAL sequential
      *> file opened EXTEND (OE) or I-O (OI) is created as if by OPEN
      *> OUTPUT then CLOSE, then the program's own OPEN runs:
      *> - OE: the OPEN reports 05 (§1985:VII.1.3.5, "If the open mode
      *>   is I-O or extend, the file has been created"), the record
      *>   written after it is the file's only record.
      *> - OI: the OPEN reports 05 and the first READ meets the end of
      *>   a file that "contains no data records" (status 10); a later
      *>   OPEN INPUT finds the file present (00, not 05) and empty.
      *> - OO: OPEN OUTPUT "creates the file" and, on re-creation of a
      *>   file that held O1, the file "contains no data records".
      *> A leg fails if creation is skipped (05 on the second open, or
      *> 35), if a record leaks into a created file, or if OUTPUT
      *> keeps the earlier record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1902.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OPTIONAL OE ASSIGN TO "F1902E"
               FILE STATUS IS OE-S.
           SELECT OPTIONAL OI ASSIGN TO "F1902I"
               FILE STATUS IS OI-S.
           SELECT OO ASSIGN TO "F1902O"
               FILE STATUS IS OO-S.
       DATA DIVISION.
       FILE SECTION.
       FD  OE
           LABEL RECORDS ARE STANDARD.
       01  OE-R PIC X(2).
       FD  OI
           LABEL RECORDS ARE STANDARD.
       01  OI-R PIC X(2).
       FD  OO
           LABEL RECORDS ARE STANDARD.
       01  OO-R PIC X(2).
       WORKING-STORAGE SECTION.
       01  OE-S PIC XX VALUE "99".
       01  OI-S PIC XX VALUE "99".
       01  OO-S PIC XX VALUE "99".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN EXTEND OE.
           DISPLAY "OE EXTEND " OE-S.
           MOVE "E1" TO OE-R.
           WRITE OE-R.
           CLOSE OE.
           OPEN INPUT OE.
           DISPLAY "OE INPUT " OE-S.
           READ OE AT END DISPLAY "OE AT END " OE-S.
           DISPLAY "OE " OE-R.
           READ OE AT END DISPLAY "OE AT END " OE-S.
           CLOSE OE.
           OPEN I-O OI.
           DISPLAY "OI I-O " OI-S.
           READ OI AT END DISPLAY "OI AT END " OI-S.
           CLOSE OI.
           OPEN INPUT OI.
           DISPLAY "OI INPUT " OI-S.
           READ OI AT END DISPLAY "OI AT END " OI-S.
           CLOSE OI.
           OPEN OUTPUT OO.
           DISPLAY "OO OUTPUT " OO-S.
           MOVE "O1" TO OO-R.
           WRITE OO-R.
           CLOSE OO.
           OPEN OUTPUT OO.
           DISPLAY "OO OUTPUT " OO-S.
           CLOSE OO.
           OPEN INPUT OO.
           DISPLAY "OO INPUT " OO-S.
           READ OO AT END DISPLAY "OO AT END " OO-S.
           CLOSE OO.
           STOP RUN.
