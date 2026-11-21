      *>--- conformance-case
      *> id: pb5979-start-key-qualified-operand-printed-as-written-85
      *> rules: [1985-SR-IX.4.7.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5979. A refusal at 85. §1985:IX.4.7.3 SR4 (cite.py:
      *> OK): the START operand must be the record key or an item that
      *> begins at the leftmost character position of one. OTHER-PART
      *> OF IREC begins at position 5, after the 4-character key, so
      *> the START is refused. The refusal names the operand as the
      *> programmer wrote it, 'OTHER-PART OF IREC' with its words
      *> apart, never the run-together 'OTHER-PARTOFIREC' that a parse
      *> node's token text prints. A compiler whose refusal prints
      *> the glued text, or that accepts the START, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5979C.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "p5979c.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KY OF IREC.
       DATA DIVISION.
       FILE SECTION.
       FD IF1.
       01 IREC.
          05 KY PIC X(4).
          05 OTHER-PART PIC X(4).
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       P-1.
           OPEN INPUT IF1
           START IF1 KEY IS EQUAL TO OTHER-PART OF IREC
               INVALID KEY CONTINUE
           END-START
           STOP RUN.
