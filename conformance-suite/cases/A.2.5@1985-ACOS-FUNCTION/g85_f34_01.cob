      *>--- conformance-case
      *> id: g85_f34_01
      *> rules: [1989-FMT-A.2.5.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.5.2 - FUNCTION ACOS ( argument-1 ), argument-1 as a
      *> literal, an identifier and an arithmetic expression
      *> (§1989:A.2.2). §1989:A.2.5.4 RV1 bounds the arccosine to [0,
      *> pi]; the value is an approximation, so each leg is pinned by a
      *> narrow range: acos(1) = 0, acos(-1) = pi = 3.14159265..,
      *> acos(0) = 1.57079632.. (R truncates to 6 places). A leg fails
      *> if the format is refused, if the argument is not passed, or if
      *> the value leaves its range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3401.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R  PIC S9V9(6).
       01  X  PIC S9V9 VALUE -1.0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION ACOS ( 1 ).
           IF R NOT < 0 AND R NOT > 0.000001
               DISPLAY "ACOS-LIT OK"
           ELSE
               DISPLAY "ACOS-LIT BAD"
           END-IF.
           COMPUTE R = FUNCTION ACOS ( X ).
           IF R NOT < 3.141592 AND R NOT > 3.141593
               DISPLAY "ACOS-ID OK"
           ELSE
               DISPLAY "ACOS-ID BAD"
           END-IF.
           COMPUTE R = FUNCTION ACOS ( X + 1 ).
           IF R NOT < 1.570796 AND R NOT > 1.570797
               DISPLAY "ACOS-EXPR OK"
           ELSE
               DISPLAY "ACOS-EXPR BAD"
           END-IF.
           STOP RUN.
