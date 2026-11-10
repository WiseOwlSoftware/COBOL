      *>--- conformance-case
      *> id: pb5083-debug-section-subject-removed-2002
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5083. The USE FOR DEBUGGING declarative, a section-name
      *> subject included, is in X3.23-1985's Debug module only; ISO/IEC
      *> 1989:2002 deleted the facility, so the program is refused at the
      *> later editions (COBOL0902) and accepted at --std 85 (the
      *> pb5083-debug-section-subject witness). Why it can fail: an edition
      *> gate that covers the paragraph-name subject only admits a
      *> section-name subject silently above 1985.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5083SECR.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DB1 SECTION.
           USE FOR DEBUGGING ON WORK-SECT.
       DB1-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           PERFORM WORK-SECT.
           STOP RUN.
       WORK-SECT SECTION.
       W1.
           CONTINUE.
