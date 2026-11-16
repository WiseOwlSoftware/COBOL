      *>--- conformance-case
      *> id: g2-stdcmp-argument-classes
      *> rules: [2002-AR-15.69.2-1, 2002-AR-15.69.2-2,
      *>    2002-RV-15.69.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:15.69.2 AR1/AR2 - "Argument-1 [Argument-2] shall be of
      *>   class alphabetic, alphanumeric, or national."
      *> §2002:15.69.3 RV3 - "If the arguments are of different
      *>   classes, and one is national, the other argument is
      *>   converted to class national for purposes of comparison."
      *> Default ordering table (AR4), highest level (RV1). Every
      *> comparison is either of identical strings ("=" in any
      *> ISO/IEC 14651 table) or differs only at a final base letter
      *> C/D ("<" or ">" at level 1 in any table), so RV5/RV6 give:
      *>   A1ALPHA  <  arg-1 class alphabetic
      *>   A1ALNUM  >  arg-1 class alphanumeric
      *>   A1NE     =  arg-1 numeric-edited: class alphanumeric
      *>   A1NAT    <  arg-1 class national
      *>   A1NATLIT >  arg-1 national literal
      *>   A2ALPHA  >  arg-2 class alphabetic
      *>   A2ALNUM  <  arg-2 class alphanumeric
      *>   A2NE     =  arg-2 numeric-edited: class alphanumeric
      *>   A2NAT    >  arg-2 class national
      *>   R3XN     >  RV3: alphanumeric "ABD" made national
      *>   R3NX     <  RV3: alphanumeric arg-2 made national
      *>   R3AN     =  RV3: alphabetic "ABC" = N"ABC"
      *>   R3LIT    =  RV3: literal "ABC" = N"ABC"
      *>   R3NE     =  RV3: national numeric-edited item
      *>   (R3NE: PIC 9.99 USAGE NATIONAL is numeric-edited, class
      *>   national by §2002:8.5.2 Table 3, compared with an
      *>   alphanumeric literal after conversion.)
      *> The "=" lines between an alphanumeric and a national operand
      *> hold only if the alphanumeric one is converted (RV3); a
      *> refusal of any legal class fails its leg. Refusals: g2-stdcmp-
      *> numeric-argument-1 (PIC 9(3)) and g2-stdcmp-national-numeric-
      *> argument-2 (PIC S9(3) USAGE NATIONAL, class numeric in 2002,
      *> kb/Work PB5625 reading B).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SCM01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-A   PIC A(3) VALUE "ABC".
       01  W-X   PIC X(3) VALUE "ABD".
       01  W-N   PIC N(3) VALUE N"ABC".
       01  W-E   PIC 9.99 VALUE "1.25".
       01  W-EN  PIC 9.99 USAGE NATIONAL VALUE N"1.25".
       01  W-D   PIC 9(3) VALUE 123.
       01  W-NN  PIC S9(3) USAGE NATIONAL VALUE 123.
       PROCEDURE DIVISION.
       P-MAIN.
           DISPLAY "A1ALPHA="
               FUNCTION STANDARD-COMPARE(W-A "ABD")
           DISPLAY "A1ALNUM="
               FUNCTION STANDARD-COMPARE(W-X "ABC")
           DISPLAY "A1NE="
               FUNCTION STANDARD-COMPARE(W-E "1.25")
           DISPLAY "A1NAT="
               FUNCTION STANDARD-COMPARE(W-N N"ABD")
           DISPLAY "A1NATLIT="
               FUNCTION STANDARD-COMPARE(N"ABD" W-N)
           DISPLAY "A2ALPHA="
               FUNCTION STANDARD-COMPARE("ABD" W-A)
           DISPLAY "A2ALNUM="
               FUNCTION STANDARD-COMPARE("ABC" W-X)
           DISPLAY "A2NE="
               FUNCTION STANDARD-COMPARE("1.25" W-E)
           DISPLAY "A2NAT="
               FUNCTION STANDARD-COMPARE(N"ABD" W-N)
           DISPLAY "R3XN="
               FUNCTION STANDARD-COMPARE(W-X W-N)
           DISPLAY "R3NX="
               FUNCTION STANDARD-COMPARE(W-N W-X)
           DISPLAY "R3AN="
               FUNCTION STANDARD-COMPARE(W-A W-N)
           DISPLAY "R3LIT="
               FUNCTION STANDARD-COMPARE("ABC" N"ABC")
           DISPLAY "R3NE="
               FUNCTION STANDARD-COMPARE(W-EN "1.25")
           STOP RUN.
