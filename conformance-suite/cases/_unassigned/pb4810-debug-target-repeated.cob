      *>--- conformance-case
      *> id: pb4810-debug-target-repeated
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4810. X3.23-1985 USE FOR DEBUGGING syntax rule 6: "Any
      *> given identifier, cd-name, file-name, or procedure-name may appear in
      *> only one USE FOR DEBUGGING statement and may appear only once in that
      *> statement." T is named twice in one statement (once alone, once by
      *> ALL REFERENCES OF), so the program is refused (COBOL6335). Why it
      *> can fail: a binder that keeps both targets runs two sections for one
      *> reference.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4810RP.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T PIC 9.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG SECTION.
           USE FOR DEBUGGING ON T ALL REFERENCES OF T.
       DBG-BODY.
           DISPLAY DEBUG-NAME.
       END DECLARATIVES.
       MAIN SECTION.
       P1.
           MOVE 1 TO T.
           STOP RUN.
