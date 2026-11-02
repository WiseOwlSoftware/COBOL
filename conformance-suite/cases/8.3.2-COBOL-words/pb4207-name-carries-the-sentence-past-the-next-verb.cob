*>--- conformance-case
*> id: pb4207-name-carries-the-sentence-past-the-next-verb
*> rules: [SR-8.3.2.1-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4207, review R3-1.  RECEIVE is written as the receiving operand of the MOVE, and ISO 8.3.2.1 1) ("Reserved
      *> words shall not be used as user-defined words or system-names", cite.py --check 8.3.2.1 -> OK) is what that breaks:
      *> the first diagnostic is COBOL0901 at the word.  RECEIVE W-Y also reads as the start of a RECEIVE statement (14.9.31.2
      *> prints "RECEIVE FROM data-name-1 GIVING identifier-1 data-name-2"; cite.py --check 14.9.31.2 "GIVING" -> OK), but that
      *> reading is refused at the DISPLAY after two tokens, while the word read as the name carries the sentence on to the
      *> real fault in it, a DISPLAY with nothing after UPON (14.9.11.2 prints "UPON mnemonic-name-1"; cite.py --check 14.9.11.2
      *> "UPON" -> OK).  Of the two readings the one that goes further is the one the report names, so the correct DISPLAY
      *> before the UPON draws no diagnostic of its own and the UPON fault is reported.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4207NAM.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-X PIC X(4).
       01 W-Y PIC X(4).
       01 W-Z PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE W-X TO RECEIVE W-Y
           DISPLAY W-Z
           DISPLAY W-Z UPON.
           STOP RUN.
