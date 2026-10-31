*>--- conformance-case
*> id: pb916-message-tag-introduction
*> rules: [GR-13.18.60.4-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB916 - BELOW COBOL-2023 the entry draws the ORDINARY INTRODUCTION GATE, and only that.
      *> ISO 13.18.60.4 GR9: "The MESSAGE-TAG clause specifies that a data item is a message-tag data item"
      *> (cite.py --check 13.18.60.4 "The MESSAGE-TAG clause specifies" OK, General rules 9).  The word is reserved
      *> from COBOL-2023 only (8.9; Annex E.2 item 25), so at 85, 2002 and 2014 this entry names a facility the
      *> edition does not have: COBOL0900 "USAGE MESSAGE-TAG requires COBOL-2023".
      *> At 2023 the usage exists and this implementation DECLINES it (Annex A.3 item 4; COBOL1943 - see
      *> pb487-message-tag-usage).  COBOL1943 used to fire at 85, 2002 and 2014 too, byte for byte the 2023
      *> message, although its own text says that below 2023 "the word is a user-defined word and the entry draws
      *> the ordinary introduction gate" - the Annex A.3 decline pre-empted the gate it described.  The harness
      *> checks that the named code is REPORTED; that the decline is no longer reported below 2023 is held by
      *> MessageTagIntroductionGateTests.  Refused at 85, 2002 and 2014; the 2023 cell is pb487.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB916MTI.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 M USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
