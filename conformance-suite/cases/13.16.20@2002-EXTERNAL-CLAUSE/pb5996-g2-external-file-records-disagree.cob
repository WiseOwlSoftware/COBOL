      *>--- conformance-case
      *> id: pb5996-g2-external-file-records-disagree
      *> rules: [2002-GR-13.16.20.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5996 - the records of an EXTERNAL file description
      *>   are external data records: §2002:13.16.20.3 GR2b "the data
      *>   contained in all record description entries subordinate to
      *>   that file description entry is external", subject to GR4,
      *>   whose "the records shall define the same number of bytes"
      *>   (cite.py: OK on both) binds every source element that
      *>   describes them; their externalized name is the file's (GR3).
      *>   FDA5996 and FDB5996 are one compilation group and describe the
      *>   records of the external file XF at 10 and at 100 bytes, so
      *>   WiseOwl refuses the second (line 44) with COBOL7735.
      *>   §2014:13.18.22.3 GR2b and GR4 are the same text. At 85 the same
      *>   program is refused by the file description's own rule,
      *>   §1985:VII.3.8.4 GR2 (COBOL6847).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDA5996.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "XF5996.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XF-R PIC X(10).
       PROCEDURE DIVISION.
       P0.
           CALL "FDB5996".
           STOP RUN.
       END PROGRAM FDA5996.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. FDB5996.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "XF5996.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  XF IS EXTERNAL.
       01  XF-S PIC X(100).
       PROCEDURE DIVISION.
       P1.
           EXIT PROGRAM.
       END PROGRAM FDB5996.
