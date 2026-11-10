      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-28
      *> rules: [1985-GR-XIV.2.2.4-28]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR28 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 28: run without the MCS, the
      * INITIAL input-output CD's terminal name is spaces before the
      * first statement, whatever the record's VALUE clause holds;
      * the rest of the area keeps its initial value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR28.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  IOCD FOR INITIAL I-O.
       01  IOCD-REC.
           02  IO-DATE PIC 9(6)  VALUE 123456.
           02  IO-TIME PIC 9(8)  VALUE 12345678.
           02  IO-TERM PIC X(12) VALUE "TERMINAL".
           02  FILLER  PIC X(7).
       PROCEDURE DIVISION.
       P1.
           IF IO-TERM = SPACES
               DISPLAY "TERMINAL SPACES"
           ELSE
               DISPLAY "TERMINAL NOT SPACES".
           DISPLAY "DATE " IO-DATE " TIME " IO-TIME.
           STOP RUN.
