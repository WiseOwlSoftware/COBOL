      *>--- conformance-case
      *> id: 1985-SR-XIV_2_2_3-9
      *> rules: [1985-SR-XIV.2.2.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XIV.2.2.3 SR9 (THE COMMUNICATION DESCRIPTION ENTRY). A
      *> refusal: the rule forbids line 10 of the program below, and
      *> WiseOwl refuses it at 85 with COBOL6169 there; a compiler that
      *> accepts it fails. Placed from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CDTEST.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD OUT-CD FOR OUTPUT.
       01  OUT-REC-1.
           05  OUT-DEST-COUNT PIC 9(4) VALUE 1.
           05  FILLER         PIC X(19).
       01  OUT-REC-2.
           05  OUT-ALL        PIC X(23) VALUE SPACES.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
