      *>--- conformance-case
      *> id: pb3990_prohibited_unchecked_keeps_the_resultant
      *> rules: [GR-14.7.4.3-7, GR-14.7.5-L3.4, GR-14.9.12.4-6]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB3990 - ROUNDED MODE IS PROHIBITED in an arithmetic statement
      *> with NO SIZE ERROR phrase and EC-SIZE checking off.
      *>
      *> ISO 14.7.4.3 rule 7: "If the PROHIBITED phrase is specified,
      *> and the arithmetic value cannot be represented exactly in the
      *> resultant identifier, the EC-SIZE-TRUNCATION exception
      *> condition is set to exist, the size error condition exists,
      *> and the content of the resultant identifier is unchanged".
      *> The rule keeps the resultant whether or not checking is on;
      *> with checking off 14.6.13.1.3 item 8 leaves only whether and
      *> how execution continues to the implementor, and CONFORMANCE.md
      *> DOC-A.1-70 continues (GnuCOBOL 3.2's disposition too). A value
      *> that is exact but too big is 14.7.5 case 3 alone and takes the
      *> no-phrase low-order digits (14.7.5 no-phrase rule 4, item 8).
      *> ROUNDED MODE is the 2014 phrase; the version matrix owns the
      *> gate below it.
      *>
      *> EVERY EXPECTED VALUE IS COMPUTED FROM THE RULE. Each receiver
      *> holds 7 before its statement; V = 1.5, V2 = 1.25, F2 = 1.5.
      *> Inexact, so unchanged (7): 1.5 into PIC 99 DISPLAY, COMP,
      *> COMP-5, COMP-3 and Z9; F2 (a COMP-2 sender) into PIC 99;
      *> 10 / 3 as a DIVIDE GIVING and as a COMPUTE quotient; V * V =
      *> 2.25 (MULTIPLY GIVING); 7 + 1.5 = 8.5 (ADD TO); -2.5 into S99
      *> (+07); 150.5 (inexact AND too big); 1.25 into Z9.9; 0.1 into
      *> a COMP-1 (binary32 holds no 0.1); 1.25 into +9.9E+99.
      *> T5 T6: one statement, ROUNDED only on T6: T5 truncates to 01,
      *> T6 keeps 07 - processing goes on to the next receiver.
      *> Exact: 2.0 stores 02; 150 is exact but past PIC 99, low-order
      *> 50. DIVIDE 10 BY 4 GIVING R5 PROHIBITED REMAINDER R6: 2.5 is
      *> inexact, R5 keeps 07; the remainder is item 8's (DOC-A.1-70,
      *> kb/Work PB3975): 10 - 2 * 4 = 2.
      *> GnuCOBOL 3.2 prints the same lines except K5 (it keeps 07 for
      *> the exact 2.0) and FS (it stores 0.1 into the COMP-1): ISO
      *> decides both, as the values above. It has no floating-point
      *> numeric-edited item, so FE is ISO's alone.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3990R7.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 D5 PIC 99 VALUE 7.
       01 B5 PIC 99 COMP VALUE 7.
       01 C5 PIC 99 COMP-5 VALUE 7.
       01 P5 PIC 99 COMP-3 VALUE 7.
       01 E5 PIC Z9 VALUE " 7".
       01 Q5 PIC 99 VALUE 7.
       01 N5 PIC 99 VALUE 7.
       01 M5 PIC 99 VALUE 7.
       01 X5 PIC 99 VALUE 7.
       01 A5 PIC 99 VALUE 7.
       01 S5 PIC S99 VALUE 7.
       01 SE PIC +99.
       01 T5 PIC 99 VALUE 7.
       01 T6 PIC 99 VALUE 7.
       01 K5 PIC 99 VALUE 7.
       01 K6 PIC 99 VALUE 7.
       01 K7 PIC 99 VALUE 7.
       01 R5 PIC 99 VALUE 7.
       01 R6 PIC 9 VALUE 7.
       01 E6 PIC Z9.9 VALUE " 7.0".
       01 FS USAGE COMP-1 VALUE 7.
       01 FSD PIC 9V9.
       01 FE PIC +9.9E+99 VALUE "+7.0E+00".
       01 F2 USAGE COMP-2 VALUE 1.5.
       01 V PIC 9V9 VALUE 1.5.
       01 V2 PIC 9V99 VALUE 1.25.
       PROCEDURE DIVISION.
           COMPUTE D5 ROUNDED MODE IS PROHIBITED = V.
           COMPUTE B5 ROUNDED MODE IS PROHIBITED = V.
           COMPUTE C5 ROUNDED MODE IS PROHIBITED = V.
           COMPUTE P5 ROUNDED MODE IS PROHIBITED = V.
           COMPUTE E5 ROUNDED MODE IS PROHIBITED = V.
           COMPUTE Q5 ROUNDED MODE IS PROHIBITED = F2.
           DIVIDE 3 INTO 10 GIVING N5 ROUNDED MODE IS PROHIBITED.
           COMPUTE M5 ROUNDED MODE IS PROHIBITED = 10 / 3.
           MULTIPLY V BY V GIVING X5 ROUNDED MODE IS PROHIBITED.
           ADD 1.5 TO A5 ROUNDED MODE IS PROHIBITED.
           COMPUTE S5 ROUNDED MODE IS PROHIBITED = -2.5.
           COMPUTE T5 T6 ROUNDED MODE IS PROHIBITED = V.
           COMPUTE K5 ROUNDED MODE IS PROHIBITED = 2.0.
           COMPUTE K6 ROUNDED MODE IS PROHIBITED = 150.
           COMPUTE K7 ROUNDED MODE IS PROHIBITED = 150.5.
           DIVIDE 10 BY 4 GIVING R5 ROUNDED MODE IS PROHIBITED
               REMAINDER R6.
           COMPUTE E6 ROUNDED MODE IS PROHIBITED = V2.
           COMPUTE FS ROUNDED MODE IS PROHIBITED = 0.1.
           COMPUTE FE ROUNDED MODE IS PROHIBITED = 1.25.
           MOVE S5 TO SE.
           MOVE FS TO FSD.
           DISPLAY "D5=" D5 " B5=" B5 " C5=" C5 " P5=" P5 " E5=" E5.
           DISPLAY "Q5=" Q5 " N5=" N5 " M5=" M5 " X5=" X5 " A5=" A5.
           DISPLAY "S5=" SE " T5=" T5 " T6=" T6.
           DISPLAY "K5=" K5 " K6=" K6 " K7=" K7.
           DISPLAY "R5=" R5 " R6=" R6 " E6=" E6.
           DISPLAY "FS=" FSD " FE=" FE.
           STOP RUN.
       END PROGRAM PB3990R7.
