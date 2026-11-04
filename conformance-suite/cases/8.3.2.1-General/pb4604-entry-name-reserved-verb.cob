      *>--- conformance-case
      *> id: pb4604-entry-name-reserved-verb
      *> rules: [SR-8.3.2.1-1, SR-13.16.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4604.  `01 MOVE PIC X.` writes the reserved verb MOVE where the entry-name clause stands.
      *> 13.16.3 SR4: "the entry-name clause shall immediately follow the level-number" and, when it is not specified,
      *> the entry is a filler (cite.py --check 13.16.3 "If no entry-name clause is specified" -> OK).  So the first
      *> word after the level-number is read as the entry-name or as the first clause, and MOVE is no clause of the
      *> 13.16.2 Format 1 general format.  The fault is the name: 8.3.2.1 1) "Reserved words shall not be used as
      *> user-defined words or system-names" (cite.py --check 8.3.2.1 -> OK).  The compiler used to answer COBOL1941,
      *> "'MOVE' is not a clause of the data description entry", which sends the reader to the wrong rule.  MOVE is
      *> reserved at every edition (8.9), so every edition refuses it by naming the reserved word.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4604A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MOVE PIC X.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SHOULD NOT COMPILE"
           STOP RUN.
