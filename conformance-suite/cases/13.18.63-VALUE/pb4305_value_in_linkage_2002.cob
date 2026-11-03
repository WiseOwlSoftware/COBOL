      *>--- conformance-case
      *> id: pb4305_value_in_linkage_2002
      *> rules: [GR-13.18.63.4-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - 13.18.63.4 GR3: "In the linkage section,
      *>   VALUE clauses take effect only during the execution of an
      *>   explicit or implicit INITIALIZE statement" (2002 Annex F.2
      *>   item 145). L is passed "Q", so its VALUE "A" does not show.
      *>   Expected: "Q". It fails if the VALUE clause is refused or
      *>   overwrites the argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305VL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X VALUE "Q".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB4305VS" USING W
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305VS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X VALUE "A".
       PROCEDURE DIVISION USING L.
       MAIN-S.
           DISPLAY L
           GOBACK.
       END PROGRAM PB4305VS.
       END PROGRAM PB4305VL.
