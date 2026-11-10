      *>--- conformance-case
      *> id: 1985-GR-XIV_2_2_4-6
      *> rules: [1985-GR-XIV.2.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.4 GR6 (THE COMMUNICATION DESCRIPTION ENTRY). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from the confirmation pass's redraft (WEAK
      *> original) by scripts/spec/place_witnesses.py (kb/Work PB4995).
      * X3.23-1985 XIV 2.2.4 general rule 6: run without the MCS, the
      * INITIAL input CD's four queue names are spaces before the
      * first statement, whatever the record's VALUE clauses hold;
      * the rest of the area keeps its initial value.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDGR6.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INITIAL INPUT.
       01  CDIN-REC.
           02  CD-Q    PIC X(12) VALUE "QUEUE".
           02  CD-S1   PIC X(12) VALUE "SUB1".
           02  CD-S2   PIC X(12) VALUE "SUB2".
           02  CD-S3   PIC X(12) VALUE "SUB3".
           02  CD-DATE PIC 9(6)  VALUE 123456.
           02  FILLER  PIC X(33).
       PROCEDURE DIVISION.
       P1.
           IF CD-Q = SPACES AND CD-S1 = SPACES
              AND CD-S2 = SPACES AND CD-S3 = SPACES
               DISPLAY "QUEUE NAMES SPACES"
           ELSE
               DISPLAY "QUEUE NAMES NOT SPACES".
           DISPLAY "DATE " CD-DATE.
           STOP RUN.
