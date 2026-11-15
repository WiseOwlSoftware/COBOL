      *>--- conformance-case
      *> id: pb5486-rewrite-from-function-cites-1985
      *> rules: [1985-SR-VIII.4.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5486: 1985 states the FROM function rule.
      *> §1985:VIII.4.6.3 SR1 (as amended by X3.23a-1989): "If
      *> identifier-1 is a function-identifier, it must reference an
      *> alphanumeric function." The REWRITE of a relative file FROM
      *> the integer function LENGTH is refused COBOL1914 at 85,
      *> citing the relative module's rule, not the 2023 SR9 wording.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5486R1.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "PB5486R1.DAT"
               ORGANIZATION IS RELATIVE
               ACCESS MODE IS RANDOM
               RELATIVE KEY IS RK.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1 PIC X(4).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       P1.
           OPEN I-O F1.
           READ F1 INVALID KEY CONTINUE END-READ.
           REWRITE R1 FROM FUNCTION LENGTH("AB")
               INVALID KEY CONTINUE
           END-REWRITE.
           CLOSE F1.
           STOP RUN.
