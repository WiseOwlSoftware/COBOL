      *>--- conformance-case
      *> id: pb3370_computer_paragraph_second_period
      *> rules: [FMT-12.3.5.2, SR-12.3.5.3-1, FMT-12.3.6.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3370 - ISO 12.3.5.3 SR1: "If computer-name-1 is not specified, the second period in
      *> the General format may be omitted."  The grammar now superset-parses that second period in
      *> both computer paragraphs (so the optional computer-name's prediction commits to the paragraph
      *> and a reserved word after it is named in the paragraph's own format) and ClosedFormatPass
      *> requires it back wherever a name or a clause is written.  This LEGAL program uses both
      *> spellings: a SOURCE-COMPUTER paragraph with no name and no second period, then an
      *> OBJECT-COMPUTER paragraph with a name, a clause and its period.  The clause must still take
      *> effect: under the descending alphabet AL-REV "A" is HIGHER than "Z" (the PROGRAM COLLATING
      *> SEQUENCE governs alphanumeric comparison), so the IF prints REVERSED.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3370PS.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER.
       OBJECT-COMPUTER. GENERIC-BOX
           PROGRAM COLLATING SEQUENCE IS AL-REV.
       SPECIAL-NAMES.
           ALPHABET AL-REV IS "Z" THRU "A".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-A PIC X VALUE "A".
       01 W-Z PIC X VALUE "Z".
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF W-A > W-Z
               DISPLAY "P3370PS REVERSED"
           ELSE
               DISPLAY "P3370PS NATIVE"
           END-IF.
           STOP RUN.
