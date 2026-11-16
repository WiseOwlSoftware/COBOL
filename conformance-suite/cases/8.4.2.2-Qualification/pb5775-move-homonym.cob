      *>--- conformance-case
      *> id: pb5775-move-homonym
      *> rules: [SR-8.4.2.2.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5775. §2023:8.4.2.2.3 SR1 (cite.py: OK): "uniqueness shall be
      *> established through a sequence of qualifiers that precludes any
      *> ambiguity of reference". Working-Storage DA and report group DA are two
      *> declarations of one user-defined name, and MOVE "Z" TO DA
      *> names it unqualified: refused; DA OF R1 is the report entry.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5775D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "m4m.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPT REPORT IS R-1.
       WORKING-STORAGE SECTION.
       01 DA PIC X VALUE "W".
       REPORT SECTION.
       RD R-1.
       01 DA TYPE DE LINE PLUS 1.
          02 COLUMN 1 PIC X(3) VALUE "DET".
       PROCEDURE DIVISION.
       P1.
           MOVE "Z" TO DA.
           DISPLAY "DA=" DA.
           STOP RUN.
