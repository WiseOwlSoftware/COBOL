      *>--- conformance-case
      *> id: pb5798-if-next-sentence-with-end-if-85
      *> rules: [1985-SR-VI.6.16.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5798. §1985:VI.6.16.3 SR3: "If the END-IF phrase is
      *> specified, the NEXT SENTENCE phrase must not be specified." Both
      *> arms are asked: the first IF has NEXT SENTENCE in its THEN arm,
      *> the second in its ELSE arm, and each is terminated by END-IF, so
      *> each is refused (COBOL7486). The third IF, with NEXT SENTENCE and
      *> no END-IF, is the legal form and draws nothing.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5798IF85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A PIC 9 VALUE 1.
       PROCEDURE DIVISION.
       P1.
           IF A = 1 NEXT SENTENCE END-IF
           DISPLAY "SKIPPED".
           IF A = 2 DISPLAY "TWO" ELSE NEXT SENTENCE END-IF
           DISPLAY "SKIPPED".
           IF A = 1 NEXT SENTENCE ELSE DISPLAY "NOT ONE".
           DISPLAY "NEXT".
           STOP RUN.
