      *>--- conformance-case
      *> id: pb4115_inspect_function_evaluated_once
      *> rules: [GR-14.9.22.4-6, GR-8.4.3.2.4-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4115. An INSPECT TALLYING whose function-identifier
      *> operand takes an argument a counter of the same statement
      *> occupies. The function's value is a temporary data item fixed
      *> when the function is referenced, and INSPECT references each
      *> identifier once:
      *>   cite.py --check "§2023:8.4.3.2.4 GR1" "A function-identifier
      *>   references a temporary data item whose value is determined
      *>   when the function is referenced at runtime" -> OK 1)
      *>   cite.py --check "§2023:14.9.22.4 GR6" "Item identification
      *>   for any identifier is done only once as the first operation
      *>   in the execution of the INSPECT statement" -> OK 6)
      *> No counter can occupy that temporary item, so the overlap of
      *> GR13 (docs/CONFORMANCE.md D-INS1, which re-reads a DATA ITEM a
      *> counter may overlap) never reaches a function: a later operand
      *> sees the value the function had when the statement began.
      *> GnuCOBOL 3.2 (measured) agrees on F1 and D2 but evaluates an
      *> operand's function when its phrase runs, after the counters
      *> before it stored (F2 2X, F3 3B). The standard controls here:
      *> GR13's overlap needs the operand to occupy the counter's
      *> storage, and a temporary item never does.
      *> EXPECTED:
      *>   F1  identifier-1 FUNCTION UPPER-CASE(G), T inside G, G =
      *>       "0A1A1A": T FOR ALL "A" counts 3 -> T=3; T FOR ALL "3"
      *>       scans the same image "0A1A1A", no "3" -> T=3 [3A1A1A]
      *>       (re-evaluating the function after the store counts
      *>       the "3" of "3A1A1A": 4A1A1A)
      *>   F2  pattern FUNCTION UPPER-CASE(Q), QT inside Q, Q = "0X",
      *>       S2 = "B1X": QT FOR ALL "B" -> QT=1; the pattern is
      *>       "0X", absent -> QT=1                             [1X]
      *>       (re-evaluated it is "1X", present: 2X)
      *>   F3  delimiter: R = "0B", RT inside R, S = "1B2B3B": RT FOR
      *>       ALL "B" -> RT=3; RT FOR ALL "3" BEFORE INITIAL
      *>       FUNCTION UPPER-CASE(R): "0B" is absent, so the region
      *>       is the whole image and holds one "3" -> RT=4    [4B]
      *>       (re-evaluated it is "3B", the region "1B2B": 3B)
      *>   D2  control, the D-INS1 data-item arm still re-reads:
      *>       INSPECT G with T inside G, G = "0A1A1A": T=3, then the
      *>       "3" in T itself is counted -> T=4            [4A1A1A]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4115-INSPECT-FN-ONCE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
          05 T  PIC 9 VALUE 0.
          05 GA PIC X(5) VALUE "A1A1A".
       01 Q.
          05 QT PIC 9 VALUE 0.
          05 QP PIC X VALUE "X".
       01 R.
          05 RT PIC 9 VALUE 0.
          05 RP PIC X VALUE "B".
       01 S  PIC X(6) VALUE "1B2B3B".
       01 S2 PIC X(3) VALUE "B1X".
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT FUNCTION UPPER-CASE(G) TALLYING T FOR ALL "A"
               T FOR ALL "3"
           DISPLAY "F1 " G
           INSPECT S2 TALLYING QT FOR ALL "B"
               QT FOR ALL FUNCTION UPPER-CASE(Q)
           DISPLAY "F2 " Q
           INSPECT S TALLYING RT FOR ALL "B"
               RT FOR ALL "3" BEFORE INITIAL FUNCTION UPPER-CASE(R)
           DISPLAY "F3 " R
           MOVE "0A1A1A" TO G
           INSPECT G TALLYING T FOR ALL "A" T FOR ALL "3"
           DISPLAY "D2 " G
           STOP RUN.
