      *>--- conformance-case
      *> id: pb4305-call-using-level-05-group-at-85
      *> rules: [SR-14.9.4.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4305 - the edition half of
      *>   pb4305_call_using_level_05_group_2002. COBOL-85 required a
      *>   CALL argument to be elementary or at level 01 or 77 (2002
      *>   Annex F.2 item 10). H is a level-05 group. Expected:
      *>   COBOL0900 at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4305NCL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G.
           05 H.
               10 H1 PIC X(2) VALUE "AB".
       PROCEDURE DIVISION.
       MAIN-P.
           CALL "PB4305NCS" USING H
           STOP RUN.
