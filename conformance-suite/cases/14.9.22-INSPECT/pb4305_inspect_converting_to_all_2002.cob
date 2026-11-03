      *>--- conformance-case
      *> id: pb4305_inspect_converting_to_all_2002
      *> rules: [SR-14.9.22.3-9]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 14.9.22.3 SR9: literal-4 and literal-5 are
      *>   the same size "except when literal-5 is a figurative
      *>   constant", and SR3 bars ALL from literal-1 to literal-4 only
      *>   (2002 Annex F.2 item 75). Each A and each B of "ABCAB"
      *>   becomes Z. Expected: "ZZCZZ". It fails if TO ALL "Z" is
      *>   refused or converts only the first character.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305IC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S PIC X(5) VALUE "ABCAB".
       PROCEDURE DIVISION.
       MAIN-P.
           INSPECT S CONVERTING "AB" TO ALL "Z"
           DISPLAY S
           STOP RUN.
