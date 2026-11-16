      *>--- conformance-case
      *> id: g2-search-all-cd-keys
      *> rules: [2002-SR-14.8.34.2-5, 2002-SR-14.8.34.2-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> SEARCH ALL of tables in a record under an output CD
      *> (§2002:A.1.2.2 SR9; the record's entries are ordinary data
      *> description entries, §2002:A.1.1.2). The CD facility is
      *> obsolete (COBOL0903 warning, not an error).
      *> §2002:14.8.34.2 SR5 - "The OCCURS clause associated with
      *> identifier-1 shall contain the KEY phrase." CTAB has
      *> ASCENDING KEY CK: CK 1,2,3 / CV A,B,C. Lines 1-2 pin SR5:
      *>   CK = 2 -> FOUND B ; CK = 4 -> AT END CTAB.
      *> §2002:14.8.34.2 SR9 - "all preceding data-names in that KEY
      *> phrase or their associated condition-names shall also be
      *> referenced". CT2 has ASCENDING KEY K1 K2, rows (1,1) P,
      *> (2,3) Q, (2,5) R. Lines 3-6 pin SR9's permitted forms:
      *>   K1 alone (no preceding key) = 1        -> FOUND P
      *>   K1 = 2 AND K2 = 5                      -> FOUND R
      *>   condition-name K1-TWO (of K1) AND K2=3 -> FOUND Q
      *>   K1 = 2 AND K2 = 4 (no such row)        -> AT END CT2
      *> Fails if a CD table with KEY is refused, if a key reference
      *> with all its preceding keys is refused, or if the search
      *> finds the wrong element.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CREC.
          05 CTAB OCCURS 3 TIMES ASCENDING KEY CK INDEXED BY CI.
             10 CK PIC 9.
             10 CV PIC X.
          05 CT2 OCCURS 3 TIMES ASCENDING KEY K1 K2 INDEXED BY CJ.
             10 K1 PIC 9.
                88 K1-TWO VALUE 2.
             10 K2 PIC 9.
             10 KV PIC X.
       PROCEDURE DIVISION.
       MAIN.
           MOVE 1 TO CK(1) MOVE 2 TO CK(2) MOVE 3 TO CK(3)
           MOVE "A" TO CV(1) MOVE "B" TO CV(2) MOVE "C" TO CV(3)
           MOVE 1 TO K1(1) MOVE 1 TO K2(1) MOVE "P" TO KV(1)
           MOVE 2 TO K1(2) MOVE 3 TO K2(2) MOVE "Q" TO KV(2)
           MOVE 2 TO K1(3) MOVE 5 TO K2(3) MOVE "R" TO KV(3)
           SEARCH ALL CTAB
             AT END DISPLAY "AT END CTAB"
             WHEN CK(CI) = 2 DISPLAY "FOUND " CV(CI)
           END-SEARCH
           SEARCH ALL CTAB
             AT END DISPLAY "AT END CTAB"
             WHEN CK(CI) = 4 DISPLAY "FOUND " CV(CI)
           END-SEARCH
           SEARCH ALL CT2
             AT END DISPLAY "AT END CT2"
             WHEN K1(CJ) = 1 DISPLAY "FOUND " KV(CJ)
           END-SEARCH
           SEARCH ALL CT2
             AT END DISPLAY "AT END CT2"
             WHEN K1(CJ) = 2 AND K2(CJ) = 5 DISPLAY "FOUND " KV(CJ)
           END-SEARCH
           SEARCH ALL CT2
             AT END DISPLAY "AT END CT2"
             WHEN K1-TWO(CJ) AND K2(CJ) = 3 DISPLAY "FOUND " KV(CJ)
           END-SEARCH
           SEARCH ALL CT2
             AT END DISPLAY "AT END CT2"
             WHEN K1(CJ) = 2 AND K2(CJ) = 4 DISPLAY "FOUND " KV(CJ)
           END-SEARCH
           STOP RUN.
