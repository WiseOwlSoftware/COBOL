      *>--- conformance-case
      *> id: wavef_dbg_proc
      *> rules: []
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> X3.23-1985 USE FOR DEBUGGING + the DEBUG-ITEM special register at --std 85 (VCR Table 7 row 7.17).
      *> The '85 debug module was deleted by ISO/IEC 1989:2002 and is absent from ISO/IEC 1989:2023, so its
      *> authoritative behavior is the 1985 standard. WITH DEBUGGING MODE compiles the debugging section as real
      *> source (the compile-time switch); the object-time switch is ON (RunUnit.DebugMode, the CCVS posture), so
      *> the USE FOR DEBUGGING ON ALL PROCEDURES declarative runs just before each procedure-name outside a
      *> debugging section, sections and paragraphs (X3.23-1985 general rule 4, kb/Work PB5083), and
      *> populates DEBUG-ITEM (DEBUG-NAME + the DEBUG-CONTENTS transfer-cause taxonomy — DB101A witness).
      *> Hand-derived stdout (each DEBUG-CONTENTS token is DB101A-pinned):
      *>   MAIN   : the first nondeclarative procedure, a section -> "START PROGRAM" (general rule 16)
      *>   P-START: its first paragraph, entered with it -> "START PROGRAM"
      *>   P-LOOP : every PERFORM execution -> "PERFORM LOOP" (general rule 20), both iterations
      *>   P-END  : reached by GO TO -> SPACES
       IDENTIFICATION DIVISION.
       PROGRAM-ID. WAVEF-DBG-PROC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NM   PIC X(30).
       01 CONT PIC X(13).
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON ALL PROCEDURES.
       DBG-BODY.
           MOVE DEBUG-NAME     TO NM.
           MOVE DEBUG-CONTENTS TO CONT.
           DISPLAY "N=" NM "C=" CONT.
       END DECLARATIVES.
       MAIN SECTION.
       P-START.
           PERFORM P-LOOP 2 TIMES.
           GO TO P-END.
       P-LOOP.
           CONTINUE.
       P-END.
           STOP RUN.
