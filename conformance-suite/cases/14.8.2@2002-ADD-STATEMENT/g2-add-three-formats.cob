      *>--- conformance-case
      *> id: g2-add-three-formats
      *> rules: [2002-FMT-14.8.2.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.2.1 - the three general formats of ADD, PDF page
      *> 437 rendered: format 1 ADD {id|lit}... TO {id [ROUNDED]}...;
      *> format 2 ADD {id|lit}... TO {id|lit} GIVING {id [ROUNDED]}...
      *> where TO is NOT underlined (optional); format 3 ADD
      *> {CORRESPONDING|CORR} id TO id [ROUNDED]; each with the ON SIZE
      *> ERROR / NOT ON SIZE ERROR pair in choice indicators (both, in
      *> either order) and an optional END-ADD.
      *> Values: §2002:14.8.2.3 GR1, GR2, GR3 and GR5; ROUNDED and
      *> truncation per §2002:14.6.3; size error per §2002:14.6.4.
      *> F1    A(1.5)+1 = 2.5 added to C=0 ROUNDED -> 03, D=0 -> 02.
      *> F1-SE S(9)+1 = 10 > PIC 9: size error, S unchanged -> 9;
      *>       NOT ON written before ON.
      *> F2    1.5+2+3 = 6.5 GIVING C ROUNDED -> 07, D -> 06
      *>       (literal-2 after TO).
      *> F2-TO A+B = 3.5 GIVING C -> 03 (TO written, no phrase).
      *> F2-NO A=1: ADD A B GIVING C, TO omitted, B is id-2 -> 03.
      *> F3    CORRESPONDING G1 TO G2 ROUNDED: F1 10+1=11, F2
      *>       10+1.5=11.5 rounded -> 12; G2 shows 1112.
      *> F3B   CORR G1 TO G2: F1 11+1=12, F2 12+1.5=13.5 truncated
      *>       -> 13; G2 shows 1213.
      *> Each leg fails if the form is refused, or a value is wrong.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ADD03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9V9 VALUE 1.5.
       01 B PIC 99 VALUE 2.
       01 C PIC 99 VALUE 0.
       01 D PIC 99 VALUE 0.
       01 S PIC 9 VALUE 9.
       01 G1.
          05 F1 PIC 99 VALUE 1.
          05 F2 PIC 9V9 VALUE 1.5.
       01 G2.
          05 F1 PIC 99 VALUE 10.
          05 F2 PIC 99 VALUE 10.
       PROCEDURE DIVISION.
       MAIN.
           ADD A 1 TO C ROUNDED D
               ON SIZE ERROR DISPLAY "F1 SE"
               NOT ON SIZE ERROR DISPLAY "F1 NSE " C " " D
           END-ADD
           ADD 1 TO S
               NOT ON SIZE ERROR DISPLAY "F1-SE NSE"
               ON SIZE ERROR DISPLAY "F1-SE SE " S
           END-ADD
           ADD A B TO 3 GIVING C ROUNDED D
               ON SIZE ERROR DISPLAY "F2 SE"
               NOT ON SIZE ERROR DISPLAY "F2 NSE " C " " D
           END-ADD
           ADD A TO B GIVING C
           DISPLAY "F2-TO " C
           MOVE 0 TO C
           MOVE 1 TO A
           ADD A B GIVING C
           DISPLAY "F2-NO " C
           MOVE 1.5 TO A
           ADD CORRESPONDING G1 TO G2 ROUNDED
               ON SIZE ERROR DISPLAY "F3 SE"
               NOT ON SIZE ERROR DISPLAY "F3 NSE " G2
           END-ADD
           ADD CORR G1 TO G2
           DISPLAY "F3B " G2
           STOP RUN.
