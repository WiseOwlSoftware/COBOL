      *>--- conformance-case
      *> id: pb4604-clause-keyword-at-the-entry-name-slot
      *> rules: [SR-13.16.3-4]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4604, the OVER-REJECTION GUARD for the entry-name slot.  A reserved word at the slot is refused as a
      *> name only when it is no clause: 13.16.3 SR4 makes the entry-name clause optional (cite.py --check 13.16.3
      *> "If no entry-name clause is specified" -> OK, the entry is a filler), so USAGE and VALUE below begin the
      *> entry's first clause and PIC the next entry's.  Every one is a legal unnamed item of the 13.16.2 Format 1
      *> general format and must keep compiling.  The word that follows a name (W-N MOVE) is another matter: it stays
      *> "not a clause" (pb2533-data-entry-reserved-word-clause).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4604D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-G.
          05 USAGE DISPLAY PIC X(2) VALUE "OK".
          05 PIC X VALUE "!".
          05 VALUE "?" PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY W-G
           STOP RUN.
