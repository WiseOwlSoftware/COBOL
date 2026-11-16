      *>--- conformance-case
      *> id: pb5825_when_exception_open_mode_opened
      *> rules: [GR-14.9.28.4-17, GR-14.9.49.4-6]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5825 review finding R1-1. A WHEN EXCEPTION INPUT | OUTPUT | I-O | EXTEND phrase
      *> is matched by the rules of the USE statement (§2023:14.9.28.4 GR17: "The rules for
      *> determining a match are specified in General rules 3a to 3g of the USE statement"), and
      *> the mode rules of that statement apply to a file open in the mode or in the process of
      *> being opened in it (§2023:14.9.49.4 GR6b: "for any file open in the input mode or in the
      *> process of being opened in the input mode").
      *> DERIVATION. F is open INPUT. OPEN OUTPUT F fails '41' (§2023:9.1.13.7 1)), an EC-I-O
      *>   condition raised for a file that IS open in the input mode and is being opened in the
      *>   output mode. So the WHEN EXCEPTION INPUT phrase matches by the opened mode, and the
      *>   WHEN EXCEPTION EXTEND phrase, written first, matches neither mode and does not run.
      *>   The 41 is fatal; RESUME NEXT STATEMENT continues after the OPEN.
      *> EDITIONS: 2023 only; the exception-checking PERFORM is a 2023 facility.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5825WE.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb5825we.dat"
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
               DISPLAY "AFTER-OPEN " F-ST
           WHEN EXCEPTION EXTEND
               DISPLAY "EXTEND-MODE " F-ST
           WHEN EXCEPTION INPUT
               DISPLAY "INPUT-MODE " F-ST
               RESUME NEXT STATEMENT
           END-PERFORM
           CLOSE F
           STOP RUN.
