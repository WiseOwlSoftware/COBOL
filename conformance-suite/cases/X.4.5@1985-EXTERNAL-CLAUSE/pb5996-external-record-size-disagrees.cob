      *>--- conformance-case
      *> id: pb5996-external-record-size-disagrees
      *> rules: [1985-GR-X.4.5.4-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5996 - §1985:X.4.5.4 GR2: "Within a run unit, if two
      *>   or more programs describe the same external data record, each
      *>   record-name of the associated record description entries must
      *>   be the same and the records must define the same number of
      *>   standard data format characters" (cite.py: OK). EXTA and EXTB
      *>   are one compilation group and describe the external record
      *>   EXT-R at 10 and at 100 characters, so the run unit they form
      *>   breaks the rule, and WiseOwl refuses it at the second
      *>   description (line 40) with COBOL7735. A compiler that accepts
      *>   it shares one record of one size between two programs that
      *>   address it at two sizes. §2002:13.16.20.3 GR4 and
      *>   §2014:13.18.22.3 GR4 say "the records shall define the same
      *>   number of bytes" (cite.py: OK), so 2002 and 2014 refuse it too;
      *>   at 2023 the rule is checked at run time,
      *>   EC-EXTERNAL-FORMAT-CONFLICT (§2023:14.8.4.3), and
      *>   conformance:ec_external_format_conflict is that test. The
      *>   legal side, two layouts of one size and a complete REDEFINES,
      *>   is conformance:1985-GR-X_4_5_4-2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXT5996A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-R IS EXTERNAL PIC X(10).
       PROCEDURE DIVISION.
       P0.
           MOVE ALL "A" TO EXT-R.
           CALL "EXT5996B".
           STOP RUN.
       END PROGRAM EXT5996A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. EXT5996B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-R IS EXTERNAL PIC X(100).
       PROCEDURE DIVISION.
       P1.
           MOVE ALL "B" TO EXT-R.
           EXIT PROGRAM.
       END PROGRAM EXT5996B.
