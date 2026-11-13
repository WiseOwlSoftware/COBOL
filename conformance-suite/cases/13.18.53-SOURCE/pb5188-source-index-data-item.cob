*>--- conformance-case
*> id: pb5188-source-index-data-item
*> rules: [SR-13.18.53.3-2, SR-13.18.60.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5188. §2023:13.18.53.3 SR2: identifier-1 "shall be described such that a MOVE statement is valid
      *> with identifier-1 as the sending operand", and §2023:14.9.25.3 SR1: "The class of identifier-1 or
      *> identifier-2 shall not be index, message-tag, object, or pointer". SOURCE IS IX names an index data item,
      *> which §2023:13.18.60.3 SR10 (and §1985:VI.5.14.3 SR5) admit in no SOURCE clause, so it is refused (COBOL2770)
      *> at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5188S.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT-FILE ASSIGN TO "PB5188SR".
       DATA DIVISION.
       FILE SECTION.
       FD  RPT-FILE
           REPORT IS RPT.
       WORKING-STORAGE SECTION.
       01  IX USAGE IS INDEX.
       REPORT SECTION.
       RD  RPT.
       01  DL TYPE DETAIL LINE PLUS 1.
           10  COLUMN 1 PIC 9(4) SOURCE IS IX.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPT-FILE.
           INITIATE RPT.
           GENERATE DL.
           TERMINATE RPT.
           CLOSE RPT-FILE.
           STOP RUN.
