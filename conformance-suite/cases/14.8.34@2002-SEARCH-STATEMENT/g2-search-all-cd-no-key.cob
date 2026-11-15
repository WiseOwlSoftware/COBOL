      *>--- conformance-case
      *> id: g2-search-all-cd-no-key
      *> rules: [2002-SR-14.8.34.2-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR5 - "The OCCURS clause associated with identifier-1 shall contain the KEY phrase."
      *> Leg: SEARCH ALL of CTAB (CD record, OCCURS with no KEY): refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRN06.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CTAB OCCURS 3 TIMES INDEXED BY CI.
             10 CK PIC 9.
          05 CT2 OCCURS 3 TIMES ASCENDING KEY K1 K2 INDEXED BY CJ.
             10 K1 PIC 9.
             10 K2 PIC 9.
                88 K2-TWO VALUE 2.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO CK(1) MOVE 2 TO CK(2) MOVE 3 TO CK(3)
           SEARCH ALL CTAB WHEN CK(CI) = 2 DISPLAY "X" END-SEARCH
           STOP RUN.
