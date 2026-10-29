      *>--- conformance-case
      *> id: pb3975_remainder_after_quotient_size_error
      *> rules: [GR-14.9.12.4-6, GR-14.7.5-L3.4]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3975 - DIVIDE ... GIVING identifier-3 REMAINDER identifier-4
      *> when the QUOTIENT meets a size error, native arithmetic.
      *>
      *> ISO 14.9.12.4 rule 6 c) stores the remainder "If the size
      *> error condition is not raised". With an ON SIZE ERROR phrase
      *> the condition is the phrase's (14.7.5) and identifier-4 keeps
      *> its value: ISO decides, row S1. With checking not enabled and
      *> no SIZE ERROR phrase, 14.7.5 sends the condition to
      *> 14.6.13.1.3, whose item 8 leaves "how any receiving operands
      *> are affected" to the implementor. So CLAUDE.md rule 1 follows
      *> GnuCOBOL 3.2, CONFORMANCE.md DOC-A.1-70: the remainder is
      *> STORED, formed (rule 7) from the quotient truncated at
      *> identifier-3's decimal point location with every integer
      *> digit kept (libcob cob_div_quotient); the quotient itself
      *> stores its low-order digits. A statement with ONLY a NOT ON
      *> SIZE ERROR phrase keeps every receiver of the size error, as
      *> the ON phrase does (kb/Work PB3989, DOC-A.1-70), so its
      *> remainder is the phrase's too: unchanged, row N1.
      *> The no-phrase rules are 2002 and later (GR-14.7.5-L3.x).
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THAT DETERMINATION, never
      *> read off a run. F is "-" before each statement and "N" or "S"
      *> afterwards only if that phrase ran.
      *>
      *> P1: 1000 / 4 = 250, past PIC 99: A gets 50; 1000 - 250 * 4 = 0.
      *> P2: 1003 / 4 = 250.75, truncated 250: B gets 50; the remainder
      *> is 1003 - 250 * 4 = 3, never 1003 - 50 * 4 = 803.
      *> P3: -1003 / 4 = -250.75, truncated toward zero -250: SB gets
      *> -50; -1003 - (-250 * 4) = -3.
      *> P4: 1000.5 / 7 = 142.92..., truncated at scale 1 142.9: Q1 gets
      *> 2.9; 1000.5 - 142.9 * 7 = 1000.5 - 1000.3 = 0.20.
      *> P5: a FLOAT-LONG dividend 1003 (exact in binary64), the same
      *> division as P2: QF gets 50, RQ = 3.
      *> P6: 333 / 1 = 333 into PIC 9: Q6 gets 3; 333 - 333 * 1 = 0.
      *> N1: NOT ON SIZE ERROR alone, 1003 / 4: C keeps 11, RC keeps
      *> 777, and the NOT imperative is skipped, F=-.
      *> S1: ON SIZE ERROR, 1003 / 4: D keeps 11, RX keeps 777, F=S.
      *> (GnuCOBOL 3.2 prints R=003 on both N1 and S1, against rule
      *> 6 c) on S1: ISO decides S1, and N1 takes S1's disposition.)
      *> B1: 1003 / 3 = 334.33..., truncated 334, into a BINARY-CHAR
      *> UNSIGNED (container 0 to 255) with no phrase: 334 - 256 = 78;
      *> 1003 - 334 * 3 = 1.
      *> B2: the same with NOT ON SIZE ERROR alone: QN keeps 7 and RN
      *> keeps 777 (every usage alike, kb/Work PB3989), F=-.
      *>
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3975NATIVE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A     PIC 99 VALUE 11.
       01 R     PIC 9 VALUE 7.
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
       01 FL    USAGE FLOAT-LONG.
       01 QF    PIC 99 VALUE 11.
       01 RQ    PIC 999 VALUE 777.
       01 Q6    PIC 9 VALUE 7.
       01 R6    PIC 99 VALUE 77.
       01 C     PIC 99 VALUE 11.
       01 RC    PIC 999 VALUE 777.
       01 D     PIC 99 VALUE 11.
       01 RX    PIC 999 VALUE 777.
       01 QB    USAGE BINARY-CHAR UNSIGNED VALUE 7.
       01 RB    PIC 999 VALUE 777.
       01 QN    USAGE BINARY-CHAR UNSIGNED VALUE 7.
       01 RN    PIC 999 VALUE 777.
       01 QBD   PIC 999.
       01 F     PIC X VALUE "-".
       PROCEDURE DIVISION.
           DIVIDE 1000 BY 4 GIVING A REMAINDER R.
           DISPLAY "P1 A=" A " R=" R.
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
           MOVE 1003 TO FL.
           DIVIDE FL BY 4 GIVING QF REMAINDER RQ.
           DISPLAY "P5 QF=" QF " R=" RQ.
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
           MOVE "-" TO F.
           DIVIDE 1003 BY 3 GIVING QN REMAINDER RN
               NOT ON SIZE ERROR MOVE "N" TO F
           END-DIVIDE.
           MOVE QN TO QBD.
           DISPLAY "B2 Q=" QBD " R=" RN " F=" F.
           STOP RUN.
       END PROGRAM PB3975NATIVE.
