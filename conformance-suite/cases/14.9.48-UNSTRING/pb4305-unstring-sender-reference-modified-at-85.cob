      *>--- conformance-case
      *> id: pb4305-unstring-sender-reference-modified-at-85
      *> rules: [SR-14.9.48.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_unstring_sender_reference_modified_2002. COBOL-85 did
      *>   not allow a reference-modified UNSTRING sending operand
      *>   (2002 Annex F.2 item 139). Expected: COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NUS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(10) VALUE "AB,CD,EF  ".
       01 A PIC X(2).
       01 B PIC X(2).
       PROCEDURE DIVISION.
       MAIN-P.
           UNSTRING S (1:5) DELIMITED BY "," INTO A B
           STOP RUN.
