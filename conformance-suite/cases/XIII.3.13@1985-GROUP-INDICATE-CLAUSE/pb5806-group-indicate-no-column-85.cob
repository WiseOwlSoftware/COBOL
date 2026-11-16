      *>--- conformance-case
      *> id: pb5806-group-indicate-no-column-85
      *> rules: [1985-SR-XIII.3.13.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5806. §1985:XIII.3.13.3 SR1: "The GROUP INDICATE clause
      *> must only be specified in a detail report group entry that defines
      *> a printable item", and §1985:III.2 defines the printable item as an
      *> elementary report entry that "contains a COLUMN NUMBER clause, a
      *> PICTURE clause, and a SOURCE, SUM, or VALUE clause". The entry
      *> PIC ZZ9 SOURCE WB GROUP INDICATE has no COLUMN clause: refused at
      *> this edition as at the later ones. Table 1 (§1985:XIII.3.9.3 SR11)
      *> lists the combination as permissible, which is necessary and not
      *> sufficient: every syntax rule binds the program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5806A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RPF ASSIGN TO "pb5806a.rpt".
       DATA DIVISION.
       FILE SECTION.
       FD RPF REPORT IS R1.
       WORKING-STORAGE SECTION.
       01 WA PIC X(3) VALUE "AAA".
       01 WB PIC 999 VALUE 7.
       REPORT SECTION.
       RD R1.
       01 D1 TYPE IS DETAIL.
           03 LINE NUMBER IS PLUS 1.
               05 COLUMN NUMBER IS 2 PIC X(3) SOURCE WA.
               05 PIC ZZ9 SOURCE WB GROUP INDICATE.
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT RPF.
           INITIATE R1.
           GENERATE D1.
           TERMINATE R1.
           CLOSE RPF.
           STOP RUN.
