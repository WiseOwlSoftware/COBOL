      *>--- conformance-case
      *> id: g85_y94
      *> rules: [1985-SR-VII.2.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.5.3 SR2 (cite.py --check OK): "Data-name-1 must be
      *> defined in the Data Division as a two-character data item of
      *> the category alphanumeric". A refusal: FS-N is a PIC 99 item,
      *> category numeric, so it cannot be the FILE STATUS item (the
      *> line of the SELECT entry that names it). Its only fault.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85Y94.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "FSP1.DAT" FILE STATUS IS FS-N.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(10).
       WORKING-STORAGE SECTION.
       01 FS-N PIC 99.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
