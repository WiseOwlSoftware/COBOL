      *>--- conformance-case
      *> id: pb5901-using-by-reference-at-85
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85 (kb/Work PB5901). The 1985 Procedure
      *> Division header has one form, PROCEDURE DIVISION [USING
      *> {data-name-1} ...] (§1985:X.5.1, "The Procedure Division is
      *> identified by, and must begin with, the following header",
      *> cite.py --check OK; the format read on the rendered page),
      *> with no BY phrase. The using-phrase's [BY REFERENCE] is
      *> printed from 2002 (§2023:14.2.1, "where using-phrase is",
      *> cite.py --check OK). This program writes BY REFERENCE in its
      *> header, so WiseOwl refuses it at 85 with COBOL0900; it
      *> compiles from 2002 (pb5901_using_by_reference).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5901M.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ITEM PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           CALL "PB5901S" USING W-ITEM.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5901S.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X(3).
       PROCEDURE DIVISION USING BY REFERENCE L1.
       P1.
           EXIT PROGRAM.
       END PROGRAM PB5901S.
       END PROGRAM PB5901M.
