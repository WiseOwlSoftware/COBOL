      *>--- conformance-case
      *> id: g2prf21_varying_sequence
      *> rules: [2002-GR-14.8.27.3-9]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.27.3 GR9 - the VARYING terms and "The sequence of
      *>   operation of the PERFORM statement". Each line is
      *>   <leg> [<values the set saw>] C=<executions> and the final
      *>   induction variable(s). Every BY below that is written as
      *>   omitted takes "For any BY phrase that is omitted, the
      *>   augment value is 1".
      *> DERIVATION of every .out line:
      *>   D1 [12] C=2 J=3   TEST BEFORE, no AFTER (d): J from 1 until
      *>                     J > 2: set at 1, 2; J=3 when condition-1
      *>                     is found true.
      *>   D0 [] C=0 J=5     (a) J is set to 5 BEFORE anything; (d)
      *>                     5 > 2 is true at once: no execution.
      *>   B1 [123] C=3 J=3  TEST AFTER, no AFTER (b): run, test 1>2
      *>                     F, J=2, run, F, J=3, run, test T; J holds
      *>                     its value at the end of the last run.
      *>   B0 [5] C=1 J=5    (b): the set runs once before the first
      *>                     test, even though 5 > 2.
      *>   E [111213212223] A=3 B=1  TEST BEFORE with AFTER (e): B
      *>                     cycles 1..3 inside each A; when B > 3, B
      *>                     is reset to 1 and A augmented; A=3 ends
      *>                     it, leaving A=3, B=1.
      *>   CA [111213212223] A=2 B=3 TEST AFTER with AFTER (c): run,
      *>                     test B >= 3, augment B, ... ; when B >= 3
      *>                     and A >= 2 are both true the statement
      *>                     ends with the values of the last run (c
      *>                     NOTE). c 3 and 4 say "execution proceeds
      *>                     with step a"; read as c step 1 (run the
      *>                     set) - reading it as GR9 a (reset every
      *>                     induction variable) would never end, which
      *>                     c 4's "control is transferred to the end"
      *>                     and the NOTE exclude.
      *>   N [531] C=3 S=-1  BY -2: "the induction variable is actually
      *>                     decremented by the absolute value" (NOTE);
      *>                     S=-1 when S < 1 is found true (d).
      *>   I [147] C=3 I=10  the set adds 2 to I: "all changes to the
      *>                     induction variable ... have immediate
      *>                     effect": 1(+2)+1=4, 4(+2)+1=7, 7(+2)+1=10.
      *>   G [0103071531] G=31  BY STP, the set doubles STP: changes to
      *>                     the augment variable have immediate
      *>                     effect: 1,+2=3,+4=7,+8=15,+16=31 > 20.
      *>   X [0106] X=11     BY BV(N), the set moves 2 to N: identifier-4
      *>                     is identified "each time ... used in a
      *>                     setting or augmenting operation": 1, then
      *>                     +BV(2)=5 -> 6, then +5 -> 11 > 10. Identified
      *>                     once (BV(1)=1) it would give 01..10.
      *> Each leg fails on the wrong test position (D0/B0), the wrong
      *>   final value (any J/A/B/S/I/G/X), a nested-variable order
      *>   error (E/CA), or a cached augment/subscript (G/X).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRF21.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T   PIC X(20).
       01 P   PIC 99.
       01 C   PIC 9.
       01 J   PIC 9.
       01 A   PIC 9.
       01 B   PIC 9.
       01 S   PIC S9.
       01 SE  PIC -9.
       01 I   PIC 99.
       01 G   PIC 99.
       01 STP PIC 99.
       01 X   PIC 99.
       01 N   PIC 9.
       01 BVT VALUE "0105".
          05 BV PIC 99 OCCURS 2.
       PROCEDURE DIVISION.
       MAIN SECTION.
       MAIN-P.
           PERFORM RST
           PERFORM PJ VARYING J FROM 1 UNTIL J > 2
           DISPLAY "D1 [" T(1:P - 1) "] C=" C " J=" J
           PERFORM RST
           PERFORM PJ VARYING J FROM 5 UNTIL J > 2
           DISPLAY "D0 [] C=" C " J=" J
           PERFORM RST
           PERFORM PJ WITH TEST AFTER VARYING J FROM 1 UNTIL J > 2
           DISPLAY "B1 [" T(1:P - 1) "] C=" C " J=" J
           PERFORM RST
           PERFORM PJ WITH TEST AFTER VARYING J FROM 5 UNTIL J > 2
           DISPLAY "B0 [" T(1:P - 1) "] C=" C " J=" J
           PERFORM RST
           PERFORM PAB VARYING A FROM 1 UNTIL A > 2
                         AFTER B FROM 1 UNTIL B > 3
           DISPLAY "E [" T(1:P - 1) "] A=" A " B=" B
           PERFORM RST
           PERFORM PAB WITH TEST AFTER
                   VARYING A FROM 1 UNTIL A >= 2
                     AFTER B FROM 1 UNTIL B >= 3
           DISPLAY "CA [" T(1:P - 1) "] A=" A " B=" B
           PERFORM RST
           PERFORM VARYING S FROM 5 BY -2 UNTIL S < 1
               MOVE S TO J
               STRING J DELIMITED BY SIZE INTO T WITH POINTER P
               ADD 1 TO C
           END-PERFORM
           MOVE S TO SE
           DISPLAY "N [" T(1:P - 1) "] C=" C " S=" SE
           PERFORM RST
           PERFORM VARYING I FROM 1 BY 1 UNTIL I > 9
               MOVE I TO J
               STRING J DELIMITED BY SIZE INTO T WITH POINTER P
               ADD 1 TO C
               ADD 2 TO I
           END-PERFORM
           DISPLAY "I [" T(1:P - 1) "] C=" C " I=" I
           PERFORM RST
           MOVE 1 TO STP
           PERFORM VARYING G FROM 1 BY STP UNTIL G > 20
               STRING G DELIMITED BY SIZE INTO T WITH POINTER P
               MULTIPLY 2 BY STP
           END-PERFORM
           STRING G DELIMITED BY SIZE INTO T WITH POINTER P
           DISPLAY "G [" T(1:P - 1) "] G=" G
           PERFORM RST
           MOVE 1 TO N
           PERFORM VARYING X FROM 1 BY BV(N) UNTIL X > 10
               STRING X DELIMITED BY SIZE INTO T WITH POINTER P
               MOVE 2 TO N
           END-PERFORM
           DISPLAY "X [" T(1:P - 1) "] X=" X
           STOP RUN.
       RST.
           MOVE SPACES TO T
           MOVE 1 TO P
           MOVE 0 TO C.
       PJ.
           STRING J DELIMITED BY SIZE INTO T WITH POINTER P
           ADD 1 TO C.
       PAB.
           STRING A B DELIMITED BY SIZE INTO T WITH POINTER P.
