      *>--- conformance-case
      *> id: g85_bf15_04
      *> rules: [1985-GR-VI.6.9.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.9.4 GR1 - "The CONTINUE statement has no effect on
      *> the execution of the program." CONTINUE is placed as a
      *> sentence, as each branch of an IF, inside an IF scope followed
      *> by more statements of the sentence, as the only statement of a
      *> performed paragraph, in an EVALUATE WHEN and in a SEARCH WHEN.
      *> Each leg's result is what the program computes with every
      *> CONTINUE deleted:
      *> C1 N = 0, CONTINUE, ADD 1: N=1
      *> C2 IF N = 1 CONTINUE ELSE MOVE 9: N stays 1
      *> C3 IF N = 2 MOVE 9 ELSE CONTINUE: N stays 1
      *> C4 PERFORM EMPTY-PARA (CONTINUE), ADD 1: N=2
      *> C5 IF N = 2 CONTINUE END-IF ADD 1 in one sentence: N=3
      *> C6 EVALUATE N WHEN 3 CONTINUE WHEN OTHER MOVE 9: N stays 3
      *> C7 SEARCH E WHEN E (I) = "B" CONTINUE: control reaches the
      *>    end of the SEARCH with I at occurrence 2, K=2
      *> A CONTINUE that transfers control or alters data fails a leg.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1504.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N   PIC 9 VALUE 0.
       01  K   PIC 9 VALUE 0.
       01  T-VALS PIC X(3) VALUE "ABC".
       01  T REDEFINES T-VALS.
           05  E PIC X OCCURS 3 TIMES INDEXED BY I.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 0 TO N.
           CONTINUE.
           ADD 1 TO N.
           DISPLAY "C1 N=" N.
           IF N = 1 CONTINUE ELSE MOVE 9 TO N.
           DISPLAY "C2 N=" N.
           IF N = 2 MOVE 9 TO N ELSE CONTINUE.
           DISPLAY "C3 N=" N.
           PERFORM EMPTY-PARA.
           ADD 1 TO N.
           DISPLAY "C4 N=" N.
           IF N = 2
               CONTINUE
           END-IF
           ADD 1 TO N
           DISPLAY "C5 N=" N.
           EVALUATE N
               WHEN 3 CONTINUE
               WHEN OTHER MOVE 9 TO N
           END-EVALUATE.
           DISPLAY "C6 N=" N.
           SET I TO 1.
           SEARCH E
               AT END MOVE 9 TO K
               WHEN E (I) = "B" CONTINUE
           END-SEARCH.
           SET K TO I.
           DISPLAY "C7 K=" K.
           STOP RUN.
       EMPTY-PARA.
           CONTINUE.
