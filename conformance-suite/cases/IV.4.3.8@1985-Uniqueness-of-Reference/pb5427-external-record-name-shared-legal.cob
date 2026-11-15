      *>--- conformance-case
      *> id: pb5427-external-record-name-shared-legal
      *> rules: [1985-SR-IV.4.3.8.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5427 - the LEGAL side of the EXTERNAL half of §1985:IV.4.3.8.1 SR4 (cite.py: OK on "the same data-name must
      *>   not be used as the name of an external record and as the name of any other external data item described in any
      *>   program contained within or containing the program which describes that external data record"). The rule bars
      *>   one name for an external record AND for another external data item; two programs that each describe the external
      *>   record EXR describe ONE record, the sharing the EXTERNAL clause is for, so the name is the record's alone.
      *>   DERIVATION: SHARE5427 moves "Q" to EXR and calls the contained SHARE5427I; its EXR is the same external record
      *>   (§2023:8.6.3: the external attribute makes the record one item of the run unit), so it displays "Q".
      *>   conformance:pb5427-external-record-name-reused-nested is the refused side.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SHARE5427.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXR EXTERNAL PIC X.
       PROCEDURE DIVISION.
       P1.
           MOVE "Q" TO EXR.
           CALL "SHARE5427I".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SHARE5427I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXR EXTERNAL PIC X.
       PROCEDURE DIVISION.
       Q1.
           DISPLAY EXR.
           EXIT PROGRAM.
       END PROGRAM SHARE5427I.
       END PROGRAM SHARE5427.
