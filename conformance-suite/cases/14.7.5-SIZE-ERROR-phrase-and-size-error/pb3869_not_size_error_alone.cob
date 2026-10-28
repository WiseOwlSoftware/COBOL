      *>--- conformance-case
      *> id: pb3869_not_size_error_alone
      *> rules: [GR-14.7.5-L2.3, GR-14.7.5-L3.4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3869 (retired) - an arithmetic statement written with ONLY a
      *> NOT ON SIZE ERROR phrase, checking not enabled.
      *>
      *> ISO 14.7.5, the paragraph on a size error condition with no
      *> SIZE ERROR phrase, ends: "and processing proceeds as specified
      *> in 14.6.13.1.3, Fatal exception conditions. If a NOT SIZE ERROR
      *> phrase is specified, it is ignored." 14.6.13.1.3 item 8 then
      *> leaves it to the implementor, checking not being enabled,
      *> "whether or not execution will continue, how it will continue,
      *> and how any receiving operands are affected". ISO does not
      *> decide the receivers, so CLAUDE.md rule 1 follows GnuCOBOL: any
      *> SIZE ERROR or NOT SIZE ERROR phrase keeps a resultant that
      *> would overflow unchanged (cobc typeck.c,
      *> COB_STORE_KEEP_ON_OVERFLOW), and the CCVS85 programs (NC106A,
      *> NC171A, ...) expect the same. That is CONFORMANCE.md
      *> DOC-A.1-70's determination for this statement: execution
      *> continues, a resultant that meets a size error keeps its value
      *> (the others store as they would have, 14.7.5 rule 2), and the
      *> NOT imperative runs only when no size error condition occurred
      *> (the clause's closing paragraph). The no-phrase rules are 2002
      *> and later (GR-14.7.5-L3.x), so this test starts there.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THAT DETERMINATION, never
      *> read off a run. F is "-" before each statement and "N"
      *> afterwards only if the NOT imperative ran.
      *>
      *> ADD1: 9 + 1 = 10, past PIC 9 (14.7.5 case 3): X keeps 9, F=-.
      *> ADD2: 5 + 1 = 6 fits: X=6 and NOT runs: F=N.
      *> MUL: 42 * 50 = 2100 is past PIC 99: M keeps 42, F=-.
      *> CMP: 40 + 2 = 42 fits, F=N.
      *> CMPR: 99.6 rounded away from zero is 100, past PIC 99: R keeps
      *> 42, F=-.
      *> MULTI: ADD 1 TO A B with A=9, B=1: A's 10 is a size error (A
      *> keeps 9), B stores 2 as it would have without it (14.7.5 rule
      *> 2), and the statement met a size error, so F=-.
      *> SUB: 3 - 5 = -2 into a signed PIC S9 fits: N=-2 and F=N.
      *> REM: 150 / 4 = 37.5, quotient 37 (PIC 99) and remainder 150 -
      *> 37 * 4 = 2 (14.9.12.4 rules 6 and 7): both fit, F=N.
      *> EDIT: 123 is past PIC Z9: E keeps the 5 moved to it, F=-.
      *> CORR: ADD CORRESPONDING is one implied ADD per pair (14.9.2.4
      *> rule 5): P is 9 + 1 = 10, past PIC 9 (P keeps 9), Q is 5 + 1 =
      *> 6 as it would have been alone, and the statement met a size
      *> error: F=-.
      *> SUBS: ADD 1 TO TE(I) with TE(2) = 9 identifies the receiver at
      *> run time and keeps 9 there: TE is 999, F=-.
      *> BOTH: ON and NOT ON SIZE ERROR together: 9 + 1 leaves X=9 and
      *> runs the ON phrase, F=S.
      *> REMOV: 59 / 30 has quotient 1 and remainder 59 - 1 * 30 = 29
      *> (14.9.12.4 rules 6 and 7); 29 is past the PIC 9 remainder, a
      *> size error condition while storing into a resultant (rule 6 c:
      *> "unless storing the value would cause a size error condition"),
      *> so the remainder keeps the 2 the REM row stored, Q=01, and NOT
      *> is skipped, F=-.
      *> RAND: the initial evaluation is done ONCE (14.7.7 rule 4 a), so
      *> the statement consumes exactly one element of the FUNCTION
      *> RANDOM sequence (15.75.3 rule 5: each reference without an
      *> argument returns the next number). Seeded the same, the
      *> NOT-only statement and the phraseless one store the same RR
      *> (PIC 999 holds any RANDOM * 1000) and leave the same next
      *> element: OKAY=Y.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3869NOTONLY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X     PIC 9 VALUE 9.
       01 A     PIC 9 VALUE 9.
       01 B     PIC 9 VALUE 1.
       01 M     PIC 99 VALUE 42.
       01 R     PIC 99 VALUE 42.
       01 N     PIC S9 VALUE 0.
       01 NE    PIC -9.
       01 Q     PIC 99 VALUE 7.
       01 REM   PIC 9 VALUE 0.
       01 E     PIC Z9.
       01 F     PIC X VALUE "-".
       01 G1.
          05 P  PIC 9 VALUE 9.
          05 Q2 PIC 99 VALUE 5.
       01 G2.
          05 P  PIC 9 VALUE 1.
          05 Q2 PIC 99 VALUE 1.
       01 TAB.
          05 TE PIC 9 OCCURS 3 TIMES VALUE 9.
       01 I     PIC 9 VALUE 2.
       01 RR    PIC 999 VALUE 0.
       01 RS    PIC 999 VALUE 0.
       01 RD1   PIC 9V9(8) VALUE 0.
       01 RD2   PIC 9V9(8) VALUE 0.
       01 OKAY  PIC X VALUE "N".
       PROCEDURE DIVISION.
           MOVE "-" TO F.
           ADD 1 TO X NOT ON SIZE ERROR MOVE "N" TO F END-ADD.
           DISPLAY "ADD1 X=" X " F=" F.
           MOVE 5 TO X.
           MOVE "-" TO F.
           ADD 1 TO X NOT ON SIZE ERROR MOVE "N" TO F END-ADD.
           DISPLAY "ADD2 X=" X " F=" F.
           MOVE "-" TO F.
           MULTIPLY 50 BY M NOT ON SIZE ERROR MOVE "N" TO F
           END-MULTIPLY.
           DISPLAY "MUL M=" M " F=" F.
           MOVE "-" TO F.
           COMPUTE R = 40 + 2 NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE.
           DISPLAY "CMP R=" R " F=" F.
           MOVE "-" TO F.
           COMPUTE R ROUNDED = 99.6 NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE.
           DISPLAY "CMPR R=" R " F=" F.
           MOVE "-" TO F.
           ADD 1 TO A B NOT ON SIZE ERROR MOVE "N" TO F END-ADD.
           DISPLAY "MULTI A=" A " B=" B " F=" F.
           MOVE "-" TO F.
           SUBTRACT 5 FROM 3 GIVING N
               NOT ON SIZE ERROR MOVE "N" TO F
           END-SUBTRACT.
           MOVE N TO NE.
           DISPLAY "SUB N=" NE " F=" F.
           MOVE "-" TO F.
           DIVIDE 150 BY 4 GIVING Q REMAINDER REM
               NOT ON SIZE ERROR MOVE "N" TO F
           END-DIVIDE.
           DISPLAY "REM Q=" Q " R=" REM " F=" F.
           MOVE 5 TO E.
           MOVE "-" TO F.
           COMPUTE E = 123 NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE.
           DISPLAY "EDIT E=" E " F=" F.
           MOVE "-" TO F.
           ADD CORRESPONDING G2 TO G1
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD.
           DISPLAY "CORR P=" P OF G1 " Q=" Q2 OF G1 " F=" F.
           MOVE "-" TO F.
           ADD 1 TO TE (I) NOT ON SIZE ERROR MOVE "N" TO F END-ADD.
           DISPLAY "SUBS TE=" TE (1) TE (2) TE (3) " F=" F.
           MOVE 9 TO X.
           MOVE "-" TO F.
           ADD 1 TO X ON SIZE ERROR MOVE "S" TO F
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD.
           DISPLAY "BOTH X=" X " F=" F.
           MOVE "-" TO F.
           DIVIDE 59 BY 30 GIVING Q REMAINDER REM
               NOT ON SIZE ERROR MOVE "N" TO F
           END-DIVIDE.
           DISPLAY "REMOV Q=" Q " R=" REM " F=" F.
           COMPUTE RD1 = FUNCTION RANDOM (7).
           COMPUTE RR = FUNCTION RANDOM * 1000
               NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE.
           COMPUTE RD1 = FUNCTION RANDOM.
           COMPUTE RD2 = FUNCTION RANDOM (7).
           COMPUTE RS = FUNCTION RANDOM * 1000.
           COMPUTE RD2 = FUNCTION RANDOM.
           IF RR = RS AND RD1 = RD2 MOVE "Y" TO OKAY.
           DISPLAY "RAND OKAY=" OKAY.
           STOP RUN.
       END PROGRAM PB3869NOTONLY.
