      *>--- conformance-case
      *> id: pb5362-external-value-of-same-value
      *> rules: [1985-GR-VII.3.9.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5362. §1985:VII.3.9.4 GR2: "all VALUE OF clauses in the run
      *> unit which are associated with that file connector must be
      *> consistent. The implementor will specify the consistency rules"
      *> (docs/CONFORMANCE.md section 3.7: every implementor-name both entries
      *> write is given the same literal-1 VALUE). GR1 makes implementor-name-1
      *> "equal to literal-1", so the rule compares values, never spellings:
      *> 7 and 007.0 are one numeric value, and SPACE and SPACES one
      *> figurative constant ("The singular and plural forms of figurative
      *> constants are equivalent", §1985:IV.4.2.2.2.3). FILE-ID is "A" in
      *> both: a COBOL-85 nonnumeric literal is delimited "at the beginning
      *> and at the end by the separator quotation mark" (§1985:IV.4.2.2.2.1),
      *> so 'A' is refused at --std 85 (kb/Work PB5160) and an alphanumeric
      *> value has one spelling in the only edition with VALUE OF.
      *> The two entries are consistent, so the run unit compiles and the
      *> called program reads the record the main program wrote.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. MP5362V.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5362v.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           VALUE OF FILE-ID "A" RETENTION 7 OWNER SPACE.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           OPEN OUTPUT SF
           MOVE "ABCD" TO SF-REC
           WRITE SF-REC
           CLOSE SF
           OPEN INPUT SF
           CALL "P5362VS"
           CLOSE SF
           DISPLAY "DONE"
           STOP RUN.
       END PROGRAM MP5362V.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5362VS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SF ASSIGN TO "p5362v.dat".
       DATA DIVISION.
       FILE SECTION.
       FD  SF IS EXTERNAL
           VALUE OF FILE-ID "A" RETENTION 007.0 OWNER SPACES.
       01  SF-REC PIC X(4).
       PROCEDURE DIVISION.
       P.
           READ SF AT END DISPLAY "EOF"
               NOT AT END DISPLAY "READ " SF-REC
           END-READ
           EXIT PROGRAM.
       END PROGRAM P5362VS.
