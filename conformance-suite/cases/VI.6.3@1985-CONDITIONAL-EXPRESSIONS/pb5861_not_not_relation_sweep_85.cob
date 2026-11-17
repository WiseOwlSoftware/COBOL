      *>--- conformance-case
      *> id: pb5861_not_not_relation_sweep_85
      *> rules: [1985-SR-VI.6.3.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5861 - §1985:VI.6.3.3 GRL2.1: "then the NOT
      *> participates as part of the relational operator" when the word
      *> after it is GREATER, >, LESS, <, EQUAL or =; otherwise GRL2.2:
      *> "The NOT is interpreted as a logical operator". In AND NOT NOT
      *> < 5 the word after the first NOT is NOT, so the first is the
      *> logical operator and NOT < the relational operator: A > 1 AND
      *> NOT (A NOT < 5). The stated operator is carried on (GR1), and
      *> SR2's table 1 bars only two LOGICAL NOTs. A = 3:
      *>   S1 AND NOT NOT > 5            NOT (3 NOT > 5)  = F
      *>   S2 AND NOT NOT GREATER THAN 2 NOT (3 NOT > 2)  = T
      *>   S3 AND NOT NOT LESS 2         NOT (3 NOT < 2)  = F
      *>   S4 AND NOT NOT = 3            NOT (3 NOT = 3)  = T
      *>   S5 AND NOT NOT EQUAL TO 4     NOT (3 NOT = 4)  = F
      *>   S6 AND NOT NOT LESS THAN 5    NOT (3 NOT < 5)  = T
      *>   S7 A > 5 OR NOT NOT < 2       F OR NOT (T)     = F
      *>   S8 A > 5 OR NOT NOT < 5       F OR NOT (F)     = T
      *>   S9 ... NOT NOT < 5 AND 4      T AND T AND (3 NOT < 4) = F
      *>   SA A = 9 OR 3 AND NOT NOT < 5 F OR (T AND T)   = T
      *>   SB the S6 condition in parentheses             = T
      *>   SC under EVALUATE TRUE WHEN                    = T
      *>   SD PERFORM UNTIL K > 0 AND NOT NOT > 2: K > 2, so K = 3
      *>   SE SEARCH WHEN E (X) > 1 AND NOT NOT < 4: E = 1 2 3 4 5,
      *>      the first E > 1 with E < 4 is E (2) = 2
      *> The defect refused every NOT NOT form (COBOL0001 near NOT).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5861A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       77  A PIC 9 VALUE 3.
       77  K PIC 9 VALUE 0.
       01  TV PIC X(5) VALUE "12345".
       01  TR REDEFINES TV.
           05  E PIC 9 OCCURS 5 INDEXED BY X.
       PROCEDURE DIVISION.
       P1.
           IF A > 1 AND NOT NOT > 5 DISPLAY "S1 T" ELSE DISPLAY "S1 F".
           IF A > 1 AND NOT NOT GREATER THAN 2
               DISPLAY "S2 T" ELSE DISPLAY "S2 F".
           IF A > 1 AND NOT NOT LESS 2 DISPLAY "S3 T"
               ELSE DISPLAY "S3 F".
           IF A > 1 AND NOT NOT = 3 DISPLAY "S4 T" ELSE DISPLAY "S4 F".
           IF A > 1 AND NOT NOT EQUAL TO 4 DISPLAY "S5 T"
               ELSE DISPLAY "S5 F".
           IF A > 1 AND NOT NOT LESS THAN 5 DISPLAY "S6 T"
               ELSE DISPLAY "S6 F".
           IF A > 5 OR NOT NOT < 2 DISPLAY "S7 T" ELSE DISPLAY "S7 F".
           IF A > 5 OR NOT NOT < 5 DISPLAY "S8 T" ELSE DISPLAY "S8 F".
           IF A > 1 AND NOT NOT < 5 AND 4 DISPLAY "S9 T"
               ELSE DISPLAY "S9 F".
           IF A = 9 OR 3 AND NOT NOT < 5 DISPLAY "SA T"
               ELSE DISPLAY "SA F".
           IF (A > 1 AND NOT NOT LESS THAN 5) DISPLAY "SB T"
               ELSE DISPLAY "SB F".
           EVALUATE TRUE
               WHEN A > 1 AND NOT NOT < 5 DISPLAY "SC T"
               WHEN OTHER DISPLAY "SC F"
           END-EVALUATE.
           PERFORM P2 UNTIL K > 0 AND NOT NOT > 2.
           DISPLAY "SD " K.
           SET X TO 1.
           SEARCH E AT END DISPLAY "SE END"
               WHEN E (X) > 1 AND NOT NOT < 4 DISPLAY "SE " E (X).
           STOP RUN.
       P2.
           ADD 1 TO K.
