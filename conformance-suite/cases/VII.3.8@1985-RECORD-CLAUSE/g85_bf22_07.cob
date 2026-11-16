      *>--- conformance-case
      *> id: g85_bf22_07
      *> rules: [1985-SR-VII.3.8.3-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.3.8.3 SR1 - Format 1: no record description larger
      *> than integer-1; records of integer-1 or fewer positions are
      *> accepted. RULE (cite.py OK): "No record description entry for
      *> the file may specify a number of character positions greater
      *> than integer-1." FD SF has RECORD CONTAINS 10 CHARACTERS and two
      *> record descriptions, SF-LONG of 10 positions (equal: the
      *> boundary) and SF-SHORT of 6 (fewer). The program must translate
      *> at 85. DERIVATION: §1985:VII.3.8.4 GR3 (cite.py OK) "Integer-1
      *> specifies the number of character positions contained in each
      *> record in the file." SF-LONG ABCDEFGHIJ is written and read
      *> back: L=ABCDEFGHIJ. §1985:VI.5.3.4 GR3 (cite.py OK): the 01
      *> entries under one FD "represent implicit redefinitions of the
      *> same area", so SF-SHORT is its first 6 positions: S=ABCDEF.
      *> The next READ is AT END: EOF.
      *> A leg fails if the smaller or the equal record is refused, or
      *> the record is not 10 positions.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2207.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "G85BF227.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SF
           RECORD CONTAINS 10 CHARACTERS.
       01  SF-LONG PIC X(10).
       01  SF-SHORT.
           02  SF-S1 PIC X(2).
           02  SF-S2 PIC X(4).
       PROCEDURE DIVISION.
       P1.
           OPEN OUTPUT SF.
           MOVE "ABCDEFGHIJ" TO SF-LONG.
           WRITE SF-LONG.
           CLOSE SF.
           OPEN INPUT SF.
           READ SF AT END DISPLAY "EOF1".
           DISPLAY "L=" SF-LONG.
           DISPLAY "S=" SF-SHORT.
           READ SF AT END DISPLAY "EOF".
           CLOSE SF.
           STOP RUN.
