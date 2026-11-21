      *>--- conformance-case
      *> id: pb5136-debug-empty-section-subject-85
      *> rules: [1985-GR-XV.3.2.4-3, 1985-GR-XV.3.2.4-18, 1985-GR-XV.3.2.4-20, 1985-GR-XV.3.2.4-22]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5136. A section may hold no paragraph: "A section
      *> consists of a section header followed by zero, one, or more
      *> successive paragraphs" (§1985:IV.6.4.1.2, cite.py: OK). The
      *> debugging section of EMPTYS runs "Immediately before each
      *> execution of the named procedure" (§1985:XV.3.2.4 GR3a, OK),
      *> whatever the procedure holds, so each transfer of control into
      *> the empty section fires it: the PERFORM ('PERFORM LOOP', GR20),
      *> the fall-through from MAIN ('FALL THROUGH', GR22) and the GO TO
      *> (GR18 names no DEBUG-CONTENTS, so it "remains spaces", GR11).
      *> Control then runs on through EMPTYS into NEXT1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5136E.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NM   PIC X(30).
       01  CT   PIC X(13).
       01  CNT  PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON EMPTYS.
       DBG-P.
           MOVE DEBUG-NAME TO NM.
           MOVE DEBUG-CONTENTS TO CT.
           DISPLAY "DEBUG [" NM "] [" CT "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           DISPLAY "PERFORM".
           PERFORM EMPTYS.
           DISPLAY "FALL".
       EMPTYS SECTION.
       NEXT1 SECTION.
       N1.
           ADD 1 TO CNT.
           IF CNT = 1
               DISPLAY "GOTO"
               GO TO EMPTYS.
           DISPLAY "END".
           STOP RUN.
