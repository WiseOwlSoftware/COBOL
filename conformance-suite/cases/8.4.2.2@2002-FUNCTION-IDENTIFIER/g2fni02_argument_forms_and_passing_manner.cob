      *>--- conformance-case
      *> id: g2fni02_argument_forms_and_passing_manner
      *> rules: [2002-SR-8.4.2.2.2-6, 2002-GR-8.4.2.2.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.2.2 SR6 - "Argument-1 shall be an identifier, a
      *> literal, a boolean expression, or an arithmetic expression."
      *> Each of the four granted forms is passed to a user-defined
      *> function below and must be accepted and deliver its value.
      *> §2002:8.4.2.2.3 GR4 - the manner of passing for
      *> function-prototype-name-1:
      *>  a) BY REFERENCE for a receiving-capable identifier when BY
      *>     REFERENCE is specified or implied (implied: §2002:14.1.3 GR4);
      *>  b) BY CONTENT for a literal, an arithmetic or boolean
      *>     expression, or an identifier not permitted as a receiving
      *>     operand (a function-identifier, §2002:8.4.2.2.2 SR1);
      *>  c) BY VALUE when the formal says BY VALUE.
      *> A function may not store into its formal (§2002:13.6.2 SR5 of the
      *> linkage section), so G2FNR and G2FNV change the
      *> caller's storage through the EXTERNAL item WX that both sides
      *> describe, then return their formal unchanged: only a formal
      *> that OCCUPIES the argument's storage (BY REFERENCE,
      *> §2002:14.1.3 GR8) sees the change.
      *> DERIVATION:
      *>  R  WX = 41, identifier, implied BY REFERENCE (a): G2FNR adds 1
      *>     to WX and returns its formal, which is WX: "R=0042 X=0042"
      *>  V  WX = 42, BY VALUE formal (c): the formal is a copy made
      *>     before the call (§2002:14.1.3 GR10); G2FNV adds 1 to WX and
      *>     returns the copy: "V=0042 X=0043"
      *>  E  WX + 1 = 44, arithmetic expression, BY CONTENT (b): G2FNF
      *>     returns 44 + 1: "E=0045 X=0043"
      *>  T  2.7 + 1, arithmetic expression, BY CONTENT into the PIC 9(4)
      *>     formal as a COMPUTE without ROUNDED (§2002:14.1.3 GR9): 3,
      *>     returned + 1: "T=0004"
      *>  N  numeric literal 7, BY CONTENT: "N=0008"
      *>  B  WB B-AND B"1010" = 1100 B-AND 1010 = 1000, a boolean
      *>     expression, BY CONTENT into the PIC 1(4) formal: "B=1000"
      *>  L  alphanumeric literal "wxyz", BY CONTENT: "L=wxyz"
      *>  U  FUNCTION UPPER-CASE(WA), a function-identifier (not a
      *>     receiving operand), BY CONTENT with value "ABCD": "U=ABCD"
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FNR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WX PIC 9(4) EXTERNAL.
       LINKAGE SECTION.
       01  L-X PIC 9(4).
       01  L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           ADD 1 TO WX.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION G2FNR.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FNV.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WX PIC 9(4) EXTERNAL.
       LINKAGE SECTION.
       01  L-X PIC 9(4).
       01  L-R PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE L-X RETURNING L-R.
           ADD 1 TO WX.
           MOVE L-X TO L-R.
           GOBACK.
       END FUNCTION G2FNV.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FNF.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-X PIC 9(4).
       01  L-R PIC 9(4).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
           COMPUTE L-R = L-X + 1.
           GOBACK.
       END FUNCTION G2FNF.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FNB.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-B PIC 1(4).
       01  L-S PIC 1(4).
       PROCEDURE DIVISION USING L-B RETURNING L-S.
           MOVE L-B TO L-S.
           GOBACK.
       END FUNCTION G2FNB.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FNA.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L-A PIC X(4).
       01  L-U PIC X(4).
       PROCEDURE DIVISION USING L-A RETURNING L-U.
           MOVE L-A TO L-U.
           GOBACK.
       END FUNCTION G2FNA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FNI02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION G2FNR
           FUNCTION G2FNV
           FUNCTION G2FNF
           FUNCTION G2FNB
           FUNCTION G2FNA.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WX PIC 9(4) EXTERNAL.
       01  WB PIC 1(4) VALUE B"1100".
       01  WA PIC X(4) VALUE "abcd".
       01  WR PIC 9(4).
       01  WS PIC X(4).
       01  WT PIC 1(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE 41 TO WX.
           COMPUTE WR = FUNCTION G2FNR(WX).
           DISPLAY "R=" WR " X=" WX.
           COMPUTE WR = FUNCTION G2FNV(WX).
           DISPLAY "V=" WR " X=" WX.
           COMPUTE WR = FUNCTION G2FNF(WX + 1).
           DISPLAY "E=" WR " X=" WX.
           COMPUTE WR = FUNCTION G2FNF(2.7 + 1).
           DISPLAY "T=" WR.
           COMPUTE WR = FUNCTION G2FNF(7).
           DISPLAY "N=" WR.
           MOVE FUNCTION G2FNB(WB B-AND B"1010") TO WT.
           DISPLAY "B=" WT.
           MOVE FUNCTION G2FNA("wxyz") TO WS.
           DISPLAY "L=" WS.
           MOVE FUNCTION G2FNA(FUNCTION UPPER-CASE(WA)) TO WS.
           DISPLAY "U=" WS.
           STOP RUN.
       END PROGRAM G2FNI02.
