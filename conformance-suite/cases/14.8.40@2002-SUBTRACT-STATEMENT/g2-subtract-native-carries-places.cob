      *>--- conformance-case
      *> id: g2-subtract-native-carries-places
      *> rules: [2002-GR-14.8.40.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.40.3 GR4 - native arithmetic carries enough places
      *> "When native arithmetic is in effect and none of the operands
      *> is described with usage binary-char, ... enough places shall be
      *> carried so as not to lose significant digits during execution."
      *>   cite.py: OK  §2002:14.8.40.3 GR4
      *> Native arithmetic is in effect: there is no OPTIONS paragraph,
      *> so "no ARITHMETIC clause is specified" (cite.py: OK
      *> §2002:8.8.1.2). Every operand is usage display, none of the
      *> seven usages GR4 excludes. All three formats are exercised:
      *> GR1 "The initial evaluation is subtracted from the value of the
      *> data item referenced by identifier-2" (OK §2002:14.8.40.3 GR1),
      *> GR2 "subtracting this value from literal-2 or the value of the
      *> data item referenced by identifier-2" (OK §2002:14.8.40.3 GR2),
      *> GR3 "data items in identifier-4 are subtracted from and stored
      *> in corresponding items in identifier-5" (OK §2002:14.8.40.3
      *> GR3). ROUNDED (§2002:14.6.3, cite.py OK): "the absolute value
      *> of the resultant identifier is increased by one in the
      *> low-order position whenever the most significant digit of the
      *> excess is greater than or equal to five"; without it
      *> "truncation is relative to the size provided for the resultant
      *> identifier".
      *> DERIVATION of every .out line:
      *>  F1  9 + 9 = 18 needs two integer places though each operand
      *>      has one; 20 - 18 = 2 -> C PIC 99      -> "F1 02"
      *>      (a sum kept in one place, 8, gives 12)
      *>  F1R 0.4 + 0.4 = 0.8; 20 - 0.8 = 19.2; ROUNDED into PIC 99,
      *>      excess digit 2 < 5 -> 19              -> "F1R 19"
      *>      (operands aligned to the receiver's 0 places give 20)
      *>  F2  20 - (9 + 9) = 2 GIVING G PIC 99      -> "F2 02"
      *>  F2E 99 - (0.4 + 0.4) = 98.2 GIVING the numeric-edited
      *>      PIC 99.9 (SR4 permits it)             -> "F2E 98.2"
      *>  F3  CORR: X 20 - 0.6 = 19.4 ROUNDED -> 19; Y 20 - 9 = 11
      *>                                            -> "F3 19 11"
      *>      (a fraction dropped before subtracting gives X 20)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SUB01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A   PIC 9   VALUE 9.
       01 B   PIC 9   VALUE 9.
       01 C   PIC 99  VALUE 20.
       01 C2  PIC 99  VALUE 20.
       01 D   PIC 9V9 VALUE 0.4.
       01 E   PIC 9V9 VALUE 0.4.
       01 G   PIC 99.
       01 GE  PIC 99.9.
       01 G1.
          02 X PIC 9V9 VALUE 0.6.
          02 Y PIC 9   VALUE 9.
       01 G2.
          02 X PIC 99  VALUE 20.
          02 Y PIC 99  VALUE 20.
       PROCEDURE DIVISION.
       P1.
           SUBTRACT A B FROM C.
           DISPLAY "F1 " C.
           SUBTRACT D E FROM C2 ROUNDED.
           DISPLAY "F1R " C2.
           SUBTRACT A B FROM 20 GIVING G.
           DISPLAY "F2 " G.
           SUBTRACT D E FROM 99 GIVING GE.
           DISPLAY "F2E " GE.
           SUBTRACT CORRESPONDING G1 FROM G2 ROUNDED.
           DISPLAY "F3 " X OF G2 " " Y OF G2.
           STOP RUN.
