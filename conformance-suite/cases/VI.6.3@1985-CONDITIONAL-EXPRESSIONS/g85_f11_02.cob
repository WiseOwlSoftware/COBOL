      *>--- conformance-case
      *> id: g85_f11_02
      *> rules: [1985-SR-VI.6.3.3-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.3.3 SR2 - omission of subject AND relational
      *> operator in an abbreviated combined relation condition. "The
      *> effect of using such abbreviations is as if the last preceding
      *> stated subject were inserted in place of the omitted subject,
      *> and the last stated relational operator were inserted in place
      *> of the omitted relational operator." "This insertion of an
      *> omitted subject and/or relational operator terminates once a
      *> complete simple condition is encountered within a complex
      *> condition." A=5 B=3 C=7 D=9 E=5; expansions:
      *>   T1 A = B OR E           (A=B) OR (A=E)            Y
      *>   T2 A = B OR C           (A=B) OR (A=C)            N
      *>   T3 A > B AND < C AND D  (A>B) AND (A<C) AND (A<D) Y
      *>      (both forms; D takes the LAST stated operator <, not >)
      *>   T4 A = B OR C = D OR E  (A=B) OR (C=D) OR (C=E)   N
      *>      (C = D is complete: insertion restarts from C, not A)
      *>   T5 A < B OR > C OR B    (A<B) OR (A>C) OR (A>B)   Y
      *>   T6 A IS NOT LESS THAN B AND C
      *>                           (A NOT < B) AND (A NOT < C) N
      *>   T7 A = B + 2 AND C - 2  (A=B+2) AND (A=C-2)       Y
      *> Each leg fails if the wrong subject or operator is inserted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F1102.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A  PIC 9 VALUE 5.
       01  B  PIC 9 VALUE 3.
       01  C  PIC 9 VALUE 7.
       01  D  PIC 9 VALUE 9.
       01  E  PIC 9 VALUE 5.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF A = B OR E
               DISPLAY "T1 Y"
           ELSE
               DISPLAY "T1 N".
           IF A = B OR C
               DISPLAY "T2 Y"
           ELSE
               DISPLAY "T2 N".
           IF A > B AND < C AND D
               DISPLAY "T3 Y"
           ELSE
               DISPLAY "T3 N".
           IF A = B OR C = D OR E
               DISPLAY "T4 Y"
           ELSE
               DISPLAY "T4 N".
           IF A < B OR > C OR B
               DISPLAY "T5 Y"
           ELSE
               DISPLAY "T5 N".
           IF A IS NOT LESS THAN B AND C
               DISPLAY "T6 Y"
           ELSE
               DISPLAY "T6 N".
           IF A = B + 2 AND C - 2
               DISPLAY "T7 Y"
           ELSE
               DISPLAY "T7 N".
           STOP RUN.
