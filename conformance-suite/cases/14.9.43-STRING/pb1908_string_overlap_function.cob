      *>--- conformance-case
      *> id: pb1908_string_overlap_function
      *> rules: [GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908/PB4973 -- STRING with a FUNCTION operand whose
      *> argument shares storage with the receiving item
      *> (identifier-3).
      *> ISO 8.4.3.2.4 GR1: a function-identifier references a
      *> temporary data item whose value is determined when the
      *> function is referenced; 14.6.4 7): the identifiers of a
      *> statement are evaluated in left to right order as the
      *> first operation. No transfer stores into the temporary
      *> item, so 14.9.43.4 GR10 (overlap undefined) never reaches
      *> a function: it is evaluated ONCE, before any character
      *> moves (docs/CONFORMANCE.md D-STR4). GnuCOBOL 3.2 evaluates
      *> it again when its transfer begins; the standard controls.
      *> F1: FUNCTION UPPER-CASE(WS-F) is ABCDEFGH of the original
      *>     WS-F, of which six characters fit after "xy" (GR8).
      *>     Result xyABCDEF.
      *> F2: the delimiter FUNCTION UPPER-CASE(WS-X(1:1)) is "Q"
      *>     of the original WS-X, so "12Z34Q56" is cut at "Q":
      *>     "12Z34" fills 2-6. Result z12Z34....
      *> F3: the function's argument is WS-S, outside the receiver.
      *>     Result ab-ABC-...
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-F   PIC X(8) VALUE "abcdefgh".
       01 WS-X   PIC X(10) VALUE "Q.........".
       01 WS-S   PIC X(3) VALUE "abc".
       01 WS-Y   PIC X(10) VALUE "..........".
       PROCEDURE DIVISION.
           STRING "xy" DELIMITED BY SIZE
                  FUNCTION UPPER-CASE(WS-F) DELIMITED BY SIZE
               INTO WS-F
           END-STRING
           DISPLAY "F1 [" WS-F "]"
           STRING "z" DELIMITED BY SIZE
                  "12Z34Q56" DELIMITED BY FUNCTION UPPER-CASE(WS-X(1:1))
               INTO WS-X
           END-STRING
           DISPLAY "F2 [" WS-X "]"
           STRING "ab-" DELIMITED BY SIZE
                  FUNCTION UPPER-CASE(WS-S) DELIMITED BY SIZE
                  "-" DELIMITED BY SIZE
               INTO WS-Y
           END-STRING
           DISPLAY "F3 [" WS-Y "]"
           STOP RUN.
