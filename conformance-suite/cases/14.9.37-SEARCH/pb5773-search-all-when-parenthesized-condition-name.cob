*>--- conformance-case
*> id: pb5773-search-all-when-parenthesized-condition-name
*> rules: [FMT-14.9.37.2]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5773 (row FMT-14.9.37.2; §1985:VI.6.22.2). Format 2's WHEN is data-name-1 IS EQUAL TO
*> {identifier-3 | literal-1 | arithmetic-expression-1} or condition-name-1, and its AND phrase the same: the
*> printed format has no parenthesis, and only Format 1's condition-1 "may be any conditional expression"
*> (§2023:14.9.37.3 SR6). A parenthesized WHEN is refused COBOL1757 at every edition, naming the parenthesis.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5773B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TBL.
          05 ENT OCCURS 3 TIMES ASCENDING KEY IS K1 INDEXED BY IX.
             10 K1 PIC X.
                88 CN VALUE "B".
             10 K2 PIC 9.
       PROCEDURE DIVISION.
       P1.
           MOVE "A1B2C3" TO TBL.
           SEARCH ALL ENT
               AT END DISPLAY "END"
               WHEN ((CN (IX)))
                   DISPLAY "HIT " K2 (IX)
           END-SEARCH.
           STOP RUN.
