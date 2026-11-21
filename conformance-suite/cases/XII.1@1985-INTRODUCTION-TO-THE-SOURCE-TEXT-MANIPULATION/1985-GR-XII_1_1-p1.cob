      *>--- conformance-case
      *> id: 1985-GR-XII_1_1-p1
      *> rules: [1985-GR-XII.1.1-p1, 1985-GR-XII.1.2-p1, 1985-GR-XII.1.2-p2]
      *> accept: [85]
      *> files: [S17PAR.cpy, S17TXT.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XII.1.1 p1; §1985:XII.1.2 p1; §1985:XII.1.2 p2
      *> (INTRODUCTION TO THE SOURCE TEXT MANIPULATION MODULE). An
      *> output test: the program must translate and run at 85 and
      *> DISPLAY exactly the lines its .out holds, the outcome the rule
      *> requires. Placed from written and run by the PB5453 reader of
      *> lineage-reads-1985/s-17.json by scripts/spec/place_witnesses.py
      *> (kb/Work PB4995).
      * XII.1.1-p1, XII.1.2-p1, XII.1.2-p2: COPY with no REPLACING
      * copies unchanged; COPY REPLACING replaces a literal, an
      * identifier, a word and a group of words (pseudo-text); REPLACE
      * works alone and also on text a COPY brought in.
      * Expected: UNCHANGED LIT-01 PLAIN 07 / REPL NEW-01 NEWNM 09 /
      * COPIED XYZ / ALONE DONE
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CR85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY S17TXT.
       COPY S17TXT REPLACING "LIT-01" BY "NEW-01"
                             OLDNAME BY NEWNAME
                             W-WORD BY W-OTHER
                             ==PIC 99 VALUE 7== BY ==PIC 99 VALUE 9==
                             GRPA BY GRPB.
       01  FOO-TXT PIC X(3) VALUE "ABC".
       01  BAR-TXT PIC X(3) VALUE "XYZ".
       PROCEDURE DIVISION.
       P1.
           DISPLAY "UNCHANGED " OLDNAME " " W-WORD " " GRPA.
           MOVE "NEWNM" TO W-OTHER.
           DISPLAY "REPL " NEWNAME " " W-OTHER " " GRPB.
           REPLACE ==FOO-TXT== BY ==BAR-TXT==
                   ==ALONE-WORD== BY =="ALONE DONE"==.
           COPY S17PAR.
           DISPLAY ALONE-WORD.
           STOP RUN.
