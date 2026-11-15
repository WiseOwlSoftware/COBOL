      *>--- conformance-case
      *> id: pb5427-external-record-name-reused-nested
      *> rules: [1985-SR-IV.4.3.8.1-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5427 - the EXTERNAL half of §1985:IV.4.3.8.1 SR4: "the same data-name must not be used as the name of an
      *>   external record and as the name of any other external data item described in any program contained within or
      *>   containing the program which describes that external data record" (cite.py: OK); from COBOL-2002 the reach is
      *>   "the run unit" (§2023:8.4.2.1, cite.py: OK), which holds this nested pair too. EXR is the external record of
      *>   NEST5427 and the name of an item subordinate to the external record OTHER-EXT of the contained NEST5427I, so a
      *>   program that accepts it binds the two EXRs to different external areas. The refused line is 28.
      *>   conformance:pb5427-external-record-name-shared-legal is the legal side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEST5427.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXR EXTERNAL PIC X.
       PROCEDURE DIVISION.
       P1.
           CALL "NEST5427I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. NEST5427I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OTHER-EXT EXTERNAL.
           05  EXR PIC X.
       PROCEDURE DIVISION.
       Q1.
           EXIT PROGRAM.
       END PROGRAM NEST5427I.
       END PROGRAM NEST5427.
