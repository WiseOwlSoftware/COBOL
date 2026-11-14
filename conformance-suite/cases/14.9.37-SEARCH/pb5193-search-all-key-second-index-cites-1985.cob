*>--- conformance-case
*> id: pb5193-search-all-key-second-index-cites-1985
*> rules: [SR-14.9.37.3-8]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5193 - the SEARCH ALL subscript refusal cites the rule of the edition compiled: §1985:VI.6.22.3 SR4.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5193KEYSECOND85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX JX.
             10 K  PIC 9(2).
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K (JX) = 05
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
