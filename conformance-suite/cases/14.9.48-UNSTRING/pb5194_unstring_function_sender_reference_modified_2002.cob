      *>--- conformance-case
      *> id: pb5194_unstring_function_sender_reference_modified_2002
      *> rules: [SR-14.9.48.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5194 - a reference-modified function-identifier as the UNSTRING sending operand. 14.9.48.3 SR2
      *> asks only the category of identifier-1 (2002 Annex F.2 item 139 lifted the COBOL-85 bar, §1985:VI.6.27.3
      *> SR7 "Identifier-1 must not be reference modified"). FUNCTION UPPER-CASE (SRC) is "AB,CD,E" and (4:4) is
      *> "CD,E", split at the comma into T1 = "CD" and T2 = "E" (each space-filled to five characters).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5194US.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  SRC PIC X(7) VALUE "ab,cd,e".
       01  T1 PIC X(5).
       01  T2 PIC X(5).
       PROCEDURE DIVISION.
       P1.
           UNSTRING FUNCTION UPPER-CASE (SRC) (4:4) DELIMITED BY ","
               INTO T1 T2.
           DISPLAY "[" T1 "][" T2 "]".
           STOP RUN.
