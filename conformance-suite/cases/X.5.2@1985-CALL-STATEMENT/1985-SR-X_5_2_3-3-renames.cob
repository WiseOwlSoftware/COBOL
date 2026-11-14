      *>--- conformance-case
      *> id: 1985-SR-X_5_2_3-3-renames
      *> rules: [1985-SR-X.5.2.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.5.2.3 SR3 (THE CALL STATEMENT). A refusal: a USING
      *> operand "must be a level 01 data item, a level 77 data item, or
      *> an elementary data item". W66G is a level 66 item, and
      *> §1985:VI.5.11.4 GR1 makes it, with THROUGH, a group item, so the
      *> CALL below is refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. RENAMEC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  REC.
           05  WA PIC X(2) VALUE "AB".
           05  WB PIC X(2) VALUE "CD".
       66  W66G RENAMES WA THRU WB.
       PROCEDURE DIVISION.
       MAIN-PARA.
           CALL "RENAMES" USING W66G.
           STOP RUN.
