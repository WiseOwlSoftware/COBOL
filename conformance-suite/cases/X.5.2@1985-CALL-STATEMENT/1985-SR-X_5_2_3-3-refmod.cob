      *>--- conformance-case
      *> id: 1985-SR-X_5_2_3-3-refmod
      *> rules: [1985-SR-X.5.2.3-3]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.3 SR3 admits "an elementary data item" as a USING
      *> operand, and §1985:IV.4.3.8.4.4 GR5 makes the unique data item a
      *> reference modification creates "an elementary data item". WG is a
      *> level 05 group, but WG(1:2) is elementary, so the CALL below is
      *> legal at 85 and the subprogram displays the two characters.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFMODC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  WG.
               10  WB PIC X(2) VALUE "CD".
               10  WC PIC X(2) VALUE "EF".
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "REFMODS" USING WG(1:2).
           STOP RUN.
       END PROGRAM REFMODC.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. REFMODS.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X(2).
       PROCEDURE DIVISION USING L1.
       S.
           DISPLAY "IN SUBP [" L1 "]".
           EXIT PROGRAM.
       END PROGRAM REFMODS.
