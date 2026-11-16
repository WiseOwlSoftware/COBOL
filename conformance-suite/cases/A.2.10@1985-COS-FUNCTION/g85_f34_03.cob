      *>--- conformance-case
      *> id: g85_f34_03
      *> rules: [1989-FMT-A.2.10.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1989:A.2.10.2 - FUNCTION COS ( argument-1 ), argument-1 as a
      *> literal, an identifier and an arithmetic expression
      *> (§1989:A.2.2). §1989:A.2.10.4 RV1: an approximation of the
      *> cosine in [-1, +1]; cos(0) = 1, cos(3.14159265) =
      *> -0.99999999.., cos(1.570796325) = 0.0000000016.. (R truncates
      *> to 6 places), each pinned by a narrow range. A leg fails if the
      *> format is refused or the value leaves its range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F3403.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  R  PIC S9V9(6).
       01  X  PIC S9V9(8) VALUE 3.14159265.
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION COS ( 0 ).
           IF R NOT < 0.999999 AND R NOT > 1
               DISPLAY "COS-LIT OK"
           ELSE
               DISPLAY "COS-LIT BAD"
           END-IF.
           COMPUTE R = FUNCTION COS ( X ).
           IF R NOT < -1 AND R NOT > -0.999999
               DISPLAY "COS-ID OK"
           ELSE
               DISPLAY "COS-ID BAD"
           END-IF.
           COMPUTE R = FUNCTION COS ( X / 2 ).
           IF R NOT < -0.000001 AND R NOT > 0.000001
               DISPLAY "COS-EXPR OK"
           ELSE
               DISPLAY "COS-EXPR BAD"
           END-IF.
           STOP RUN.
