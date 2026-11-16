      *>--- conformance-case
      *> id: pb5845_linage_open_refused_85
      *> rules: [1985-GR-VII.1.3.5-5, 1985-GR-VII.1.3.5-L2.5,
      *>    1985-GR-VII.4.3.4-1, 1985-GR-VII.4.3.4-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5845. An OPEN OUTPUT whose LINAGE values violate the
      *> value rules is refused with the implementor-defined status 90
      *> (docs/conformance/7/DOC-A.1-110.md), and at COBOL-85 that
      *> leaves the file NOT open. Every derivation below is from the
      *> 1985 text, checked with scripts/spec/cite.py --check:
      *>  - 1985 VII.3.7.4 GR2 "The value must be greater than zero"
      *>    (the page body) and GR3 "The value must be greater than zero
      *>    and not greater than integer-1" (the footing start).
      *>  - 1985 VII.1.3.5 GR5 (status 9x): "The input-output statement
      *>    was unsuccessfully executed as a result of a condition that
      *>    is specified by the implementor". So the refused OPEN is an
      *>    unsuccessful OPEN.
      *>  - 1985 VII.4.3.4 GR1: "The successful execution of an OPEN
      *>    statement determines the availability of the file and
      *>    results in the file being in an open mode", and GR4: "An OPEN
      *>    statement must be successfully executed prior to the
      *>    execution of any of the permissible input-output
      *>    statements". After the 90 the file is not in an open mode.
      *>  - 1985 VII.1.3.5 GRL2.4: a WRITE attempted on a file not open
      *>    in the output or extend mode is status 48 (g), a CLOSE
      *>    attempted for a file not in the open mode is 42 (b), an OPEN
      *>    attempted for a file in the open mode is 41 (a).
      *>  - 1985 VII.1.3.5 GRL2.5: a 9x condition must not duplicate a
      *>    4x one, so a WRITE of the not-open file is 48, never 90.
      *> Expected values by statement (the USE procedure runs for each
      *> unsuccessful statement and shows the status it was given):
      *>  OPEN OUTPUT F1, W-N 0: 90, USE. A second OPEN OUTPUT of the
      *>    file that is still not open meets the same zero: 90 again,
      *>    never 41 (the file is not in the open mode, GRL2.4 a).
      *>  WRITE R1: 48, USE. CLOSE F1: 42, USE.
      *>  W-N 3: OPEN OUTPUT 00 and WRITE 00 (the zero was the cause).
      *>  W-N 0 with F1 now open: OPEN OUTPUT is 41, USE: GRL2.4 a) is
      *>    the condition that applies to a file in the open mode, and
      *>    the file stays open (its CLOSE is 00).
      *>  OPEN INPUT F1 with W-N 0: 00. VII.3.7.4 GR6 reads the LINAGE
      *>    values only "at the time the file is opened by the execution
      *>    of an OPEN statement with the OUTPUT phrase".
      *>  F2, footing 0 and then 6 (body 5): 90 both times, WRITE 48,
      *>    CLOSE 42 for the first; footing 5 (not greater than the body)
      *>    is accepted, 00.
      *> Why each leg can fail: a runtime that keeps the connector open
      *> after the 90 answers OPEN-2 41, WRITE 90 and CLOSE-1 00 (the
      *> COBOL-2002 and later continuation, VCR row 7.84).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5845A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "pb5845a-1.prt"
               ORGANIZATION SEQUENTIAL
               FILE STATUS IS FS1.
           SELECT F2 ASSIGN TO "pb5845a-2.prt"
               ORGANIZATION SEQUENTIAL
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD F1
           LINAGE IS W-N LINES.
       01 R1 PIC X(10).
       FD F2
           LINAGE IS 5 LINES WITH FOOTING AT W-F.
       01 R2 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS1 PIC XX VALUE "??".
       01 FS2 PIC XX VALUE "??".
       01 W-N PIC 99 VALUE 0.
       01 W-F PIC 99 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       USE-F1 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F1.
       USE-F1-P.
           DISPLAY "USE-1 FS=" FS1.
       USE-F2 SECTION.
           USE AFTER STANDARD EXCEPTION PROCEDURE ON F2.
       USE-F2-P.
           DISPLAY "USE-2 FS=" FS2.
       END DECLARATIVES.
       MAIN-SECT SECTION.
       P0.
           OPEN OUTPUT F1.
           DISPLAY "OPEN-1 FS=" FS1.
           OPEN OUTPUT F1.
           DISPLAY "OPEN-2 FS=" FS1.
           MOVE "LINE" TO R1.
           WRITE R1.
           DISPLAY "WRITE-1 FS=" FS1.
           CLOSE F1.
           DISPLAY "CLOSE-1 FS=" FS1.
           MOVE 3 TO W-N.
           OPEN OUTPUT F1.
           DISPLAY "OPEN-3 FS=" FS1.
           WRITE R1.
           DISPLAY "WRITE-2 FS=" FS1.
           MOVE 0 TO W-N.
           OPEN OUTPUT F1.
           DISPLAY "OPEN-4 FS=" FS1.
           CLOSE F1.
           DISPLAY "CLOSE-2 FS=" FS1.
           OPEN INPUT F1.
           DISPLAY "OPEN-5 FS=" FS1.
           CLOSE F1.
           DISPLAY "CLOSE-3 FS=" FS1.
           OPEN OUTPUT F2.
           DISPLAY "F-OPEN-1 FS=" FS2.
           MOVE "LINE" TO R2.
           WRITE R2.
           DISPLAY "F-WRITE FS=" FS2.
           CLOSE F2.
           DISPLAY "F-CLOSE FS=" FS2.
           MOVE 6 TO W-F.
           OPEN OUTPUT F2.
           DISPLAY "F-OPEN-2 FS=" FS2.
           MOVE 5 TO W-F.
           OPEN OUTPUT F2.
           DISPLAY "F-OPEN-3 FS=" FS2.
           CLOSE F2.
           DISPLAY "F-CLOSE-2 FS=" FS2.
           STOP RUN.
