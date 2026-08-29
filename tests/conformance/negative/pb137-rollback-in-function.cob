      *> reject-at: 2023
      *> ISO 14.9.36.3 SR1 via 8.6.6: a FUNCTION is ALWAYS recursive, so ROLLBACK inside one is the same
      *> recursive-source-element ban (kb/Work PB137).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. PB137F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-R PIC 9(4).
       PROCEDURE DIVISION RETURNING L-R.
       P.
           ROLLBACK
           MOVE 1 TO L-R
           GOBACK.
       END FUNCTION PB137F.
