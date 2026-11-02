      *>--- conformance-case
      *> id: pb4305_unstring_sender_reference_modified_2002
      *> rules: [SR-14.9.48.3-2]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 14.9.48.3 SR2 asks only the category of
      *>   identifier-1, which may be reference modified (2002 Annex
      *>   F.2 item 139). S (1:5) is "AB,CD", split at the comma.
      *>   Expected: "AB" and "CD" shown as "ABCD". It fails if the
      *>   sender is refused or the whole of S is split.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305US.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(10) VALUE "AB,CD,EF  ".
       01 A PIC X(2).
       01 B PIC X(2).
       PROCEDURE DIVISION.
       MAIN-P.
           UNSTRING S (1:5) DELIMITED BY "," INTO A B
           DISPLAY A B
           STOP RUN.
