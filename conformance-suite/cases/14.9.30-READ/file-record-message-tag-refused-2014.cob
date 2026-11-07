*>--- conformance-case
*> id: file-record-message-tag-refused-2014
*> rules: [GR-14.9.30.4-13, GR-13.18.60.4-9]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4838 - the message-tag half of file-record-pointer-and-message-tag-round-trip is introduced by COBOL-2023
      *> (USAGE MESSAGE-TAG, 13.18.60.4 GR9; Annex E.2 item 25), so a file record that holds one is refused below it, on the
      *> tag's own introduction gate; the strongly-typed group and the file statements are all older.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FILERECTAGREFUSED.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT TF ASSIGN TO "filerectagrefused.dat"
              ORGANIZATION SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD TF.
       01 FREC TYPE T-REC.
       WORKING-STORAGE SECTION.
       01 T-REC IS TYPEDEF STRONG.
          05 TKEY PIC 9(2).
          05 TTAG USAGE MESSAGE-TAG.
       PROCEDURE DIVISION.
           OPEN OUTPUT TF
           MOVE 1 TO TKEY OF FREC
           WRITE FREC
           CLOSE TF
           STOP RUN.
