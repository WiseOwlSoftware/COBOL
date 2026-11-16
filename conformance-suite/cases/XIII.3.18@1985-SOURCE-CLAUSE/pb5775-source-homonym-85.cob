      *>--- conformance-case
      *> id: pb5775-source-homonym-85
      *> rules: [1985-SR-XIII.3.18.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5775. §1985:IV.4.3.8.1 SRL2.1 (cite.py: OK): "For each nonunique
      *> user-defined name that is explicitly referenced, uniqueness must be
      *> established through a sequence of qualifiers which precludes any
      *> ambiguity of reference." Working-Storage DFLD and report entry DFLD are two
      *> declarations of one user-defined name, and SOURCE DFLD
      *> names it unqualified: the program is refused, and DFLD OF R1 is the
      *> spelling of the report entry. A compiler that resolves the report
      *> group name in a namespace of its own compiles it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5775C.
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
