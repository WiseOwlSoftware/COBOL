      *>--- conformance-case
      *> id: g2-value-sr18-sr19-cd-legal
      *> rules: [2002-SR-13.16.61.2-18, 2002-SR-13.16.61.2-19]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.61.2 SR18a - "when literal-2 is of a class other
      *> than alphanumeric or national, the value of literal-2 shall be
      *> less than the value of literal-3"; SR19 - "The value of
      *> literal-4 shall not be equal to the value of any occurrence of
      *> literal-2" nor (SR19a) "to any value in the range of any
      *> occurrence of literal-2 through literal-3, inclusive".
      *> The accepted leg, in a record under a CD entry (§2002:A.1.1.2):
      *> C-LOW VALUE 1 THRU 3 ascends (1 < 3) and its FALSE value 7 is
      *> outside 1..3; C-ONE VALUE 4 6 has FALSE value 5, equal to
      *> neither 4 nor 6.  Expected (each line derived):
      *>   SET C-LOW TO TRUE: §2002:14.8.35.3 GR5 "the conditional
      *>   variable is set to the value of the first literal that
      *>   appears in the VALUE clause" -> C=1
      *>   SET C-LOW TO FALSE: §2002:13.16.61.3 GR17 "the value of
      *>   literal-4 from the FALSE phrase is placed in the associated
      *>   conditional-variable" -> C=7
      *>   MOVE 3: §2002:8.8.4.1.4.2 GR1 the variable "falls in this
      *>   range, including the end values" -> 3 LOW, 7 NOT LOW
      *>   SET C-ONE TO FALSE -> D=5, then D=5 is NOT ONE; MOVE 6 -> ONE
      *> Fails if a legal ascending range or a legal FALSE value in the
      *> communication section is refused, or if SET / the condition
      *> test uses the wrong value.  The refusal legs are
      *> g2-value-sr18-thru-cd, g2-value-sr18-thru-nat,
      *> g2-value-sr19-false-in-range-cd, g2-value-sr19-false-equal-cd
      *> and g2-value-sr19-false-nat.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2VR19K.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CREC.
           05 C PIC 9 VALUE 0.
              88 C-LOW VALUE 1 THRU 3 WHEN SET TO FALSE 7.
           05 D PIC 9 VALUE 0.
              88 C-ONE VALUES 4 6 WHEN SET TO FALSE 5.
       PROCEDURE DIVISION.
       MAIN-P.
           SET C-LOW TO TRUE
           DISPLAY "C=" C
           SET C-LOW TO FALSE
           DISPLAY "C=" C
           MOVE 3 TO C
           IF C-LOW DISPLAY "3 LOW" ELSE DISPLAY "3 NOT LOW"
               END-IF
           MOVE 7 TO C
           IF C-LOW DISPLAY "7 LOW" ELSE DISPLAY "7 NOT LOW"
               END-IF
           SET C-ONE TO FALSE
           DISPLAY "D=" D
           IF C-ONE DISPLAY "5 ONE" ELSE DISPLAY "5 NOT ONE"
               END-IF
           MOVE 6 TO D
           IF C-ONE DISPLAY "6 ONE" ELSE DISPLAY "6 NOT ONE"
               END-IF
           STOP RUN.
       END PROGRAM G2VR19K.
