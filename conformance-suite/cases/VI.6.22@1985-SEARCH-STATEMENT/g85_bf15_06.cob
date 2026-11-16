      *>--- conformance-case
      *> id: g85_bf15_06
      *> rules: [1985-GR-VI.6.22.4-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.22.4 GR5 - "After execution of
      *> imperative-statement-1 or imperative-statement-2, that does
      *> not terminate with a GO TO statement, control passes to the
      *> end of the SEARCH statement."
      *> Format 1 (serial) over E = "ABC":
      *> F1 two WHENs both true at "B": only the first imperative runs,
      *>    then control is at the end of the SEARCH: F1 WHEN-1, F1 END
      *> F2 no match: AT END imperative, then the end: F2 AT END, F2 END
      *> F3 the AT END imperative ends in GO TO F3-OUT: F3 AT END then
      *>    F3 OUT; the DISPLAY after END-SEARCH is not executed
      *> F4 WHEN imperative ends in GO TO F4-OUT: F4 WHEN, F4 OUT
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1506.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-VALS PIC X(3) VALUE "ABC".
       01  T REDEFINES T-VALS.
           05  E PIC X OCCURS 3 TIMES INDEXED BY I.
       PROCEDURE DIVISION.
       F1-PARA.
           SET I TO 1.
           SEARCH E
               AT END DISPLAY "F1 AT END"
               WHEN E (I) = "B" DISPLAY "F1 WHEN-1"
               WHEN E (I) = "B" DISPLAY "F1 WHEN-2"
           END-SEARCH
           DISPLAY "F1 END".
       F2-PARA.
           SET I TO 1.
           SEARCH E
               AT END DISPLAY "F2 AT END"
               WHEN E (I) = "Z" DISPLAY "F2 WHEN"
           END-SEARCH
           DISPLAY "F2 END".
       F3-PARA.
           SET I TO 1.
           SEARCH E
               AT END DISPLAY "F3 AT END"
                   GO TO F3-OUT
               WHEN E (I) = "Z" DISPLAY "F3 WHEN"
           END-SEARCH
           DISPLAY "F3 NOT REACHED".
       F3-OUT.
           DISPLAY "F3 OUT".
       F4-PARA.
           SET I TO 1.
           SEARCH E
               AT END DISPLAY "F4 AT END"
               WHEN E (I) = "C" DISPLAY "F4 WHEN"
                   GO TO F4-OUT
           END-SEARCH
           DISPLAY "F4 NOT REACHED".
       F4-OUT.
           DISPLAY "F4 OUT".
           STOP RUN.
