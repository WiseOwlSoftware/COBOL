      *>--- conformance-case
      *> id: pb5007-content-validation-below-2002
      *> rules: [FMT-13.18.63.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5007 - the VALUE clause's format 5 (ISO 13.18.63.2,
      *> the content-validation-entry, "[ IS | ARE ] { INVALID | VALID }
      *> [ WHEN condition-1 ]") belongs to the VALIDATE facility, a
      *> COBOL-2002 introduction: 8.9 reserves VALID from 2002, and
      *> COBOL-85 has no such phrase. At --std 85 the level-88 entry is
      *> therefore refused BY NAME, the edition diagnostic COBOL0900
      *> naming its introduction, with no COBOL1708 decline beside it.
      *> It drew a generic COBOL1941 at IS ("'IS' is not a clause of
      *> the data description entry") when the phrase's grammar arm
      *> asked the edition. INVALID is reserved at every edition, so it
      *> has no other reading here; a COBOL-85 symbolic-character named
      *> VALID keeps its VALUE-operand reading (the reservation gate).
      *> The refusal at 2002 and later is
      *> wiseowl/cases/13.18.63-VALUE/declined-validate-value-format5.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5007VL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC X.
          88 WS-A-OK VALUE "Y" IS INVALID.
       PROCEDURE DIVISION.
       MAIN-PARA.
           STOP RUN.
