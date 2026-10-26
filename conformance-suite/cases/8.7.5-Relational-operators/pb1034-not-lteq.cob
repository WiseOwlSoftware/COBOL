*>--- conformance-case
*> id: pb1034-not-lteq
*> rules: [FMT-8.7.5.1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1034. ISO 1989:2023 8.8.4.2.2 Format 1: IS <= carries no [NOT] (rendered page PDF p217), so
      *> `NOT <=` is no alternative of the format - the sibling of pb1034-not-gteq on the other OR-EQUAL symbol.
      *> The standard spells that comparison IS > (or IS GREATER THAN). Written without the optional IS, `A NOT`
      *> ends the condition at the bare operand A and the NOT starts no statement, so the parse fails at NOT
      *> (COBOL0001), as it does with IS (pb1034-not-gteq). The IF's statement is present, so this is no
      *> missing imperative-statement: COBOLNET2072 used to claim one here (kb/Work PB3832).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1034NEG2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 5.
       01 B PIC 9 VALUE 3.
       PROCEDURE DIVISION.
       MAIN.
           IF A NOT <= B
               DISPLAY "GT"
           ELSE
               DISPLAY "LE"
           END-IF
           STOP RUN.
