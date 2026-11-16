      *>--- conformance-case
      *> id: g2-evaluate-through-cd-operands
      *> rules: [2002-GR-14.8.12.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.12.3 GR2 - "The manner of determining the range of
      *> values specified by the THROUGH phrase is described in 14.6.7".
      *> §2002:14.6.7 rule 1: when the range is defined by numeric
      *> values it "includes literal-1, literal-2, and all algebraic
      *> values between literal-1 and literal-2". The range operands
      *> (identifier-3, identifier-4) and the subject are items of a CD
      *> record.
      *>  EV : subject N2 (CD) = 50 against literal ranges -> EV=MID
      *>  EV2: subject WV against LO (CD, 12) THRU HI (CD, 20) for
      *>       WV = 12, 15, 20, 21 (both bounds are in the range)
      *>                              -> EV2=IN, IN, IN, OUT
      *>  EV3 (context, not a witness of GR2): HI THRU LO with WV = 15.
      *>       §2002:14.8.12.3 GR4 - "If the selection object is a
      *>       range-expression, the pair is considered to be a
      *>       conditional expression" of the form subject >= left-part
      *>       AND subject <= right-part, false for 15 >= 20 -> EV3=OUT
      *> Why each leg can fail: a range test that excludes a bound, or
      *> CD items not read as range operands.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2EVL01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WV PIC 9(3).
       01 K  PIC 9.
       01 VT.
          05 FILLER PIC 9(3) VALUE 12.
          05 FILLER PIC 9(3) VALUE 15.
          05 FILLER PIC 9(3) VALUE 20.
          05 FILLER PIC 9(3) VALUE 21.
       01 VR REDEFINES VT.
          05 VV PIC 9(3) OCCURS 4.
       COMMUNICATION SECTION.
       CD CDIN FOR INPUT
           SYMBOLIC QUEUE IS SQ.
       01 INREC.
          05 N2     PIC 9(3).
          05 LO     PIC 9(3).
          05 HI     PIC 9(3).
          05 FILLER PIC X(78).
       PROCEDURE DIVISION.
       MAIN.
           MOVE 50 TO N2.
           EVALUATE N2
               WHEN 0 THRU 9 DISPLAY "EV=LOW"
               WHEN 10 THRU 99 DISPLAY "EV=MID"
               WHEN OTHER DISPLAY "EV=HIGH"
           END-EVALUATE.
           MOVE 12 TO LO. MOVE 20 TO HI.
           PERFORM VARYING K FROM 1 BY 1 UNTIL K > 4
               MOVE VV(K) TO WV
               EVALUATE WV
                   WHEN LO THRU HI DISPLAY "EV2=IN"
                   WHEN OTHER DISPLAY "EV2=OUT"
               END-EVALUATE
           END-PERFORM.
           MOVE 15 TO WV.
           EVALUATE WV
               WHEN HI THRU LO DISPLAY "EV3=IN"
               WHEN OTHER DISPLAY "EV3=OUT"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM G2EVL01.
