      *>--- conformance-case
      *> id: g2-inspect-cd-operands
      *> rules: [2002-FMT-14.8.21.1, 2002-GR-14.8.21.3-3,
      *>    2002-GR-14.8.21.3-4, 2002-GR-14.8.21.3-11,
      *>    2002-GR-14.8.21.3-16]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> INSPECT whose identifier-1 and identifier-3..7 are items of an
      *> output CD's record (§2002:A.1.2.2 SR8, SR9; VALUE clauses there
      *> take effect, §2002:13.16.61.3 GR4); counters are in
      *> working-storage (GR12). The facility draws COBOL0903. The F
      *> lines use working-storage operands and literals. Every
      *> cite.py --check here is OK.
      *> §2002:14.8.21.1 - all four formats and every phrase shape:
      *>   tallying ALL/LEADING lists and CHARACTERS with
      *>   after-before (F1, F5, T1), several counters (F1, F5),
      *>   AFTER and BEFORE both on one operand, in either order
      *>   (F7; the after-before-phrase is a double-bar choice on
      *>   the printed page 462), replacing CHARACTERS/ALL/LEADING/
      *>   FIRST lists (F2, F6, F8 LEADING list of two, F9 FIRST
      *>   list of two, R1, R2), format 3 (F3, T2), format 4 with
      *>   literals (F4) and identifiers (R1). Fails if a granted
      *>   shape is refused.
      *> §2002:14.8.21.3 GR4 - identifier-3/4/5/6/7 stand for
      *>   literal-1/2/3/4/5 (T1, T2, R1, R2 use CD items IA IB IC ID
      *>   IZ ISTAR I6 I7 in every slot).
      *> §2002:14.8.21.3 GR11 - a ALL, b LEADING anchored at the first
      *>   eligible cycle (T1 C4 after IB = 2, C5 LEADING IB = 0, F5
      *>   K4 = 1 not 2), c CHARACTERS (T1 C3, N1 C8).
      *> §2002:14.8.21.3 GR16 - a CHARACTERS (R1 S3), b ALL (R2 S6),
      *>   c LEADING (R1 S5, R2 S8 anchored after IC), d FIRST, the
      *>   leftmost (R1 S2, R2 S7 after IB, F6 each FIRST operand,
      *>   F9 two FIRST operands of the SAME value: '12BAAC').
      *>   c again: F8 LEADING "B" is first eligible at 1, no B
      *>   there, so the B's stay: '11BBC'.
      *> §2002:14.8.21.3 GR3 - a alphabetic, boolean, national (A1);
      *>   b unsigned display numeric, numeric-edited, alphanumeric-
      *>   edited inspected as alphanumeric (N2); c national-edited
      *>   inspected as national (A2: NX 'AB CD' -> 'AB-CD', 1 space
      *>   tallied); d signed numeric inspected as unsigned of the
      *>   length without a separate sign, sign kept (N1: NS embedded
      *>   sign; NL leading separate: 3 characters, no '-' found).
      *>   c/d with usage national numeric items (A3): NN 9(4) and
      *>   N1/N7 9 are class numeric, so SR4 does not apply; NN is
      *>   inspected as national (2 ones, 7207) and signed SN keeps
      *>   its sign (-7207).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS11.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CTRS.
          05 CT PIC 99 OCCURS 16 VALUE 0.
       01 K1 PIC 99 VALUE 0.
       01 K2 PIC 99 VALUE 0.
       01 K3 PIC 99 VALUE 0.
       01 K4 PIC 99 VALUE 0.
       01 T  PIC X(12) VALUE "ABCABCABCXYZ".
       01 U  PIC X(8) VALUE "ABACADAB".
       01 U2 PIC X(8) VALUE "ABACADAB".
       01 W-NS PIC -9(4).
       01 W-NL PIC -9(3).
       01 W-NX PIC X(2).
       01 W-SN PIC X(2).
       01 W-NN PIC 9(4).
       01 K5 PIC 99 VALUE 0.
       01 K6 PIC 99 VALUE 0.
       01 TB PIC X(12) VALUE "ABCABCABCXYZ".
       01 LB PIC X(5) VALUE "AABBC".
       01 UF PIC X(6) VALUE "AABAAC".
       COMMUNICATION SECTION.
       CD CDO FOR OUTPUT.
       01 CR.
          05 S1 PIC X(10) VALUE "AABAACAADA".
          05 S2 PIC X(10) VALUE "AABAACAADA".
          05 S3 PIC X(10) VALUE "AABAACAADA".
          05 S4 PIC X(10) VALUE "AABAACAADA".
          05 S5 PIC X(10) VALUE "AABAACAADA".
          05 S6 PIC X(10) VALUE "AABAACAADA".
          05 S7 PIC X(10) VALUE "AABAACAADA".
          05 S8 PIC X(10) VALUE "AABAACAADA".
          05 S9 PIC X(10) VALUE "AABAACAADA".
          05 IA PIC X VALUE "A".
          05 IB PIC X VALUE "B".
          05 IC PIC X VALUE "C".
          05 ID PIC X VALUE "D".
          05 IZ PIC X VALUE "Z".
          05 ISTAR PIC X VALUE "*".
          05 I6 PIC X(3) VALUE "ABC".
          05 I7 PIC X(3) VALUE "xyz".
          05 NS PIC S9(4) VALUE -1201.
          05 NL PIC S9(3) SIGN LEADING SEPARATE VALUE -102.
          05 NU PIC 9(5) VALUE 10203.
          05 NE PIC ZZ9.99.
          05 AE PIC XXBXX.
          05 AL PIC A(4) VALUE "ABBA".
          05 BO PIC 1(4) VALUE B"1010".
          05 NA PIC N(4) VALUE N"ABBA".
          05 NX PIC NNBNN.
          05 NN PIC 9(4) USAGE NATIONAL VALUE 1201.
          05 SN PIC S9(4) USAGE NATIONAL VALUE -1201.
          05 N1 PIC 9 USAGE NATIONAL VALUE 1.
          05 N7 PIC 9 USAGE NATIONAL VALUE 7.
       PROCEDURE DIVISION.
       MAIN.
           INSPECT S1 TALLYING CT (1) FOR LEADING IA
           INSPECT S1 TALLYING CT (2) FOR ALL IA AFTER INITIAL IB
           INSPECT S1 TALLYING CT (3) FOR CHARACTERS BEFORE INITIAL IC
           INSPECT S1 TALLYING CT (4) FOR LEADING IA AFTER INITIAL IB
           INSPECT S1 TALLYING CT (5) FOR LEADING IB
           INSPECT S1 TALLYING CT (6) FOR ALL IA AFTER INITIAL IZ
           DISPLAY "T1 " CT (1) " " CT (2) " " CT (3) " " CT (4) " "
               CT (5) " " CT (6)
           INSPECT S9 TALLYING CT (7) FOR ALL IB
               REPLACING ALL IB BY ISTAR
           DISPLAY "T2 " CT (7) " " S9
           INSPECT S2 REPLACING FIRST IA BY IZ
           INSPECT S3 REPLACING CHARACTERS BY ISTAR BEFORE INITIAL IC
           INSPECT S4 CONVERTING I6 TO I7 AFTER INITIAL ID
           INSPECT S5 REPLACING LEADING IA BY IZ
           DISPLAY "R1 " S2 " " S3 " " S4 " " S5
           INSPECT S6 REPLACING ALL IA BY IZ
           INSPECT S7 REPLACING FIRST IA BY IZ AFTER INITIAL IB
           INSPECT S8 REPLACING LEADING IA BY IZ AFTER INITIAL IC
           DISPLAY "R2 " S6 " " S7 " " S8
           INSPECT NS TALLYING CT (8) FOR ALL "1"
           INSPECT NS REPLACING ALL "1" BY "7"
           MOVE NS TO W-NS
           INSPECT NL TALLYING CT (9) FOR CHARACTERS
           INSPECT NL TALLYING CT (10) FOR ALL "-"
           INSPECT NL REPLACING ALL "0" BY "9"
           MOVE NL TO W-NL
           DISPLAY "N1 " CT (8) " " W-NS " " CT (9) " " CT (10) " "
               W-NL
           MOVE 12.5 TO NE
           MOVE "ABCD" TO AE
           INSPECT NU REPLACING ALL "0" BY "5"
           INSPECT NE TALLYING CT (11) FOR ALL " "
               REPLACING ALL "." BY ","
           INSPECT AE REPLACING ALL " " BY "-"
           DISPLAY "N2 " NU " " CT (11) " [" NE "] [" AE "]"
           INSPECT AL TALLYING CT (12) FOR ALL "B"
           INSPECT BO TALLYING CT (13) FOR ALL B"1"
           INSPECT BO REPLACING ALL B"0" BY B"1"
           INSPECT NA TALLYING CT (14) FOR ALL N"B"
           DISPLAY "A1 " CT (12) " " CT (13) " " BO " " CT (14)
           MOVE N"ABCD" TO NX
           INSPECT NX TALLYING CT (15) FOR ALL N" "
               REPLACING ALL N" " BY N"-"
           MOVE "NO" TO W-NX
           IF NX = N"AB-CD" MOVE "OK" TO W-NX END-IF
           DISPLAY "A2 " CT (15) " " W-NX
           INSPECT NN TALLYING CT (16) FOR ALL N1
           INSPECT NN REPLACING ALL N1 BY N7
           MOVE NN TO W-NN
           INSPECT SN REPLACING ALL N1 BY N7
           MOVE "NO" TO W-SN
           IF SN = -7207 MOVE "OK" TO W-SN END-IF
           DISPLAY "A3 " CT (16) " " W-NN " " W-SN
           INSPECT T TALLYING K1 FOR ALL "ABC" BEFORE INITIAL "X"
                              K2 FOR CHARACTERS AFTER INITIAL "C"
                                     LEADING "A"
           DISPLAY "F1 " K1 " " K2
           INSPECT T REPLACING CHARACTERS BY "-" AFTER INITIAL "Y"
                               ALL "A" BY "a"
                               LEADING "a" BY "b"
                               FIRST "C" BY "c" BEFORE INITIAL "Z"
           DISPLAY "F2 " T
           MOVE 0 TO K1
           INSPECT T TALLYING K1 FOR ALL "B"
                     REPLACING ALL "B" BY "8"
           DISPLAY "F3 " K1 " " T
           INSPECT T CONVERTING "abc" TO "XYZ" BEFORE INITIAL "-"
           DISPLAY "F4 " T
           INSPECT U TALLYING K3 FOR ALL "B" "C"
                              K4 FOR LEADING "A" "D"
           DISPLAY "F5 " K3 " " K4
           INSPECT U2 REPLACING ALL "A" BY "1" "B" BY "2"
                                FIRST "C" BY "3" "D" BY "4"
           DISPLAY "F6 " U2
           INSPECT TB TALLYING K5 FOR ALL "A" AFTER INITIAL "B"
                                             BEFORE INITIAL "X"
                               K6 FOR CHARACTERS BEFORE INITIAL "B"
                                                 AFTER INITIAL "Y"
           DISPLAY "F7 " K5 " " K6
           INSPECT LB REPLACING LEADING "A" BY "1" "B" BY "2"
           DISPLAY "F8 " LB
           INSPECT UF REPLACING FIRST "A" BY "1" "A" BY "2"
           DISPLAY "F9 " UF
           STOP RUN.
