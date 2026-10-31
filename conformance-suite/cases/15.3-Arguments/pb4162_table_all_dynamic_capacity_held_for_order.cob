*>--- conformance-case
*> id: pb4162_table_all_dynamic_capacity_held_for_order
*> rules: [GR-8.4.3.2.4-2, AR-15.3-14]
*> accept: [2014, 2023]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4162 - the dynamic-capacity leg of pb4162_table_all_argument_held_for_order. ISO §8.4.3.2.4 GR2 evaluates a
      *> function's arguments "individually in the order specified in the list of arguments, from left to right" (cite.py: OK
      *> 8.4.3.2.4 2)), and §15.3 14) makes T (ALL) a sequence of arguments, each evaluated at its place in the list; "If the
      *> subscript ALL is specified for a dynamic-capacity table, the range of values of the subscript is from 1 to the current
      *> capacity of the table" (cite.py: OK 15.3 14)). The range is read when the argument is evaluated, so a later argument's
      *> user-defined function activation that lowers the capacity (SET CAP TO 1, §14.9.39 Format 14) cannot shorten it.
      *> The activation (PB4162D) adds 100 to the first three occurrences of D and 5 to the occurrences of the nested table ND,
      *> and sets the capacity of D to 1; the answers are over the occurrences the tables held when the call was reached:
      *>   DYN-SUM  SUM (D (ALL), 0) = 10 + 20 + 30 = 60;  afterwards CAP = 1 and D (1) = 110
      *>   DYN-OMAX ORD-MAX (D (ALL), 0) = 3 (§15.71.4 1), cite.py: OK 15.71.4 1)) - the third occurrence is the greatest
      *>   DYN-NEST MAX (MAX (D (ALL), 1), 0) = 30, the nested call naming ALL held as the call it is
      *>   NEST     SUM (ND (ALL, ALL), 0) with ND = 1 2 / 3 (a dynamic-capacity table nested in a fixed table has a capacity PER
      *>            OUTER OCCURRENCE: row 1 holds two, row 2 one) = 6;  afterwards ND (1, 1) = 6
      *> Read after the activation the sums would be 110 and 21.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB4162D.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DYN EXTERNAL.
          05 D OCCURS DYNAMIC CAPACITY IN CAP PIC 9(3).
       01 NEST EXTERNAL.
          05 NR OCCURS 2 TIMES.
             10 ND OCCURS DYNAMIC PIC 9(2).
       LINKAGE SECTION.
       01 L-X PIC 9(8).
       01 L-R PIC 9(8).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       P.
           ADD 100 TO D (1) D (2) D (3).
           ADD 5 TO ND (1, 1) ND (1, 2) ND (2, 1).
           SET CAP TO 1.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION PB4162D.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4162DYN.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION PB4162D
           FUNCTION ALL INTRINSIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 DYN EXTERNAL.
          05 D OCCURS DYNAMIC CAPACITY IN CAP PIC 9(3).
       01 NEST EXTERNAL.
          05 NR OCCURS 2 TIMES.
             10 ND OCCURS DYNAMIC PIC 9(2).
       01 WS-LO PIC 9(8) VALUE 0.
       01 WS-R  PIC S9(5).
       01 WS-C  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (D (ALL), FUNCTION PB4162D (WS-LO))
           MOVE CAP TO WS-C
           DISPLAY "DYN-SUM=" WS-R " CAP=" WS-C " D1=" D (1)
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION ORD-MAX (D (ALL), FUNCTION PB4162D (WS-LO))
           MOVE CAP TO WS-C
           DISPLAY "DYN-OMAX=" WS-R " CAP=" WS-C
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION MAX (FUNCTION MAX (D (ALL), 1), FUNCTION PB4162D (WS-LO))
           MOVE CAP TO WS-C
           DISPLAY "DYN-NEST=" WS-R " CAP=" WS-C
           PERFORM LOAD-DATA
           COMPUTE WS-R = FUNCTION SUM (ND (ALL, ALL), FUNCTION PB4162D (WS-LO))
           DISPLAY "NEST=" WS-R " ND11=" ND (1, 1)
           STOP RUN.
       LOAD-DATA.
           SET CAP TO 3
           MOVE 10 TO D (1)
           MOVE 20 TO D (2)
           MOVE 30 TO D (3)
           MOVE 1 TO ND (1, 1)
           MOVE 2 TO ND (1, 2)
           MOVE 3 TO ND (2, 1).
       END PROGRAM PB4162DYN.
