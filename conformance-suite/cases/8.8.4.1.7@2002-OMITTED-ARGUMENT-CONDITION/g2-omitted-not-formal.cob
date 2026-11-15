      *>--- conformance-case
      *> id: g2-omitted-not-formal
      *> rules: [2002-SR-8.8.4.1.7.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.8.4.1.7.2 SR1 (cite.py OK): "Data-name-1 shall be a
      *> formal parameter defined in the source element in which this
      *> condition is specified."  A prohibiting syntax rule granting
      *> no form, so a refusal is its evidence.
      *> Two violations, one per kind of non-formal data-name:
      *>   W  - a working-storage item of the program;
      *>   LX - a linkage-section item that is not in the procedure
      *>        division header's USING list, so it is no formal
      *>        parameter.
      *> P, the one formal, is tested legally on the line between them,
      *> so only the W and LX lines are refused (COBOL1686).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2OMR01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W PIC X(4).
       LINKAGE SECTION.
       01  P PIC X(4).
       01  LX PIC X(4).
       PROCEDURE DIVISION USING OPTIONAL P.
       MAIN-PARA.
           IF W IS OMITTED DISPLAY "W" END-IF
           IF P IS OMITTED DISPLAY "P" END-IF
           IF LX IS NOT OMITTED DISPLAY "LX" END-IF
           GOBACK.
