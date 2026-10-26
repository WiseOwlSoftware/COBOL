      *>--- conformance-case
      *> id: pb3830_phrase_format_selectors
      *> rules: [GR-14.5.3.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830, branch review R1-1. ISO 14.5.3.3: "Any phrase
      *> encountered is the next phrase of the most-recently preceding
      *> unterminated statement with which that phrase may be syntactically
      *> associated." (cite.py --check 14.5.3.3 -> OK.) Whether a phrase MAY
      *> be associated with a statement is decided by the statement's
      *> general formats, and a format is chosen by everything the statement
      *> wrote, not by its phrases alone: READ NEXT is Format 1 only, which
      *> prints no INVALID KEY phrase, and READ ... KEY IS is Format 2 only,
      *> which prints no AT END phrase (14.9.30.2); WRITE ... ADVANCING is
      *> Format 1 only, which prints no INVALID KEY phrase (14.9.51.2).
      *> So in each leg the inner statement may NOT take the phrase that
      *> follows it, the phrase and the END- after it are the OUTER
      *> statement's, and the program is legal.
      *> Expected values: RELF holds record 1 only. Leg 1: the random READ
      *> of record 1 succeeds, so NOT INVALID KEY runs (14.9.30.4 GR13 c)).
      *> Leg 2: after a fresh OPEN the READ NEXT returns record 1, so NOT AT
      *> END runs (the same GR13 c)). Leg 3: record 2 does not exist, so the
      *> WRITE succeeds and NOT INVALID KEY runs (9.1.14 2)). A compiler
      *> that gave the phrase to the inner statement runs neither phrase in
      *> legs 1 and 3 and refuses leg 2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830FS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RELF ASSIGN TO "PB3830FSR.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK
               FILE STATUS IS FS.
           SELECT IXG ASSIGN TO "PB3830FSG.TMP"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IXG-KEY
               FILE STATUS IS FS2.
           SELECT PRT ASSIGN TO "PB3830FSP.TMP"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS3.
       DATA DIVISION.
       FILE SECTION.
       FD  RELF.
       01  RELF-REC PIC X(4).
       FD  IXG.
       01  IXG-REC.
           05 IXG-KEY PIC X(4).
       FD  PRT.
       01  PRT-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK   PIC 9(4) VALUE 1.
       01  FS   PIC XX.
       01  FS2  PIC XX.
       01  FS3  PIC XX.
       01  F    PIC X VALUE "-".
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT RELF
           MOVE "AAAA" TO RELF-REC
           WRITE RELF-REC
           CLOSE RELF
           OPEN OUTPUT IXG
           CLOSE IXG
           OPEN I-O RELF
           OPEN INPUT IXG
           OPEN OUTPUT PRT.
      *> Leg 1: READ NEXT selects Format 1, which prints no INVALID KEY.
           MOVE 1 TO RK
           READ RELF
               INVALID KEY READ IXG NEXT RECORD
               NOT INVALID KEY MOVE "N" TO F
           END-READ.
           DISPLAY "1 F=" F.
      *> Leg 2: KEY IS selects Format 2, which prints no AT END.
           MOVE "-" TO F
           CLOSE RELF
           OPEN I-O RELF
           READ RELF NEXT RECORD
               AT END READ IXG KEY IS IXG-KEY
               NOT AT END MOVE "N" TO F
           END-READ.
           DISPLAY "2 F=" F.
      *> Leg 3: ADVANCING selects WRITE Format 1: no INVALID KEY.
           MOVE "-" TO F
           MOVE 2 TO RK
           WRITE RELF-REC
               INVALID KEY WRITE PRT-REC AFTER ADVANCING 1 LINE
               NOT INVALID KEY MOVE "N" TO F
           END-WRITE.
           DISPLAY "3 F=" F.
           CLOSE RELF IXG PRT.
           STOP RUN.
