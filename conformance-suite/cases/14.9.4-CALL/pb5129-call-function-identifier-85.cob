      *>--- conformance-case
      *> id: pb5129-call-function-identifier-85
      *> rules: [GR-14.9.4.4-8]
      *> source-format: fixed
      *>---
      *> kb/Work PB5129 - X3.23-1985 Amendment 1 adds to the CALL
      *> statement "Identifier-2 must not be a function-identifier." (X
      *> 5.2.3 SR4), and identifier-2 is the operand of both USING
      *> phrases (X 5.2.2). So at 85 the BY CONTENT function argument is
      *> refused with COBOL0900 (construct call-function-identifier-
      *> argument-2002); from 2002 it is identifier-2 (14.9.4.4 GR8),
      *> pb5129_call_by_content_function_identifier.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5129N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WS-A                PIC X(4) VALUE "abcd".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "PB5129T" USING BY CONTENT FUNCTION UPPER-CASE (WS-A).
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5129T.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LS-ARG              PIC X(4).
       PROCEDURE DIVISION USING LS-ARG.
       SUB-PARA.
           DISPLAY "[" LS-ARG "]".
           EXIT PROGRAM.
       END PROGRAM PB5129T.
       END PROGRAM PB5129N.
