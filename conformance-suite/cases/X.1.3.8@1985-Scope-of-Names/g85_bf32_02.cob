      *>--- conformance-case
      *> id: g85_bf32_02
      *> rules: [1985-GR-X.1.3.8.2-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:X.1.3.8.2 GR2 - when the set of names (GR1: the names
      *> defined in program B plus the global names of program A) and
      *> qualification identify only one object, it is the referenced
      *> object. F is defined in both programs: F OF GA identifies only
      *> A's global F, F OF GB only B's F. X is defined in both, but
      *> A's X is not global, so only B's X is in the set. GZ is defined
      *> only in A, globally. Line B fails if any reference resolves to
      *> the wrong object; line A fails if the stores B made through
      *> F OF GA and GZ did not reach A's items, or the store to B's X
      *> reached A's X.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF3202.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GA GLOBAL.
           05  F   PIC X(4) VALUE "AAAA".
       01  X   PIC X(4) VALUE "AX".
       01  GZ  PIC X(4) VALUE "GZGZ" GLOBAL.
       PROCEDURE DIVISION.
       P1.
           CALL "BF32N02".
           DISPLAY "A F=" F OF GA " X=" X " GZ=" GZ.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. BF32N02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GB.
           05  F   PIC X(4) VALUE "BBBB".
       01  X   PIC X(4) VALUE "BX".
       PROCEDURE DIVISION.
       N1.
           DISPLAY "B F-GA=" F OF GA " F-GB=" F OF GB " X=" X
               " GZ=" GZ.
           MOVE "ZZZZ" TO F OF GA.
           MOVE "QQ" TO X.
           MOVE "NEWZ" TO GZ.
           EXIT PROGRAM.
       END PROGRAM BF32N02.
       END PROGRAM G85BF3202.
