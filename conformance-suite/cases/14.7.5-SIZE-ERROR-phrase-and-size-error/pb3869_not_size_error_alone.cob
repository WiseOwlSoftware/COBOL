      *>--- conformance-case
      *> id: pb3869_not_size_error_alone
      *> rules: [GR-14.7.5-L2.3, GR-14.7.5-L3.4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3869 - an arithmetic statement written with ONLY a NOT ON SIZE
      *> ERROR phrase is the statement with no phrase.
      *>
      *> ISO 14.7.5, the paragraph on a size error condition with no SIZE
      *> ERROR phrase, ends: "and processing proceeds as specified in
      *> 14.6.13.1.3, Fatal exception conditions. If a NOT SIZE ERROR
      *> phrase is specified, it is ignored." The closing paragraph of the
      *> clause transfers to the NOT SIZE ERROR imperative only when "no
      *> size error condition occurs during the execution of the
      *> arithmetic operations ... or while storing into the resultant
      *> identifiers". So the NOT phrase runs exactly when the statement
      *> met no size error condition, and a statement that met one is
      *> disposed of as the phraseless statement is: with checking not
      *> enabled, CONFORMANCE.md DOC-A.1-70 (14.6.13.1.3 item 8) has
      *> execution continue and the resultant receive the LOW-ORDER
      *> digits of the result. Before the fix the phrase turned on the
      *> phrase-checked shape, whose disposition (the receiver left
      *> unchanged, 14.7.5 storing rule 2) belongs to ON SIZE ERROR.
      *> The no-phrase rules are 2002 and later (GR-14.7.5-L3.x), so this
      *> test starts there.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE SPEC, never read off a
      *> run. F is "-" before each statement and "N" afterwards only if
      *> the NOT imperative ran.
      *>
      *> ADD1: 9 + 1 = 10, further from zero than PIC 9 permits (case 3),
      *> so the digit 0 lands and NOT is ignored: X=0 F=-.
      *> ADD2: 5 + 1 = 6 fits: X=6 and NOT runs: F=N.
      *> MUL: 42 * 50 = 2100 into PIC 99 keeps its low-order 00, F=-.
      *> CMP: 40 + 2 = 42 fits, F=N.
      *> CMPR: 99.6 rounded away from zero is 100, past PIC 99: 00, F=-.
      *> MULTI: ADD 1 TO A B with A=9, B=1: A's 10 is a size error (A=0),
      *> B stores 2 as it would have without it (14.7.5 rule 2), and the
      *> statement met a size error, so F=-.
      *> SUB: 3 - 5 = -2 into a signed PIC S9 fits: N=-2 and F=N.
      *> REM: 150 / 4 = 37.5, quotient 37 (PIC 99) and remainder
      *> 150 - 37 * 4 = 2 (14.9.12.4 rules 6 and 7): both fit, F=N.
      *> EDIT: 123 into PIC Z9 keeps its low-order digits 23, F=-.
      *> BOTH: ON and NOT ON SIZE ERROR together are the two-phase
      *> statement: 9 + 1 leaves X=9 and runs the ON phrase, F=S.
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
           MOVE "-" TO F.
           COMPUTE E = 123 NOT ON SIZE ERROR MOVE "N" TO F
           END-COMPUTE.
           DISPLAY "EDIT E=" E " F=" F.
           MOVE 9 TO X.
           MOVE "-" TO F.
           ADD 1 TO X ON SIZE ERROR MOVE "S" TO F
               NOT ON SIZE ERROR MOVE "N" TO F
           END-ADD.
           DISPLAY "BOTH X=" X " F=" F.
           STOP RUN.
       END PROGRAM PB3869NOTONLY.
