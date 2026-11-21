      *>--- conformance-case
      *> id: pb5993-access-random-sequential-file-cites-1985-85
      *> rules: [1985-GR-VII.2.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5993. A refusal at 85. §1985:VII.2.6.3 GR3 (cite.py:
      *> OK): "When the ORGANIZATION clause is not specified, sequential
      *> organization is implied." F writes no ORGANIZATION clause, so
      *> it is a sequential file, whose ACCESS MODE clause prints
      *> SEQUENTIAL alone (§1985:VII.2.4.2), and ACCESS MODE IS RANDOM
      *> is refused (COBOL1858). The refusal says the file is
      *> sequential by default and cites §1985:VII.2.6.3 GR3, never
      *> "record sequential" (a COBOL-2002 phrase) or §2023:12.4.5.10.3
      *> GR6. A compiler that accepts the clause, or a refusal citing
      *> another edition's clause, fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5993A.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "p5993a.dat" ACCESS MODE IS RANDOM.
       DATA DIVISION.
       FILE SECTION.
       FD F.
       01 R PIC X.
       PROCEDURE DIVISION.
       P-1.
           STOP RUN.
