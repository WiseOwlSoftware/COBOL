      *>--- conformance-case
      *> id: pb5083-debug-section-subject
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5083. X3.23-1985 USE FOR DEBUGGING general rule 3 a: the
      *> section for procedure-name-1 runs "Immediately before each
      *> execution of the named procedure". A SECTION is executed when
      *> control reaches it, so the subject WORK-SECT is ONE trigger, named
      *> WORK-SECT (general rule 13), and control entering the section at its
      *> second paragraph does not execute the section: not by a GO TO
      *> P2 OF WORK-SECT either, general rule 9: "A reference to ...
      *> procedure-name-1 as a qualifier does not constitute reference to
      *> that item for the debugging described in the general rules above".
      *> General rule 16: the first execution of the first nondeclarative
      *> procedure, MAIN-SECT here, is DEBUG-CONTENTS 'START PROGRAM' with
      *> DEBUG-NAME the procedure's own name. General rule 20: the PERFORM
      *> of WORK-SECT is 'PERFORM LOOP'; general rule 22: control running
      *> from PRE-SECT into WORK-SECT is 'FALL THROUGH'.
      *> Expected output, in order: START PROGRAM for MAIN-SECT; M1, W2
      *> (entered mid-section: no debugging section); M2; the PERFORM
      *> trigger; W1, W2 (second visit); M2B, PR1; the fall-through trigger;
      *> W1, W2; END. Why it can fail: a section subject read as each of its
      *> paragraphs runs the section for W1 and W2 and names them (the
      *> pre-fix output), one that fires on any entry to the section's pcs
      *> fires on the qualified GO TO.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5083SECT.
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
           USE FOR DEBUGGING ON MAIN-SECT WORK-SECT.
       DBG-P.
           MOVE DEBUG-NAME TO NM.
           MOVE DEBUG-CONTENTS TO CT.
           DISPLAY "DEBUG [" NM "] [" CT "]".
       END DECLARATIVES.
       MAIN-SECT SECTION.
       M1.
           DISPLAY "M1".
           GO TO W2 OF WORK-SECT.
       M2.
           DISPLAY "M2".
           PERFORM WORK-SECT.
           DISPLAY "M2B".
           GO TO PRE-SECT.
       PRE-SECT SECTION.
       PR1.
           DISPLAY "PR1".
       WORK-SECT SECTION.
       W1.
           DISPLAY "W1".
       W2.
           DISPLAY "W2".
           ADD 1 TO CNT.
           IF CNT = 1 GO TO M2.
       END-SECT SECTION.
       E1.
           DISPLAY "END".
           STOP RUN.
