      *>--- conformance-case
      *> id: g85_bf17_01
      *> rules: [1985-SR-VI.6.21.3-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.21.3 SR10 - condition-1 and condition-2 of a
      *> PERFORM may be any conditional expression (VI-54).
      * SR10 OPENS the UNTIL slot to every 1985 conditional
      * expression, so each leg puts a different form of section
      * VI.6.3 there and prints a count derived from that form's
      * own rules. A slot narrowed to simple relations fails to
      * translate; a slot that evaluates a form wrongly prints a
      * different digit. The last leg is condition-2 of format 4;
      * it is out-of-line because SR2 forbids AFTER in-line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1701.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A       PIC 9     VALUE 0.
       01 B       PIC 9     VALUE 0.
       01 A2      PIC 9     VALUE 0.
       01 A3      PIC 9     VALUE 0.
       01 ST      PIC 9     VALUE 0.
          88 ST-DONE        VALUE 1.
       01 C-CN    PIC 9     VALUE 0.
       01 SNEG    PIC S9(3) VALUE -3.
       01 SPOS    PIC S9(3) VALUE -3.
       01 C-SGN1  PIC 9     VALUE 0.
       01 C-SGN2  PIC 9     VALUE 0.
       01 CX      PIC X(3)  VALUE "123".
       01 C-CLS   PIC 9     VALUE 0.
       01 EA      PIC 9     VALUE 0.
       01 EB      PIC 9     VALUE 5.
       01 NA      PIC 9     VALUE 0.
       01 VI      PIC 9     VALUE 0.
       01 VJ      PIC 9     VALUE 0.
       01 C-AF    PIC 9     VALUE 0.
       PROCEDURE DIVISION.
       MAIN-P.
      * (a) complex condition (VI.6.3.2) with parentheses: B is 0,
      * so the condition first holds when A reaches 3.
           PERFORM UNTIL (A > 2 AND B = 0) OR A > 5
               ADD 1 TO A
           END-PERFORM.
           DISPLAY "SR10-COMPLEX=" A.
      * (b) abbreviated combined relation (VI.6.3.3): A2 = 3 OR 2
      * OR 1 is (A2 = 3) OR (A2 = 2) OR (A2 = 1); true at 1 only
      * through the inserted subject and operator (a dropped
      * abbreviation runs to 3).
           PERFORM UNTIL A2 = 3 OR 2 OR 1
               ADD 1 TO A2
           END-PERFORM.
           DISPLAY "SR10-ABBREV=" A2.
      * (c) VI.6.3.3 table: a > b AND NOT < c expands to
      * (a > b) AND (a NOT < c); so (A3 NOT < 2) AND (A3 NOT > 9)
      * is false at 0 and 1 and first true at 2.
           PERFORM UNTIL A3 NOT < 2 AND NOT > 9
               ADD 1 TO A3
           END-PERFORM.
           DISPLAY "SR10-ABBNOT=" A3.
      * (d) condition-name condition: true once ST holds 1.
           PERFORM UNTIL ST-DONE
               ADD 1 TO C-CN
               ADD 1 TO ST
           END-PERFORM.
           DISPLAY "SR10-CONDNM=" C-CN.
      * (e) sign condition true at entry: TEST BEFORE is implied
      * (SR3), so GR10c passes control to the end: zero passes.
           PERFORM UNTIL SNEG IS NEGATIVE
               ADD 1 TO C-SGN1
           END-PERFORM.
           DISPLAY "SR10-SIGN-T=" C-SGN1.
      * (f) NOT NEGATIVE: -3, -2, -1 are negative, 0 is not; three
      * passes.
           PERFORM UNTIL SPOS IS NOT NEGATIVE
               ADD 1 TO C-SGN2
               ADD 1 TO SPOS
           END-PERFORM.
           DISPLAY "SR10-SIGN-F=" C-SGN2.
      * (g) negated class condition: "123" is NUMERIC, "12A" is
      * not, so exactly one pass.
           PERFORM UNTIL NOT (CX IS NUMERIC)
               ADD 1 TO C-CLS
               MOVE "12A" TO CX
           END-PERFORM.
           DISPLAY "SR10-CLASS =" C-CLS.
      * (h) relation of two arithmetic expressions: EA + 1 > 4 is
      * first true at EA = 4.
           PERFORM UNTIL EA + 1 > EB - 1
               ADD 1 TO EA
           END-PERFORM.
           DISPLAY "SR10-ARITH =" EA.
      * (i) nested parentheses: first true at NA = 2.
           PERFORM UNTIL ((NA > 1) AND ((NA = 2) OR (NA = 4)))
               ADD 1 TO NA
           END-PERFORM.
           DISPLAY "SR10-NESTED=" NA.
      * (j) condition-2: VJ = 3 OR 2 admits VJ = 1 only, true at 2
      * through the abbreviated disjunct (a dropped abbreviation
      * gives 4); VI admits 1 and 2 (GR10d), so the set is executed
      * twice.
           PERFORM COUNT-P VARYING VI FROM 1 BY 1 UNTIL VI > 2
               AFTER VJ FROM 1 BY 1 UNTIL VJ = 3 OR 2.
           DISPLAY "SR10-COND-2=" C-AF.
           STOP RUN.
       COUNT-P.
           ADD 1 TO C-AF.
