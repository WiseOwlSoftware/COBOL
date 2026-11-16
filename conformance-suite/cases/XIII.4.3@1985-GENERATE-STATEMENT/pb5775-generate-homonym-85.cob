      *>--- conformance-case
      *> id: pb5775-generate-homonym-85
      *> rules: [1985-FMT-XIII.4.3.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5775. §1985:IV.4.3.8.1 SRL2.1 (cite.py: OK): "For each nonunique
      *> user-defined name that is explicitly referenced, uniqueness must be
      *> established through a sequence of qualifiers which precludes any
      *> ambiguity of reference." Working-Storage DA and report group DA are two
      *> declarations of one user-defined name, and GENERATE DA
      *> names it unqualified: the program is refused, and DA OF R1 is the
      *> spelling of the report entry. A compiler that resolves the report
      *> group name in a namespace of its own compiles it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5775A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPT ASSIGN TO "m4.rpt".
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
           OPEN OUTPUT RPT.
           INITIATE R-1.
           GENERATE DA.
           TERMINATE R-1.
           CLOSE RPT.
           DISPLAY "RAN".
           STOP RUN.
