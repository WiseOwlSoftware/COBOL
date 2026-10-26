*>--- conformance-case
*> id: pb1319-sign-group-unsigned-85
*> rules: [SR-13.18.52.3-1]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1319 - COBOL-85 admits a group SIGN clause only on "a group item containing at least one such
      *> numeric data description entry" (one "whose PICTURE contains the character S"; ANSI X3.23-1985 as the vendor
      *> manuals transcribe it). G holds an alphanumeric and an UNSIGNED numeric item, so at --std 85 the clause is
      *> refused with COBOL0900. The 2023 text of 13.18.52.3 SR1 admits any alphanumeric group item
      *> (cite.py: OK  13.18.52.3 1)), and the derived edge is 2002 (VCR row 7.40); the positive is
      *> 2002/pb1319_sign_group_subjects_2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1319U.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G SIGN IS LEADING SEPARATE.
          05 X PIC X(3) VALUE "ABC".
          05 N PIC 9(3) VALUE 5.
       PROCEDURE DIVISION.
           DISPLAY G
           STOP RUN.
