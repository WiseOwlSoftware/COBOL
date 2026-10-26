      *>--- conformance-case
      *> id: pb3830_call_on_overflow_2023
      *> rules: [GR-14.5.3.3-4]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3830. ISO 14.5.3.3 (its closing paragraph, after list item 4):
      *> "Any phrase encountered is the next phrase of the most-recently
      *> preceding unterminated statement with which that phrase may be
      *> syntactically associated. If all permitted occurrences of a phrase
      *> have already been specified for a given statement, a subsequent
      *> occurrence of that phrase is not syntactically associated with that
      *> statement." (cite.py --check 14.5.3.3 -> OK; cite.py labels the
      *> paragraph "4)", the rendered page prints it unindented after 4) b).)
      *> ISO 14.5.1: "Any statement with a conditional phrase that is not
      *> terminated by its explicit scope terminator is a conditional
      *> statement" (cite.py --check 14.5.1 -> OK).
      *>
      *> CALL prints [ ON OVERFLOW imperative-statement-1 ] in its 85-2014
      *> format only (ISO/IEC 1989:2002 14.8.4.1 Format 1; removed at 2023,
      *> Annex E.2 item 1c). At 2023 no CALL format prints it, so the ON
      *> OVERFLOW below may not be syntactically associated with the CALL and
      *> is the STRING's: "ABCD" overflows S2 and F becomes "O". Below 2023
      *> it is the CALL's, which makes that CALL a conditional statement in
      *> NOT ON OVERFLOW's imperative-statement (pb3830-call-on-overflow-85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830CO.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB3830COS.TMP"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RELF ASSIGN TO "PB3830COR.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
           SELECT PRTF ASSIGN TO "PB3830COP.TMP"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD SF.
       01 SF-REC PIC X(4).
       FD RELF.
       01 RELF-REC PIC X(4).
       FD PRTF LINAGE IS 2 LINES.
       01 PRT-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01 RK   PIC 9(4) VALUE 5.
       01 A    PIC 9 VALUE 5.
       01 B    PIC 9 VALUE 9.
       01 C    PIC 9 VALUE 0.
       01 D    PIC 9 VALUE 0.
       01 F    PIC X VALUE "-".
       01 G    PIC X VALUE "-".
       01 S1   PIC X(4) VALUE "ABCD".
       01 S2   PIC X(2) VALUE SPACES.
       01 T-AREA VALUE "ABC".
          05 T-E PIC X OCCURS 3 INDEXED BY TI.
       PROCEDURE DIVISION.
       MAIN-P.
           STRING S1 DELIMITED BY SIZE INTO S2
               NOT ON OVERFLOW CALL "PB3830YY"
               ON OVERFLOW MOVE "O" TO F
           END-STRING
           DISPLAY "F=" F
           STOP RUN.
