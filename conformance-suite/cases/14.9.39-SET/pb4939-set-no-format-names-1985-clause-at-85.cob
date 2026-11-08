      *>--- conformance-case
      *> id: pb4939-set-no-format-names-1985-clause-at-85
      *> rules: [SR-14.9.39.3-23]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4939. At --std 85 the SET statement's governing clause is
      *> §1985:VI.6.23.2 (checked with cite.py --check), which prints Format 1
      *> (index assignment) and Format 2 (index arithmetic) as the formats
      *> that take receivers; Format 2 is SET {index-name-3} ... UP/DOWN BY
      *> and §1985:VI.6.23.4 GR4 describes it. WS-N is an integer data item,
      *> not an index-name, so no 1985 format admits it. The refusal names
      *> that clause and its formats, not the 2023 Format 14 and Format 10
      *> of §2023:14.9.39.2 that edition lacks.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4939A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-N UP BY 1
           STOP RUN.
