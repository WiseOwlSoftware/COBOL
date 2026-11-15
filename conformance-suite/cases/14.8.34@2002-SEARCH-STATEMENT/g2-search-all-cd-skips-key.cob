      *>--- conformance-case
      *> id: g2-search-all-cd-skips-key
      *> rules: [2002-SR-14.8.34.2-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.34.2 SR9 - "all preceding data-names in that KEY phrase or their associated condition-names shall also be referenced"
      *> Leg: WHEN K2(CJ) = 2 without the preceding key K1 (CD table): refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRN07.
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
           SEARCH ALL CT2 WHEN K2(CJ) = 2 DISPLAY "Y" END-SEARCH
           STOP RUN.
