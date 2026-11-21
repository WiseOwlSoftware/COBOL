      *>--- conformance-case
      *> id: pb5924-logop-space-required-85
      *> rules: [1985-SR-VI.6.3.2-p3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:VI.6.3.2 (cite.py: OK): "The logical operators must be preceded by a space and followed by a space"
      *> A logical operator written without a space before or after it ("(A = 1)AND(B = 2)")
      *> is refused at 85. Refused COBOL7672 since PB5989.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. LOGOPNS.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  A PIC 9 VALUE 1.
       01  B PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       P1.
           IF (A = 1)AND(B = 2)
               DISPLAY "BOTH"
           END-IF.
           STOP RUN.
