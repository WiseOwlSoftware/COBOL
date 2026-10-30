*>--- conformance-case
*> id: pb4124_held_function_argument_keeps_description
*> rules: [GR-8.4.3.2.4-2, RV-15.4.1-1, DOC-A.1-92]
*> accept: [2002, 2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4124 - ISO §8.4.3.2.4 GR2: "At the time reference is made to a function, its arguments are evaluated
      *> individually in the order specified in the list of arguments, from left to right." (cite.py: OK 8.4.3.2.4 2)).
      *> GR2 orders the evaluation and nothing else, so an earlier argument that a LATER argument's user-defined function
      *> activation could change is evaluated first and HELD, and the call's result is the result it would have had
      *> without the activation: §15.4.1's "the returned value is the same for all instances of a given function ... so long
      *> as the value and order of the arguments ... are unchanged" (cite.py: OK 15.4.1), and §15.4: "The evaluation of a
      *> function produces a returned value in a temporary elementary data item" (cite.py: OK 15.4). The temporary's
      *> description is implementor-defined under native arithmetic (§15.4.1; docs/CONFORMANCE.md DOC-A.1-92 gives
      *> GnuCOBOL 3.2.0's: MAX, MIN and MEDIAN return a COPY of the selected argument's item, an arithmetic expression is
      *> described by its value), so no length is asserted here; what is asserted is that holding an argument changes
      *> NEITHER the item the selection returns NOR its length. FUNCTION LENGTH answers "the length of argument-1 in
      *> alphanumeric character positions" (§15.50.4 3), cite.py: OK 15.50.4 3)) and FUNCTION BYTE-LENGTH "the length of
      *> argument-1 in number of bytes" (§15.14.4 1), cite.py: OK 15.14.4 1)), of the temporary a function-identifier
      *> references (§8.4.3.2.4 r1), so each pair below measures the SAME call twice, once with a literal filler argument
      *> (nothing to hold) and once with a user-defined function activation as the filler (the argument to its left is held).
      *> The filler never wins the selection, so both calls select the same argument, whose item is the item measured.
      *> Before the fix the held argument was described by the intermediate item it was held in: the pair EXPR measured 4
      *> and 8, NEST measured 8 and 38, NESTLIT 1 and 38 (the nested MAX selected the literal 5).
      *>   EXPR   - an arithmetic expression argument: LENGTH.
      *>   NEST   - a nested MAX that selects the data item WS-C (PIC 9(8)): LENGTH and BYTE-LENGTH.
      *>   NESTLIT- a nested MAX that selects the literal 5: LENGTH.
      *>   MIN    - a nested MIN over an expression and an item, under a MIN: BYTE-LENGTH.
      *>   MEDIAN - MEDIAN, an odd count, selects the middle argument WS-C: LENGTH.
      *>   ORDER  - GR2 itself: MIN (MAX (WS-O, 1), activation that adds 1 to WS-O) - the nested MAX is evaluated BEFORE the
      *>            activation, 4, so the minimum of 4 and 99999999 is 4 (read after it, 5).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4124F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-O PIC 9(4) EXTERNAL.
       LINKAGE SECTION.
       01 L-X PIC 9(8).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 1 TO WS-O.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION PB4124F.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4124.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB4124F
           FUNCTION MAX INTRINSIC
           FUNCTION MIN INTRINSIC
           FUNCTION MEDIAN INTRINSIC
           FUNCTION LENGTH INTRINSIC
           FUNCTION BYTE-LENGTH INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-O  PIC 9(4) EXTERNAL.
       01 WS-A  PIC 9(4) VALUE 7.
       01 WS-B  PIC 9(4) VALUE 3.
       01 WS-C  PIC 9(8) VALUE 21.
       01 WS-Z  PIC 9(4) VALUE 0.
       01 WS-LO PIC 9(8) VALUE 0.
       01 WS-HI PIC 9(8) VALUE 99999999.
       01 WS-U  PIC 9(4).
       01 WS-F  PIC 9(4).
       01 WS-R  PIC 9(8).
       PROCEDURE DIVISION.
       MAIN.
           MOVE FUNCTION LENGTH (FUNCTION MAX (WS-A * WS-B, 0)) TO WS-U
           MOVE FUNCTION LENGTH (FUNCTION MAX (WS-A * WS-B, FUNCTION PB4124F (WS-LO))) TO WS-F
           PERFORM REPORT-PAIR-EXPR
           MOVE FUNCTION LENGTH (FUNCTION MAX (FUNCTION MAX (WS-C, 5), 0)) TO WS-U
           MOVE FUNCTION LENGTH (FUNCTION MAX (FUNCTION MAX (WS-C, 5), FUNCTION PB4124F (WS-LO))) TO WS-F
           PERFORM REPORT-PAIR-NEST-LENGTH
           MOVE FUNCTION BYTE-LENGTH (FUNCTION MAX (FUNCTION MAX (WS-C, 5), 0)) TO WS-U
           MOVE FUNCTION BYTE-LENGTH (FUNCTION MAX (FUNCTION MAX (WS-C, 5), FUNCTION PB4124F (WS-LO))) TO WS-F
           PERFORM REPORT-PAIR-NEST-BYTES
           MOVE FUNCTION LENGTH (FUNCTION MAX (FUNCTION MAX (WS-Z, 5), 0)) TO WS-U
           MOVE FUNCTION LENGTH (FUNCTION MAX (FUNCTION MAX (WS-Z, 5), FUNCTION PB4124F (WS-LO))) TO WS-F
           PERFORM REPORT-PAIR-NESTLIT
           MOVE FUNCTION BYTE-LENGTH (FUNCTION MIN (FUNCTION MIN (WS-C, WS-A * WS-B), 99999999)) TO WS-U
           MOVE FUNCTION BYTE-LENGTH (FUNCTION MIN (FUNCTION MIN (WS-C, WS-A * WS-B), FUNCTION PB4124F (WS-HI))) TO WS-F
           PERFORM REPORT-PAIR-MIN
           MOVE FUNCTION LENGTH (FUNCTION MEDIAN (WS-C, 0, 99999999)) TO WS-U
           MOVE FUNCTION LENGTH (FUNCTION MEDIAN (WS-C, 0, FUNCTION PB4124F (WS-HI))) TO WS-F
           PERFORM REPORT-PAIR-MEDIAN
           MOVE 4 TO WS-O
           COMPUTE WS-R = FUNCTION MIN (FUNCTION MAX (WS-O, 1), FUNCTION PB4124F (WS-HI))
           DISPLAY "ORDER=" WS-R " O=" WS-O
           STOP RUN.
       REPORT-PAIR-EXPR.
           IF WS-U = WS-F DISPLAY "EXPR LENGTH SAME" ELSE DISPLAY "EXPR LENGTH DIFFERENT" END-IF.
       REPORT-PAIR-NEST-LENGTH.
           IF WS-U = WS-F DISPLAY "NEST LENGTH SAME" ELSE DISPLAY "NEST LENGTH DIFFERENT" END-IF.
       REPORT-PAIR-NEST-BYTES.
           IF WS-U = WS-F DISPLAY "NEST BYTES SAME" ELSE DISPLAY "NEST BYTES DIFFERENT" END-IF.
       REPORT-PAIR-NESTLIT.
           IF WS-U = WS-F DISPLAY "NESTLIT LENGTH SAME" ELSE DISPLAY "NESTLIT LENGTH DIFFERENT" END-IF.
       REPORT-PAIR-MIN.
           IF WS-U = WS-F DISPLAY "MIN BYTES SAME" ELSE DISPLAY "MIN BYTES DIFFERENT" END-IF.
       REPORT-PAIR-MEDIAN.
           IF WS-U = WS-F DISPLAY "MEDIAN LENGTH SAME" ELSE DISPLAY "MEDIAN LENGTH DIFFERENT" END-IF.
       END PROGRAM PB4124.
