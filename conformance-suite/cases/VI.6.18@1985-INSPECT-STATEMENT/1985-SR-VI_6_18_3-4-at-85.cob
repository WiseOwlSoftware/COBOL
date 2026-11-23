      *>--- conformance-case
      *> id: 1985-SR-VI_6_18_3-4-at-85
      *> rules: [1985-SR-VI.6.18.3-4]
      *> accept: [85]
      *> source-format: fixed
      *>---
      *> §1985:VI.6.18.3 SR4: "No more than one BEFORE phrase and one
      *> AFTER phrase can be specified for any one ALL, LEADING,
      *> CHARACTERS, FIRST, or CONVERTING phrase." The legal form is
      *> the most the rule allows: one AFTER and one BEFORE phrase on
      *> one ALL phrase. The region is the text after the first "X"
      *> and before the first "Z" of "AAXAAZAA", positions 4 and 5,
      *> so the tally is 2; the leg fails if the compiler refuses the
      *> two phrases or counts the "A" outside the region.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3178SR6184.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  S           PIC X(8) VALUE "AAXAAZAA".
       01  CNT         PIC 99 VALUE 0.
       01  CNT-E       PIC Z9.
       PROCEDURE DIVISION.
       MAIN-PARA.
           INSPECT S TALLYING CNT FOR ALL "A"
               AFTER INITIAL "X" BEFORE INITIAL "Z".
           MOVE CNT TO CNT-E.
           DISPLAY "TALLY " CNT-E.
           STOP RUN.
