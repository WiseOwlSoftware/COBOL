      *>--- conformance-case
      *> id: pb3830-call-not-on-overflow
      *> rules: [GR-14.5.3.3-4]
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
      *> no CALL format prints a NOT ON OVERFLOW phrase, and no other statement is open, so the phrase may be
      *> syntactically associated with no statement. Refused COBOL2269
      *> (the statement-format-shape code) at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3830U1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "PB3830U1S.TMP"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RELF ASSIGN TO "PB3830U1R.TMP"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS DYNAMIC
               RELATIVE KEY IS RK.
           SELECT PRTF ASSIGN TO "PB3830U1P.TMP"
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
           CALL "PB3830XX"
               NOT ON OVERFLOW MOVE "N" TO F
           END-CALL
           STOP RUN.
