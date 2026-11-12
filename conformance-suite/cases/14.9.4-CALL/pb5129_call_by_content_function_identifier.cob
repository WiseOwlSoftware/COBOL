      *>--- conformance-case
      *> id: pb5129_call_by_content_function_identifier
      *> rules: [GR-14.9.4.4-8, FMT-14.9.4.2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB5129 - from ISO/IEC 1989:2002 a function-identifier is
      *> a CALL USING BY CONTENT argument: Format 1 prints BY CONTENT
      *> { identifier-2 } ... (14.9.4.2), 14.9.4.4 GR8: "An argument that
      *> consists merely of a single identifier or literal is regarded
      *> as an identifier or literal", and no rule bars a function there
      *> (14.9.4.3 SR3 bars it BY REFERENCE only). X3.23-1985 Amendment 1
      *> says "Identifier-2 must not be a function-identifier." (X 5.2.3
      *> SR4); its refusal is pb5129-call-function-identifier-85.
      *> DERIVATION (WS-A = "abcd"): the first CALL passes the values of
      *> UPPER-CASE(WS-A) = ABCD and, keyword-less after BY CONTENT (GR5
      *> carries the mode), REVERSE(WS-A) = dcba; the second passes WS-A
      *> and UPPER-CASE(WS-A). The callee stores into its first formal,
      *> which BY CONTENT never carries back, so WS-A stays abcd.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5129C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A                PIC X(4) VALUE "abcd".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB5129S" USING BY CONTENT FUNCTION UPPER-CASE (WS-A)
               FUNCTION REVERSE (WS-A).
           CALL "PB5129S" USING BY CONTENT WS-A
               FUNCTION UPPER-CASE (WS-A).
           DISPLAY "WS-A [" WS-A "]".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5129S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LS-ARG              PIC X(4).
       01  LS-TWO              PIC X(4).
       PROCEDURE DIVISION USING LS-ARG LS-TWO.
       SUB-PARA.
           DISPLAY "[" LS-ARG "][" LS-TWO "]".
           MOVE "ZZZZ" TO LS-ARG.
           GOBACK.
       END PROGRAM PB5129S.
       END PROGRAM PB5129C.
