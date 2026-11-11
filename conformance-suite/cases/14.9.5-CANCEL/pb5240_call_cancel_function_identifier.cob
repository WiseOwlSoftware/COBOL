      *>--- conformance-case
      *> id: pb5240_call_cancel_function_identifier
      *> rules: [SR-14.9.5.3-1, SR-14.9.4.3-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> A function-identifier is an identifier (ISO 8.4.3.1.2 Format
      *> 1), "shall not be specified as a receiving operand" (8.4.3.2.3
      *> SR1) and is barred nowhere else; X3.23-1985 Amendment 1 says
      *> an alphanumeric one "may be specified anywhere in the general
      *> formats that an identifier is permitted" (IV 4.3.8.3.3 SR3).
      *> So CALL identifier-1 and CANCEL identifier-1 take one whose
      *> value is an alphanumeric program-name (14.9.4.3 SR1, 14.9.5.3
      *> SR1; kb/Work PB5240). CFSUB1 counts its calls in WORKING-
      *> STORAGE: each leg fails if a CALL does not reach it (no line),
      *> or a CANCEL does not reach it (a count of 2): CANCEL makes the
      *> next CALL find the program in its initial state (14.9.5.4
      *> GR3), so every line shows a count of 1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5240C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC X(6) VALUE "cfsub1".
       01 R PIC X(6) VALUE "1BUSFC".
       PROCEDURE DIVISION.
       P1.
           CALL FUNCTION UPPER-CASE (N).
           CANCEL FUNCTION UPPER-CASE (N).
           CALL FUNCTION REVERSE (R).
           CANCEL FUNCTION REVERSE (R) "CFSUB1".
           CALL FUNCTION UPPER-CASE (N).
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CFSUB1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C PIC 9 VALUE 0.
       01 T PIC X.
       PROCEDURE DIVISION.
       S1.
           ADD 1 TO C.
           MOVE C TO T.
           DISPLAY "CALLS " T.
           EXIT PROGRAM.
       END PROGRAM CFSUB1.
       END PROGRAM PB5240C.
