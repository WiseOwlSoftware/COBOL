      *>--- conformance-case
      *> id: g2-allocate-nonpositive-null
      *> rules: [2002-GR-14.8.3.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.3.3 GR2 - "If arithmetic-expression-1 evaluates
      *> to 0 or a negative value, the data item referenced by
      *> data-name-2 is set to the predefined address NULL."
      *> Before each NULL leg the pointer is set to the address of W,
      *> so a leg that left the pointer alone prints NOT NULL: the
      *> rule SETS data-name-2, it does not merely leave a NULL there.
      *> Legs: a zero literal, a negative data item, an expression
      *> that evaluates to 0, an expression that evaluates to a
      *> negative value, a negative non-integer (-0.5, which GR1
      *> rounds up to 0, so NULL on either reading), and the same
      *> rule with data-name-2 a level-1 record under an output CD
      *> (§2002:A.1.2.2 SR9: "Record description entries subordinate
      *> to an output CD entry implicitly redefine the record area").
      *> The control leg (4 CHARACTERS, a positive amount) is not
      *> NULL, so an ALLOCATE that always returned NULL fails it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALC01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W   PIC X(4) VALUE "WXYZ".
       01 NEG PIC S9(3) VALUE -5.
       01 ZRO PIC 9 VALUE 0.
       01 P   USAGE POINTER.
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT.
       01 CP  USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           SET P TO ADDRESS OF W
           ALLOCATE 0 CHARACTERS RETURNING P
           IF P = NULL DISPLAY "ZERO-LIT NULL"
           ELSE DISPLAY "ZERO-LIT NOT NULL" END-IF
           SET P TO ADDRESS OF W
           ALLOCATE NEG CHARACTERS RETURNING P
           IF P = NULL DISPLAY "NEG-ITEM NULL"
           ELSE DISPLAY "NEG-ITEM NOT NULL" END-IF
           SET P TO ADDRESS OF W
           ALLOCATE ZRO * 7 CHARACTERS RETURNING P
           IF P = NULL DISPLAY "ZERO-EXPR NULL"
           ELSE DISPLAY "ZERO-EXPR NOT NULL" END-IF
           SET P TO ADDRESS OF W
           ALLOCATE NEG + 2 CHARACTERS RETURNING P
           IF P = NULL DISPLAY "NEG-EXPR NULL"
           ELSE DISPLAY "NEG-EXPR NOT NULL" END-IF
           SET P TO ADDRESS OF W
           ALLOCATE NEG / 10 CHARACTERS RETURNING P
           IF P = NULL DISPLAY "NEG-FRACTION NULL"
           ELSE DISPLAY "NEG-FRACTION NOT NULL" END-IF
           SET CP TO ADDRESS OF W
           ALLOCATE NEG CHARACTERS RETURNING CP
           IF CP = NULL DISPLAY "CD-NEG NULL"
           ELSE DISPLAY "CD-NEG NOT NULL" END-IF
           SET CP TO ADDRESS OF W
           ALLOCATE 0 CHARACTERS RETURNING CP
           IF CP = NULL DISPLAY "CD-ZERO NULL"
           ELSE DISPLAY "CD-ZERO NOT NULL" END-IF
           ALLOCATE 4 CHARACTERS RETURNING P
           IF P = NULL DISPLAY "CONTROL NULL"
           ELSE DISPLAY "CONTROL NOT NULL" END-IF
           FREE P
           STOP RUN.
