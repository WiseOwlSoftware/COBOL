      *>--- conformance-case
      *> id: 1985-SR-VI_6_17_3-4
      *> rules: [1985-SR-VI.6.17.3-4]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.17.3 SR4 (THE INITIALIZE STATEMENT): "The
      *> description of the data item referenced by identifier-1 or any
      *> items subordinate to identifier-1 may not contain the
      *> DEPENDING phrase of the OCCURS clause." G's subordinate E is
      *> described OCCURS 1 TO 5 TIMES DEPENDING ON N, so INITIALIZE G
      *> breaks the rule; so does INITIALIZE E (1), whose own
      *> description holds the phrase. INITIALIZE W-X is legal. A
      *> refusal at 85 (the 2002 syntax rules drop SR4): a compiler
      *> that accepts either statement fails (kb/Work PB5088).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N5088OD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 9 VALUE 3.
       01  W-X PIC X(3).
       01  G.
           05  E PIC X OCCURS 1 TO 5 TIMES DEPENDING ON N.
       PROCEDURE DIVISION.
       P1.
           INITIALIZE W-X.
           INITIALIZE G.
           INITIALIZE E (1).
           STOP RUN.
