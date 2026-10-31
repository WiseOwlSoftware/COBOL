*>--- conformance-case
*> id: pb4156-extended-letter-beside-euro-sign
*> rules: [GR-8.1.3.2-4]
*> source-format: free
*> portable: unverified
*>---
      *> ISO 8.3.2.1 / 8.1.3.2 GR4 / Annex B (kb/Work PB4156): the word CAFÉ€ holds TWO characters outside COBOL-85's
      *> word set. The É is an extended letter (Annex B holds it), which COBOL-85 lacks: the 2002 introduction gate
      *> (COBOL0900) names it. The euro sign is in no edition's Annex B, so it is no extended letter: the
      *> word-character rule (COBOL2773) names it at every target. The decision is per CHARACTER, so the euro sign
      *> does not hide the É from the gate at COBOL-85 (the gate was once skipped for the whole word).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4156N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 CAFÉ€ PIC X(5) VALUE "hello".
       PROCEDURE DIVISION.
           DISPLAY CAFÉ€.
           STOP RUN.
