*>--- conformance-case
*> id: pb3974_expression_subject_frozen_past_carrier
*> rules: [GR-14.9.13.4-3, GR-8.4.3.2.4-2]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3974 (review R1-1) - AN EXPRESSION FROZEN FOR ITS EVALUATION ORDER IS HELD EVEN WHEN
      *> ITS DIGIT BOUND PASSES THE 38-DIGIT SCALED CARRIER.  The bound of WS-B * WS-B * WS-B over
      *> PIC 9(15) is 45 digits, but its VALUE is what the item holds, and here it is 8.
      *> ISO 14.9.13.4 GR3: "At the beginning of the execution of the EVALUATE statement, each selection
      *> subject is evaluated and assigned a value" - c) for an arithmetic-expression: "a numeric value
      *> according to the rules for evaluating an arithmetic expression".  ISO 8.4.3.2.4 GR2: "its
      *> arguments are evaluated individually in the order specified in the list of arguments, from left
      *> to right", so an argument is read before a later argument's activation changes its operand.
      *> RVBMP adds 1 to the EXTERNAL WS-B and answers twice its formal (the same storage, 14.2.3 GR8),
      *> so with WS-B = 2:
      *>   CUBE3 - FUNCTION SUM (WS-B * WS-B * WS-B, FUNCTION RVBMP (WS-B)): the first argument is 8
      *>           when it is evaluated, RVBMP then answers 3 * 2 = 6, so the sum is 14 (not 27 + 6).
      *>   EVAL  - EVALUATE WS-B * WS-B * WS-B: the subject is 8 at the beginning, so the WHEN that calls
      *>           RVBMP (6, WS-B becomes 3) is false and WHEN 8 is selected (not WHEN 27).
      *>   CUBE2 - the same shape at 30 digits, inside the carrier: 4 + 6 = 10 (the control).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. RVBMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC 9(15) EXTERNAL.
       LINKAGE SECTION.
       01 L-X PIC 9(15).
       01 L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 1 TO WS-B.
           COMPUTE L-R = L-X * 2.
           GOBACK.
       END FUNCTION RVBMP.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3974FZ.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION RVBMP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-B PIC 9(15) EXTERNAL.
       01 WS-R PIC 9(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 2 TO WS-B.
           COMPUTE WS-R =
               FUNCTION SUM(WS-B * WS-B, FUNCTION RVBMP(WS-B)).
           DISPLAY "CUBE2=" WS-R " B=" WS-B.
           MOVE 2 TO WS-B.
           COMPUTE WS-R =
               FUNCTION SUM(WS-B * WS-B * WS-B, FUNCTION RVBMP(WS-B)).
           DISPLAY "CUBE3=" WS-R " B=" WS-B.
           MOVE 2 TO WS-B.
           EVALUATE WS-B * WS-B * WS-B
              WHEN FUNCTION RVBMP(WS-B) DISPLAY "EVAL=BMP"
              WHEN 8 DISPLAY "EVAL=8"
              WHEN 27 DISPLAY "EVAL=27"
              WHEN OTHER DISPLAY "EVAL=OTHER"
           END-EVALUATE.
           STOP RUN.
       END PROGRAM PB3974FZ.
