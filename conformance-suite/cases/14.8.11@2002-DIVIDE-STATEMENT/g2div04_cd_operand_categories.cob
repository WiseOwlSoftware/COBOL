      *>--- conformance-case
      *> id: g2div04_cd_operand_categories
      *> rules: [2002-SR-14.8.11.2-1, 2002-SR-14.8.11.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.11.2 SR1 - "Identifier-1 and identifier-2 shall
      *> reference an elementary data item of category numeric."
      *> §2002:14.8.11.2 SR2 - "Identifier-3 and identifier-4 shall
      *> reference an elementary data item of category numeric or
      *> numeric-edited." Every DIVIDE here takes a permitted operand
      *> from an output CD: DESTINATION COUNT DC is "an integer"
      *> (§2002:A.1.2.3 GR16a) and TEXT LENGTH TL "an elementary data
      *> item whose implicit description is that of an integer of 4
      *> digits" (GR16b), so both are numeric. Native arithmetic,
      *> truncation (§2002:14.6.3); GR7 remainder = dividend minus
      *> subsidiary quotient times divisor.
      *>  D1 SR1 id-1 DC and id-2 TL: 17 / 2 = 8.5    -> D1 TL=0008
      *>  D2 SR1 id-1 DC: 17 / 2, Q = 8, R = 1        -> D2 Q=0008 R=0001
      *>  D3 SR1 id-2 TL BY id-1 DC, SR2 numeric-edited id-3 E PIC ZZZ9:
      *>     8 / 2 = 4                                -> D3 E=   4
      *>  D4 SR2 numeric-edited id-4 RE PIC ZZ9: 17 / 2, Q = 8,
      *>     R = 17 - 16 = 1                          -> D4 Q=0008 RE=  1
      *>  D5 SR2 CD id-3 TL and id-4 DC: 30 / 7, TL = 4, DC = 30 - 28
      *>                                              -> D5 TL=0004 DC=0002
      *> The refusals beside it (g2div05..g2div09) take an alphanumeric
      *> or group CD operand. Each line fails if a CD field's implicit
      *> numeric category is not seen (a refusal) or it is not stored
      *> as a receiver. COBOL0903 (the obsolete communication facility)
      *> is a warning.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2DIV04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 Q  PIC 9(4) VALUE 0.
       01 R  PIC 9(4) VALUE 0.
       01 E  PIC ZZZ9.
       01 RE PIC ZZ9.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       CD CDIN FOR INPUT.
       01 INREC.
          05 IQ     PIC X(12).
          05 FILLER PIC X(75).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 17 TO TL. MOVE 2 TO DC.
           DIVIDE DC INTO TL.
           DISPLAY "D1 TL=" TL.
           DIVIDE DC INTO 17 GIVING Q REMAINDER R.
           DISPLAY "D2 Q=" Q " R=" R.
           DIVIDE TL BY DC GIVING E.
           DISPLAY "D3 E=" E.
           MOVE 0 TO Q.
           DIVIDE 17 BY DC GIVING Q REMAINDER RE.
           DISPLAY "D4 Q=" Q " RE=" RE.
           DIVIDE 7 INTO 30 GIVING TL REMAINDER DC.
           DISPLAY "D5 TL=" TL " DC=" DC.
           STOP RUN.
       END PROGRAM G2DIV04.
