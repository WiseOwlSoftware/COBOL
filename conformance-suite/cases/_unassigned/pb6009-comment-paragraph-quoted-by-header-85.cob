      *>--- conformance-case
      *> id: pb6009-comment-paragraph-quoted-by-header-85
      *> rules: []
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6009 - §1985:VI.3.2 prints AUTHOR before DATE-WRITTEN; the refusal
      *> quotes each paragraph by its header as written ('AUTHOR.'), never with the period
      *> the reference-format conversion writes for the discarded comment-entry ('AUTHOR. .').
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6009Q.
       DATE-WRITTEN. 1985-01-01.
       AUTHOR. A WRITER.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
