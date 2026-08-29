      *> reject-at: 2023
      *> ISO 14.9.7.3 SR1: COMMIT shall not be specified in a RECURSIVE source element (kb/Work PB137 -
      *> the old payload-free BoundNop made the SR unenforceable).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB137R RECURSIVE.
       PROCEDURE DIVISION.
       MAIN.
           COMMIT
           GOBACK.
       END PROGRAM PB137R.
