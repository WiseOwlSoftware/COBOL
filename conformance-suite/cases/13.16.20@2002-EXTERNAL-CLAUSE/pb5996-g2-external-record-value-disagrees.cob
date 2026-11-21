      *>--- conformance-case
      *> id: pb5996-g2-external-record-value-disagrees
      *> rules: [2002-GR-13.16.20.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5996 - §2002:13.16.20.3 GR4: "the VALUE clause
      *>   specification, if any, for each record name of the associated
      *>   record description entries shall be identical" (cite.py: OK).
      *>   VLA5996 and VLB5996 are one compilation group and describe the
      *>   external record EXT-V at one size with VALUE "ABCD" and VALUE
      *>   "abcd": a literal's characters are its content, so the two
      *>   specifications are not identical and WiseOwl refuses the
      *>   second (line 33) with COBOL7735. A compiler that accepts it
      *>   gives one record two initial values. §2014:13.18.22.3 GR4 is
      *>   the same text (cite.py: OK). At 2023 the rule is checked at run
      *>   time (§2023:14.8.4.3), conformance:pb1236_external_value_
      *>   literal_case_is_significant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VLA5996.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-V PIC X(4) IS EXTERNAL VALUE "ABCD".
       PROCEDURE DIVISION.
       P0.
           CALL "VLB5996".
           STOP RUN.
       END PROGRAM VLA5996.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. VLB5996.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  EXT-V PIC X(4) IS EXTERNAL VALUE "abcd".
       PROCEDURE DIVISION.
       P1.
           EXIT PROGRAM.
       END PROGRAM VLB5996.
