      *>--- conformance-case
      *> id: pb5901-using-reference-without-by-at-85
      *> rules: [FMT-14.2.1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> A refusal at 85 (kb/Work PB5901): the same header as
      *> pb5901-using-by-reference-at-85 with the optional word BY
      *> left out (§2023:14.2.1 underlines REFERENCE, not BY). The
      *> 1985 header, PROCEDURE DIVISION [USING {data-name-1} ...]
      *> (§1985:X.5.1, "The Procedure Division is identified by, and
      *> must begin with, the following header", cite.py --check OK),
      *> has no REFERENCE phrase either, so WiseOwl refuses it at 85
      *> with COBOL0900.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5901N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-ITEM PIC X(3) VALUE "ABC".
       PROCEDURE DIVISION.
       P0.
           CALL "PB5901T" USING W-ITEM.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5901T.
       DATA DIVISION.
       LINKAGE SECTION.
       01  L1 PIC X(3).
       PROCEDURE DIVISION USING REFERENCE L1.
       P1.
           EXIT PROGRAM.
       END PROGRAM PB5901T.
       END PROGRAM PB5901N.
