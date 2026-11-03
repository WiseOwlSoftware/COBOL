      *>--- conformance-case
      *> id: pb4297_infinity_words_user_defined
      *> rules: [SR-8.3.2.1-1]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4297 - 8.3.2.1 1): "Reserved words shall not be
      *>   used as user-defined words or system-names." NEGATIVE-INFINITY
      *>   and POSITIVE-INFINITY are on no edition's 8.9 list: the 1985
      *>   and 2002 standards do not have them, the 2009 draft lists them
      *>   in 8.10 as context-sensitive words of its ROUNDED phrase (a
      *>   context-sensitive word is a user-defined word outside its
      *>   context), and the 2023 standard has neither. So at every
      *>   edition each is a user-defined word. Expected output: "NP".
      *>   It fails if either is refused as reserved (COBOL0901), as
      *>   GnuCOBOL's 2002 and 2014 lists made them.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4297INF.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NEGATIVE-INFINITY PIC X VALUE "N".
       01 POSITIVE-INFINITY PIC X VALUE "P".
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY NEGATIVE-INFINITY POSITIVE-INFINITY
           STOP RUN.
