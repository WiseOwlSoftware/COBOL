      *>--- conformance-case
      *> id: pb5924-section-header-column-10-85
      *> rules: [1985-SR-IV.7.3.2-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.7.3.2 (cite.py: OK):
      *> "The section header must start in area A"
      *> Section headers beginning in column 10 (area A) compile and run.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SECL.
       DATA DIVISION.
         WORKING-STORAGE SECTION.
       01 X PIC X(4) VALUE "SECL".
       PROCEDURE DIVISION.
         S1 SECTION.
       P1.
           DISPLAY X.
           STOP RUN.
