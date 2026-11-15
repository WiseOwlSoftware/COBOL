      *>--- conformance-case
      *> id: pb5427-external-record-name-reused-sibling-85
      *> rules: [1985-SR-IV.4.3.8.1-4]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5427 - the ACCEPTED side of conformance:pb5427-external-record-name-reused-sibling: at --std 85 the
      *>   external record EXR of SBL5427A and the item EXR subordinate to the external record OTHER-EXT of SBL5427B are
      *>   in two programs neither of which contains the other, and §1985:IV.4.3.8.1 SR4 reaches only "any program contained
      *>   within or containing the program which describes that external data record" (cite.py: OK). The program only
      *>   translates and runs to STOP RUN, so its .out is empty.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SBL5427A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXR EXTERNAL PIC X.
       PROCEDURE DIVISION.
       P1.
           STOP RUN.
       END PROGRAM SBL5427A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SBL5427B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OTHER-EXT EXTERNAL.
           05  EXR PIC X.
       PROCEDURE DIVISION.
       Q1.
           STOP RUN.
       END PROGRAM SBL5427B.
