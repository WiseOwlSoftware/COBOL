      *>--- conformance-case
      *> id: g2mov20_cd_move_corr_elementary
      *> rules: [2002-SR-14.8.24.2-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.24.2 SR10 - "Identifier-3 and identifier-4 shall
      *> specify group data items and shall not be reference
      *> modified." Identifier-3 here is CE1, an elementary item of an
      *> output CD's record, so the MOVE CORRESPONDING at line 23 must
      *> be refused. The control at line 24 (the CD group CG) is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MOV20.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WG.
          05 CE1 PIC X(3).
       COMMUNICATION SECTION.
       CD CDO FOR OUTPUT.
       01 CG.
          05 CE1 PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
           MOVE CORRESPONDING CE1 OF CG TO WG.
           MOVE CORRESPONDING CG TO WG.
           STOP RUN.
