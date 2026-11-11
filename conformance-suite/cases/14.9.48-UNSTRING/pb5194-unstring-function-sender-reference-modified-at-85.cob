      *>--- conformance-case
      *> id: pb5194-unstring-function-sender-reference-modified-at-85
      *> rules: [SR-14.9.48.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5194 - the edition half of pb5194_unstring_function_sender_reference_modified_2002.
      *> §1985:VI.6.27.3 SR7: "Identifier-1 must not be reference modified." The bar is on the operand, so a
      *> reference-modified function-identifier is refused as the data-name form is. Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5194NUS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC PIC X(7) VALUE "ab,cd,e".
       01  T1 PIC X(5).
       01  T2 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           UNSTRING FUNCTION UPPER-CASE (SRC) (4:4) DELIMITED BY ","
               INTO T1 T2.
           STOP RUN.
