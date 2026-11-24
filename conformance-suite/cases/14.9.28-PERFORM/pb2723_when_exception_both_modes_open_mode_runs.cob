      *>--- conformance-case
      *> id: pb2723_when_exception_both_modes_open_mode_runs
      *> rules: [GR-14.9.28.4-17, GR-14.9.49.4-3, GR-14.9.49.4-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2723 review finding R1-1. A WHEN EXCEPTION INPUT | OUTPUT | I-O | EXTEND
      *> phrase is matched by the rules of the USE statement (§2023:14.9.28.4 GR17: "The rules
      *> for determining a match are specified in General rules 3a to 3g of the USE statement"),
      *> and a mode's rule applies to a file open in the mode or in the process of being opened
      *> in it (§2023:14.9.49.4 GR6: "for any file open in the input mode or in the process of
      *> being opened in the input mode"; c) to e) the same for the other modes).
      *> DERIVATION. An OPEN refused '41' (§2023:9.1.13.7 GR1) on a file open in another mode
      *>   meets two modes. Where a phrase names each of the two, the text orders neither, and
      *>   the determination D-USE-OPEN-MODE-BOTH (docs/conformance/3) gives the mode the file
      *>   IS open in, as for a USE declarative, whatever order the phrases are written in:
      *>   A. F open INPUT, OPEN OUTPUT F: WHEN OUTPUT is written first, WHEN INPUT runs.
      *>   B. F open OUTPUT, OPEN EXTEND F: WHEN EXTEND is written first, WHEN OUTPUT runs.
      *>   C. F open INPUT, OPEN OUTPUT F, no WHEN INPUT: the being-opened mode's WHEN OUTPUT
      *>      runs (kb/Work PB5825: when one of the two modes has a phrase, it runs).
      *>   Each 41 is fatal; RESUME NEXT STATEMENT continues after the OPEN.
      *> EDITIONS: 2023 only; the exception-checking PERFORM is a 2023 facility.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2723WB.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb2723wb.dat"
               ORGANIZATION IS LINE SEQUENTIAL
               FILE STATUS IS F-ST.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 F-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 F-ST PIC XX.
       PROCEDURE DIVISION.
       M.
           OPEN OUTPUT F
           CLOSE F
           OPEN INPUT F
       >>TURN EC-ALL CHECKING ON
           PERFORM
               OPEN OUTPUT F
               DISPLAY "A-AFTER " F-ST
           WHEN EXCEPTION OUTPUT
               DISPLAY "A-OUTPUT " F-ST
               RESUME NEXT STATEMENT
           WHEN EXCEPTION INPUT
               DISPLAY "A-INPUT " F-ST
               RESUME NEXT STATEMENT
           END-PERFORM
           CLOSE F
           OPEN OUTPUT F
           PERFORM
               OPEN EXTEND F
               DISPLAY "B-AFTER " F-ST
           WHEN EXCEPTION EXTEND
               DISPLAY "B-EXTEND " F-ST
               RESUME NEXT STATEMENT
           WHEN EXCEPTION OUTPUT
               DISPLAY "B-OUTPUT " F-ST
               RESUME NEXT STATEMENT
           END-PERFORM
           CLOSE F
           OPEN INPUT F
           PERFORM
               OPEN OUTPUT F
               DISPLAY "C-AFTER " F-ST
           WHEN EXCEPTION OUTPUT
               DISPLAY "C-OUTPUT " F-ST
               RESUME NEXT STATEMENT
           WHEN EXCEPTION EXTEND
               DISPLAY "C-EXTEND " F-ST
               RESUME NEXT STATEMENT
           END-PERFORM
           CLOSE F
           STOP RUN.
