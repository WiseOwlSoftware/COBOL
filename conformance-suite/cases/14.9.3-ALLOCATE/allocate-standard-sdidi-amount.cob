      *>--- conformance-case
      *> id: allocate-standard-sdidi-amount
      *> rules: [GR-14.9.3.4-1, GR-14.9.3.4-5]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> ALLOCATE arithmetic-expression-1 CHARACTERS under ARITHMETIC IS
      *> STANDARD (kb/Work PB5619). The count is a standard intermediate
      *> value, rounded and range-tested as it is, not landed at a
      *> fixed scale first.
      *> §2002:14.8.3.3 GR1: "If arithmetic-expression-1 does not
      *> evaluate to an integer, the result is rounded up to the next
      *> whole number."
      *> T1 count = 1 + 1 + 1 / 10**20 = 2.00000000000000000001, inside
      *>    the 32-digit standard intermediate. Rounded up it is 3, so
      *>    a 3-character cell: the MOVE of "ABC" fits. A scale-6
      *>    landing read it as 2 and the MOVE failed EC-BOUND-PTR.
      *> T2 count = 10 ** 40, an integer past every carrier: the
      *>    storage is not available, so P is NULL and the nonfatal
      *>    EC-STORAGE-NOT-AVAIL is handled (the same outcome as the
      *>    native-float arm, pb151_allocate); it is not a size error
      *>    that ends the run unit.
      *> ARITHMETIC IS STANDARD is a 2002-2014 clause (2023 removed it,
      *> COBOL0807), so the golden is accepted at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. ALLOCSD.
       OPTIONS.
           ARITHMETIC IS STANDARD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 B3 PIC X(3) BASED.
       01 ONE PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H1 SECTION.
           USE AFTER EXCEPTION CONDITION EC-STORAGE-NOT-AVAIL.
       H1-P.
           DISPLAY "HANDLED=EC-STORAGE-NOT-AVAIL".
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           >>TURN EC-STORAGE-NOT-AVAIL CHECKING ON
           ALLOCATE ONE + ONE + ONE / 100000000000000000000
               CHARACTERS RETURNING P.
           SET ADDRESS OF B3 TO P.
           MOVE "ABC" TO B3.
           DISPLAY "T1=" B3.
           ALLOCATE 10 ** 40 CHARACTERS RETURNING P.
           IF P = NULL DISPLAY "T2 NULL" ELSE DISPLAY "T2 CELL"
           END-IF.
           STOP RUN.
