      *>--- conformance-case
      *> id: pb5801_debug_level66_target
      *> rules: [1985-GR-XV.3.2.4-6]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5801 review R1-1. The target X66 is a level 66 entry
      *>   renaming G-X; no syntax rule of the statement excludes one.
      *>   §1985:XV.3.2.4 GR6 c: the section runs after "any other COBOL
      *>   statement that explicitly references and causes the content of the
      *>   data item referenced by identifier-1 to be changed".
      *> DERIVATION. DISPLAY G-X changes nothing: A ABC. MOVE "XYZ" TO X66
      *>   references X66 and changes it: the section runs, DEBUG-NAME X66.
      *>   DISPLAY X66 (1:2) changes nothing: B XY. MOVE "QQQ" TO G-X changes
      *>   the same storage but references G-X, not identifier-1: no run.
      *>   C QQQ. Fails if a reference through the alias is read as one to
      *>   G-X (the section never ran), or a reference to G-X as one to X66.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5801DB.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. X WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           02  G-X PIC X(3) VALUE "ABC".
       66  X66 RENAMES G-X.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D1 SECTION.
           USE FOR DEBUGGING ON X66.
       D1P.
           DISPLAY "DBG " DEBUG-NAME.
       END DECLARATIVES.
       M SECTION.
       P1.
           DISPLAY "A " G-X.
           MOVE "XYZ" TO X66.
           DISPLAY "B " X66 (1:2).
           MOVE "QQQ" TO G-X.
           DISPLAY "C " G-X.
           STOP RUN.
