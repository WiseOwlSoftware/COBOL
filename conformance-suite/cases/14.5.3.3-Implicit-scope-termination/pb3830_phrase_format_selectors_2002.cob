      *>--- conformance-case
      *> id: pb3830_phrase_format_selectors_2002
      *> rules: [GR-14.5.3.3-4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R1-1: the format selectors COBOL 2002
      *> introduced. ISO 14.5.3.3: "Any phrase encountered is the next
      *> phrase of the most-recently preceding unterminated statement with
      *> which that phrase may be syntactically associated." (cite.py
      *> --check 14.5.3.3 -> OK.) READ PREVIOUS and READ ... ADVANCING ON
      *> LOCK are Format 1 only, which prints no INVALID KEY phrase
      *> (14.9.30.2); a COMPUTE whose right side is a boolean-expression is
      *> Format 2 (boolean-compute), which prints no SIZE ERROR phrase
      *> (14.9.8.2). So in each leg the phrase after the inner statement and
      *> the END- after it are the OUTER statement's: the program is legal.
      *> Expected values: RELF holds record 1 only, so the random READ
      *> succeeds and NOT INVALID KEY runs (14.9.30.4 GR13 c)); 0 + 1 fits
      *> PIC 9, so the ADD has no size error and NOT ON SIZE ERROR runs
      *> (14.7.5). The inner statements never execute.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830F2.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "PB3830F2R.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT IXG ASSIGN TO "PB3830F2G.TMP"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXG-KEY
               FILE STATUS IS FS2.
       DATA DIVISION.
       FILE SECTION.
       FD  RELF.
       01  RELF-REC PIC X(4).
       FD  IXG.
       01  IXG-REC.
           05 IXG-KEY PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK   PIC 9(4) VALUE 1.
       01  FS   PIC XX.
       01  FS2  PIC XX.
       01  F    PIC X VALUE "-".
       01  N    PIC 9 VALUE 0.
       01  B1   PIC 1(4) VALUE B"1100".
       01  B2   PIC 1(4) VALUE B"1010".
       01  B3   PIC 1(4).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RELF
           MOVE "AAAA" TO RELF-REC
           WRITE RELF-REC
           CLOSE RELF
           OPEN OUTPUT IXG
           CLOSE IXG
           OPEN I-O RELF
           OPEN INPUT IXG.
      *> Leg 1: READ PREVIOUS selects Format 1: no INVALID KEY.
           READ RELF
               INVALID KEY READ IXG PREVIOUS RECORD
               NOT INVALID KEY MOVE "N" TO F
           END-READ.
           DISPLAY "1 F=" F.
      *> Leg 2: ADVANCING ON LOCK selects Format 1: no INVALID KEY.
           MOVE "-" TO F
           READ RELF
               INVALID KEY READ IXG ADVANCING ON LOCK
               NOT INVALID KEY MOVE "N" TO F
           END-READ.
           DISPLAY "2 F=" F.
      *> Leg 3: a boolean-expression selects COMPUTE Format 2: no SIZE
      *> ERROR phrase.
           MOVE "-" TO F
           ADD 1 TO N
               ON SIZE ERROR COMPUTE B3 = B1 B-AND B2
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD.
           DISPLAY "3 F=" F " N=" N.
           CLOSE RELF IXG.
           STOP RUN.
