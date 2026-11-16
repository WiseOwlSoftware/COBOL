      *>--- conformance-case
      *> id: pb526_linage_ec_condition_85
      *> rules: [GR-13.18.34.4-3, GR-13.18.34.4-6, DOC-A.1-110]
      *> accept: [85, 2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The COBOL-85 EDITION TWIN of
      *> conformance-suite/cases/9.1.13.11-Implementor-defined-condition-with-unsuccessful-completion/pb526_linage_ec_condition,
      *> which carries the 2002 and 2023 derivation and every cite.py --check
      *> line. CORRECTED by kb/Work PB5845: this twin used to expect the 2002
      *> continuation at COBOL-85 too (B-OPEN 90, then the WRITE 90 and the
      *> CLOSE 00 on a file the OPEN had left open), and 1985 does not give it.
      *>
      *> WHAT 1985 DETERMINES, each text checked with scripts/spec/cite.py:
      *>  - VII.3.7.4 GR3: "The value must be greater than zero and not
      *>    greater than integer-1" (the footing start; integer-1 is the
      *>    page body).
      *>  - VII.1.3.5 GR5, an I-O status of 9x: "The input-output statement
      *>    was unsuccessfully executed as a result of a condition that is
      *>    specified by the implementor". The implementation's value for the
      *>    violation is 90 (docs/conformance/7/DOC-A.1-110.md), so the OPEN
      *>    is unsuccessful.
      *>  - VII.4.3.4 GR1: "The successful execution of an OPEN statement
      *>    determines the availability of the file and results in the file
      *>    being in an open mode". So the file is not open after the 90.
      *>  - VII.1.3.5 GRL2.4 g) and b): a WRITE attempted on a file not open
      *>    in the output or extend mode is 48, a CLOSE attempted for a file
      *>    not in the open mode is 42. GRL2.5 forbids a 9x for a condition a
      *>    4x names, so the WRITE is 48, never the 90 it answered before.
      *> The 2002 and later continuation (the counter at 0 until the CLOSE,
      *> every later WRITE raising EC-I-O-LINAGE) is NOT in the 1985 text, so
      *> the old A legs, which wrote after a value broke the page and expected
      *> LINAGE-COUNTER 0 and a latched 90, are not pinned here; an OPEN
      *> refusal with a zero page body is pinned by
      *> conformance-suite/cases/VII.4.3@1985-OPEN-STATEMENT/pb5845_linage_open_refused_85.
      *> LINAGE-COUNTER of a file that is not open has no value in 1985, so
      *> it is shown only while the file is open.
      *>
      *> Expected, in order:
      *>  A-OPEN 00 LC 1 (VII.3.7.4 GR9 d); A-W1 00 LC 2 (GR9 c 3).
      *>  B-OPEN (footing 0, body 5): USE-B 90, 90. B-W1: USE-B 48, 48.
      *>  B-CLOSE: USE-B 42, 42. B-REOPEN (footing 3): 00, LC 1. B-W2
      *>  ADVANCING 2 puts the counter at 3, the footing start, so the
      *>  AT END-OF-PAGE imperative runs: B-EOP2, 00, LC 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB526E85.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT LPA ASSIGN TO "pb526e85-a.prt"
               FILE STATUS IS FS-A.
           SELECT LPB ASSIGN TO "pb526e85-b.prt"
               FILE STATUS IS FS-B.
       DATA DIVISION.
       FILE SECTION.
       FD LPA LINAGE IS 2 LINES.
       01 A-REC PIC X(4).
       FD LPB LINAGE IS 5 LINES WITH FOOTING AT WS-FT.
       01 B-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-FT PIC 99  VALUE 0.
       01 FS-A  PIC XX  VALUE "??".
       01 FS-B  PIC XX  VALUE "??".
       01 LA    PIC 9(3).
       01 LB    PIC 9(3).
       PROCEDURE DIVISION.
       DECLARATIVES.
       ERR-B-SECT SECTION.
           USE AFTER STANDARD ERROR PROCEDURE ON LPB.
       ERR-B-PARA.
           DISPLAY "USE-B FS=" FS-B.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       MAIN-P.
           OPEN OUTPUT LPA.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-OPEN FS=" FS-A " LC=" LA.
           MOVE "AAAA" TO A-REC.
           WRITE A-REC.
           MOVE LINAGE-COUNTER OF LPA TO LA.
           DISPLAY "A-W1 FS=" FS-A " LC=" LA.
           CLOSE LPA.
           OPEN OUTPUT LPB.
           DISPLAY "B-OPEN FS=" FS-B.
           MOVE "EEEE" TO B-REC.
           WRITE B-REC AFTER ADVANCING 2 LINES.
           DISPLAY "B-W1 FS=" FS-B.
           CLOSE LPB.
           DISPLAY "B-CLOSE FS=" FS-B.
           MOVE 3 TO WS-FT.
           OPEN OUTPUT LPB.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-REOPEN FS=" FS-B " LC=" LB.
           MOVE "FFFF" TO B-REC.
           WRITE B-REC AFTER ADVANCING 2 LINES
               AT END-OF-PAGE DISPLAY "B-EOP2"
               NOT AT END-OF-PAGE DISPLAY "B-NOEOP2"
           END-WRITE.
           MOVE LINAGE-COUNTER OF LPB TO LB.
           DISPLAY "B-W2 FS=" FS-B " LC=" LB.
           CLOSE LPB.
           STOP RUN.
