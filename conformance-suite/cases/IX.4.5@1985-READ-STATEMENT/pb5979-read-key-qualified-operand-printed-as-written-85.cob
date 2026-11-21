      *>--- conformance-case
      *> id: pb5979-read-key-qualified-operand-printed-as-written-85
      *> rules: [1985-SR-IX.4.5.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5979. A refusal at 85. §1985:IX.4.5.3 SR2 (cite.py:
      *> OK): "Data-name-1 must be the name of a data item specified as
      *> a record key associated with file-name-1" (the READ KEY IS
      *> phrase). KA OF IREC1 is the record key of IF1, not of IF2, so
      *> READ IF2 KEY IS KA OF IREC1 is refused. The refusal names the
      *> operand as the programmer wrote it, 'KA OF IREC1' with its
      *> words apart, never the run-together 'KAOFIREC1' that a parse
      *> node's token text prints. A compiler whose refusal prints
      *> the glued text, or that accepts the READ, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5979D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IF1 ASSIGN TO "p5979d1.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KA OF IREC1.
           SELECT IF2 ASSIGN TO "p5979d2.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS KB OF IREC2.
       DATA DIVISION.
       FILE SECTION.
       FD IF1.
       01 IREC1.
          05 KA PIC X(4).
       FD IF2.
       01 IREC2.
          05 KB PIC X(4).
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       P-1.
           OPEN INPUT IF2
           READ IF2 KEY IS KA OF IREC1 INVALID KEY CONTINUE
           END-READ
           STOP RUN.
