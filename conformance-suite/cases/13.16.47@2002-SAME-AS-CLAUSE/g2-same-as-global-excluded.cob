      *>--- conformance-case
      *> id: g2-same-as-global-excluded
      *> rules: [2002-GR-13.16.47.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.47.3 GR1 - the SAME AS description excludes "the
      *> EXTERNAL, GLOBAL, REDEFINES, and SELECT WHEN clauses specified for
      *> data-name-1". S3 SAME AS GLB1 is therefore not GLOBAL, so the
      *> nested program G2SAX2N, which declares no S3, has no S3 in scope
      *> and its DISPLAY S3 is refused at that line. The companion of the
      *> accepted g2-same-as-excluded-clauses, where the nested program
      *> reads GLB1 itself (the GLOBAL control). Fails if S3 resolves in the
      *> nested program.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAX02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GLB1 PIC X(4) GLOBAL VALUE "GLOB".
       01  S3 SAME AS GLB1.
       PROCEDURE DIVISION.
           CALL "G2SAX2N"
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SAX2N.
       PROCEDURE DIVISION.
           DISPLAY GLB1 S3
           GOBACK.
       END PROGRAM G2SAX2N.
       END PROGRAM G2SAX02.
