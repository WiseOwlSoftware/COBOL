      *>--- conformance-case
      *> id: pb4305-inspect-converting-to-all-at-85
      *> rules: [SR-14.9.22.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_inspect_converting_to_all_2002. COBOL-85 did not
      *>   allow a figurative constant beginning with ALL as the
      *>   CONVERTING TO operand (2002 Annex F.2 item 75). Expected:
      *>   COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NIC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(5) VALUE "ABCAB".
       PROCEDURE DIVISION.
       MAIN-P.
           INSPECT S CONVERTING "AB" TO ALL "Z"
           STOP RUN.
