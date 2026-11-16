      *>--- conformance-case
      *> id: pb5775-source-homonym
      *> rules: [SR-13.18.53.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5775. §2023:8.4.2.2.3 SR1 (cite.py: OK): "uniqueness shall be
      *> established through a sequence of qualifiers that precludes any
      *> ambiguity of reference". Working-Storage DFLD and report entry DFLD are two
      *> declarations of one user-defined name, and SOURCE DFLD
      *> names it unqualified: refused; DFLD OF R1 is the report entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5775G.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "srchom.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WS-REC.
           05 DFLD PIC X(3) VALUE "WSV".
       REPORT SECTION.
       RD R1.
       01 DET TYPE IS DETAIL.
           02 LINE PLUS 1.
               03 DFLD COLUMN 1 PIC X(3) VALUE "RPV".
               03 COLUMN 6 PIC X(3) SOURCE DFLD.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPF.
           INITIATE R1.
           GENERATE DET.
           TERMINATE R1.
           CLOSE RPF.
           DISPLAY "DONE".
           STOP RUN.
