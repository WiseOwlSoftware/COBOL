*>--- conformance-case
*> id: pb5193-search-all-condition-index-plus-cites-2014
*> rules: [SR-14.9.37.3-9]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5193 - the SEARCH ALL subscript refusal cites the rule of the edition compiled: §2014:14.9.34.2 SR7.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5193CONDPLUS2014.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T.
          05 E OCCURS 5 TIMES
               ASCENDING KEY IS K
               INDEXED BY IX JX.
             10 K  PIC 9(2).
                88 K-IS-FIVE VALUE 05.
             10 V  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           SEARCH ALL E
               AT END DISPLAY "NONE"
               WHEN K-IS-FIVE (IX + 1)
                   DISPLAY "HIT"
           END-SEARCH.
           STOP RUN.
