*>--- conformance-case
*> id: pb3794_multiple_data_item_lock_on_multiple
*> rules: [FMT-12.4.5.9.2, SR-8.3.2.1-3]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3794 - a data item named MULTIPLE in a program whose LOCK MODE clause writes the MULTIPLE
      *> phrase. ISO 12.4.5.9.2 prints LOCK MODE IS { MANUAL | AUTOMATIC } [ [WITH] LOCK ON [MULTIPLE]
      *> { RECORD | RECORDS } ] (cite.py: OK 12.4.5.9.2 "MULTIPLE"). 8.9 reserves MULTIPLE at COBOL-85 only, and
      *> the 2023 8.10 table lists it as the context-sensitive word of the LOCK ON phrase (cite.py: OK 8.10
      *> "| MULTIPLE | LOCK ON phrase |"), so 8.3.2.1 3) - "Context-sensitive words may be used as user-defined
      *> words and system-names in contexts other than the language construct in which they are defined" -
      *> admits the declaration, and 8.10 makes the word in the phrase the keyword. The leg that fails: the
      *> reservation gate retypes every MULTIPLE of a program that declares one, and the lock phrase's keyword
      *> position, spelling only the token, was refused with "unexpected 'MULTIPLE'" at 2002, 2014 and 2023.
      *> 12.4.5.9.3 SR2 bars the MULTIPLE phrase for sequential organization or access, so the file is INDEXED
      *> with DYNAMIC access.
      *>
      *> EXPECTED VALUES, DERIVED: 12.4.5.9.4 GR3 - the clause has no effect on a file open in the sharing with
      *> no other mode, and this program is the file's sole opener with no SHARING clause, so the record written
      *> (14.9.51.4, status 00) is read back by key (14.9.30.4, status 00) unchanged; the data item MULTIPLE starts
      *> at its VALUE 4 and the ADD makes it 5 (14.9.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3794MULT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "pb3794-multiple.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS R-KEY
               LOCK MODE IS MANUAL WITH LOCK ON MULTIPLE RECORDS
               FILE STATUS IS WS-FS.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R.
          05 R-KEY PIC X(2).
          05 R-VAL PIC X(4).
       WORKING-STORAGE SECTION.
       01 WS-FS PIC X(2).
       01 MULTIPLE PIC 9 VALUE 4.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F.
           MOVE "07" TO R-KEY.
           MOVE "WXYZ" TO R-VAL.
           WRITE R.
           DISPLAY "WFS=" WS-FS.
           CLOSE F.
           OPEN I-O F.
           MOVE "07" TO R-KEY.
           READ F
               INVALID KEY DISPLAY "MISSING"
               NOT INVALID KEY DISPLAY "VAL=" R-VAL
           END-READ.
           DISPLAY "RFS=" WS-FS.
           CLOSE F.
           ADD 1 TO MULTIPLE.
           DISPLAY "MULTIPLE=" MULTIPLE.
           STOP RUN.
