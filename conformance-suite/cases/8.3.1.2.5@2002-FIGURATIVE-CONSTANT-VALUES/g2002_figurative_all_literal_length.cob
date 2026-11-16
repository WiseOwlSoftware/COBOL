      *>--- conformance-case
      *> id: g2002_figurative_all_literal_length
      *> rules: [2002-SR-8.3.1.2.5.2-2, 2002-GR-8.3.1.2.5.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.3.1.2.5.2 SR2 - "Literal-1 shall be an alphanumeric,
      *> boolean, or national literal, any of which may be a
      *> concatenation
      *> expression": ALL "AB" (alphanumeric), ALL N"XY" (national), ALL
      *> B"10"
      *> (boolean), ALL "A" & "BC" and ALL "A" & QUOTE (concatenation
      *> expressions; §2002:8.8.3.2 SR1 admits a figurative operand
      *> without
      *> ALL) are all accepted. The numeric and figurative literal-1
      *> twins in
      *> this folder are refused.
      *> §2002:8.3.1.2.5.3 GR2, applied in order:
      *>  a) in a concatenation the figurative constant is one
      *>     character:
      *>     "X" & SPACE & "Y" is X Y (line 1) and "A" & QUOTE is A" ,
      *>     so
      *>     ALL of it repeated into X(6) is A"A"A" (line 2);
      *>  b) in a VALUE clause or associated with a data item the string
      *>     is
      *>     repeated and truncated to the item's size: V5, Y5 and the
      *>     JUSTIFIED J5 all ABABA (the size is reached before
      *>     JUSTIFIED
      *>     applies), Z2 AB, Q1 X (the minimum of 1), the comparison
      *>     equal
      *>     (lines 3-6); N4 is XYXY and BB 10101 (line A); S8 is
      *>     ABCABCAB
      *>     (line B);
      *>  c) other than ALL literal-1 it is one character: SPACE, ALL
      *>     SPACES,
      *>     QUOTE, ZERO in DISPLAY and STRING are each one character
      *>     (lines
      *>     7-8: S8 is AB"0 C then two spaces of the initial MOVE);
      *>  d) otherwise its length is that of literal-1: DISPLAY ALL "AB"
      *>     is
      *>     AB (line 9).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FIG04.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
           01 C3 PIC X(3) VALUE "X" & SPACE & "Y".
           01 V5 PIC X(5) VALUE ALL "AB".
           01 Y5 PIC X(5).
           01 J5 PIC X(5) JUSTIFIED RIGHT.
           01 Z2 PIC X(2).
           01 Q1 PIC X(1).
           01 S8 PIC X(8).
           01 X6 PIC X(6).
           01 N4 PIC N(4).
           01 BB PIC 1(5).
       PROCEDURE DIVISION.
           DISPLAY "1[" C3 "]"
           MOVE ALL "A" & QUOTE TO X6
           DISPLAY "2[" X6 "]"
           DISPLAY "3[" V5 "]"
           MOVE ALL "AB" TO Y5
           MOVE ALL "AB" TO J5
           DISPLAY "4[" Y5 "][" J5 "]"
           MOVE ALL "ABC" TO Z2
           MOVE ALL "XYZ" TO Q1
           DISPLAY "5[" Z2 "][" Q1 "]"
           IF V5 = ALL "AB"
               DISPLAY "6[CMP-EQ]"
           ELSE
               DISPLAY "6[CMP-NE]"
           END-IF
           DISPLAY "7[" SPACE "][" ALL SPACES "][" QUOTE "][" ZERO "]"
           MOVE SPACES TO S8
           STRING "AB" QUOTE ZERO SPACE "C" DELIMITED BY SIZE
               INTO S8
           END-STRING
           DISPLAY "8[" S8 "]"
           DISPLAY "9[" ALL "AB" "]"
           MOVE ALL N"XY" TO N4
           MOVE ALL B"10" TO BB
           DISPLAY "A[" N4 "][" BB "]"
           MOVE ALL "A" & "BC" TO S8
           DISPLAY "B[" S8 "]"
           STOP RUN.
