      *>--- conformance-case
      *> id: pb3975_remainder_after_quotient_size_error_standard_decimal
      *> rules: [GR-14.9.12.4-6, GR-14.9.12.4-7]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3975 - the standard-decimal twin of
      *> pb3975_remainder_after_quotient_size_error: DIVIDE ... GIVING
      *> identifier-3 REMAINDER identifier-4 when the QUOTIENT meets a
      *> size error, under ARITHMETIC IS STANDARD-DECIMAL. ISO 14.9.12.4
      *> rules 1 and 7 make the quotient and the remainder the
      *> expressions (dividend / divisor) and (dividend -
      *> (subsidiary-quotient * divisor)); the mode changes how they are
      *> evaluated, never which receivers a size error affects. With
      *> checking not enabled and no SIZE ERROR phrase, 14.6.13.1.3
      *> item 8 leaves the receivers to the implementor, and
      *> CONFORMANCE.md DOC-A.1-70 follows GnuCOBOL 3.2: the remainder
      *> is stored, from the quotient truncated at identifier-3's
      *> decimal point location with every integer digit kept. A NOT
      *> ON SIZE ERROR phrase alone keeps every receiver, as the ON
      *> phrase does (kb/Work PB3989).
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THAT DETERMINATION:
      *> P2: 1003 / 4 = 250.75, truncated 250: B gets 50, R = 3.
      *> P3: -1003 / 4 = -250.75, truncated -250: SB gets -50, SR = -3.
      *> P4: 1000.5 / 7 = 142.92..., truncated at scale 1 142.9: Q1 gets
      *> 2.9; 1000.5 - 142.9 * 7 = 0.20.
      *> P6: 333 / 1 = 333 into PIC 9: Q6 gets 3; 333 - 333 * 1 = 0.
      *> N1: NOT ON SIZE ERROR alone: C keeps 11, RC keeps 777, F=-.
      *> S1: ON SIZE ERROR: D keeps 11, RX keeps 777, F=S (rule 6 c).
      *> B1: 1003 / 3 truncated 334 into a BINARY-CHAR UNSIGNED: the
      *> container residue 334 - 256 = 78; 1003 - 334 * 3 = 1.
      *>
      *> EDITION. ARITHMETIC IS STANDARD-DECIMAL is the 2014 clause of
      *> the OPTIONS paragraph; the version matrix owns the gate below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3975SDIDI.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B     PIC 99 VALUE 11.
       01 R3    PIC 999 VALUE 777.
       01 SB    PIC S99 VALUE 11.
       01 SR    PIC S999 VALUE 777.
       01 SBE   PIC -99.
       01 SRE   PIC -999.
       01 Q1    PIC 9V9 VALUE 1.1.
       01 R1    PIC 9V99 VALUE 7.77.
       01 Q1E   PIC 9.9.
       01 R1E   PIC 9.99.
       01 Q6    PIC 9 VALUE 7.
       01 R6    PIC 99 VALUE 77.
       01 C     PIC 99 VALUE 11.
       01 RC    PIC 999 VALUE 777.
       01 D     PIC 99 VALUE 11.
       01 RX    PIC 999 VALUE 777.
       01 QB    USAGE BINARY-CHAR UNSIGNED VALUE 7.
       01 RB    PIC 999 VALUE 777.
       01 QBD   PIC 999.
       01 F     PIC X VALUE "-".
       PROCEDURE DIVISION.
           DIVIDE 1003 BY 4 GIVING B REMAINDER R3.
           DISPLAY "P2 B=" B " R=" R3.
           DIVIDE -1003 BY 4 GIVING SB REMAINDER SR.
           MOVE SB TO SBE.
           MOVE SR TO SRE.
           DISPLAY "P3 SB=" SBE " SR=" SRE.
           DIVIDE 7 INTO 1000.5 GIVING Q1 REMAINDER R1.
           MOVE Q1 TO Q1E.
           MOVE R1 TO R1E.
           DISPLAY "P4 Q1=" Q1E " R1=" R1E.
           DIVIDE 333 BY 1 GIVING Q6 REMAINDER R6.
           DISPLAY "P6 Q6=" Q6 " R6=" R6.
           MOVE "-" TO F.
           DIVIDE 1003 BY 4 GIVING C REMAINDER RC
               NOT ON SIZE ERROR MOVE "N" TO F
           END-DIVIDE.
           DISPLAY "N1 C=" C " R=" RC " F=" F.
           MOVE "-" TO F.
           DIVIDE 1003 BY 4 GIVING D REMAINDER RX
               ON SIZE ERROR MOVE "S" TO F
           END-DIVIDE.
           DISPLAY "S1 D=" D " R=" RX " F=" F.
           DIVIDE 1003 BY 3 GIVING QB REMAINDER RB.
           MOVE QB TO QBD.
           DISPLAY "B1 Q=" QBD " R=" RB.
           STOP RUN.
       END PROGRAM PB3975SDIDI.
