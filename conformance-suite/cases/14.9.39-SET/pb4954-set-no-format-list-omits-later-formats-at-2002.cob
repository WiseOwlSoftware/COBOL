      *>--- conformance-case
      *> id: pb4954-set-no-format-list-omits-later-formats-at-2002
      *> rules: [SR-14.9.39.3-23]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4954. At --std 2002 the SET UP/DOWN BY formats are
      *> Format 2 (index-arithmetic) and Format 9 (data-pointer-arithmetic,
      *> Format 10 of the 2023 numbering), both printed by §2002:14.8.35.1
      *> (checked with cite.py --check). The dynamic-capacity-table format
      *> (2023 Format 14) is a 2014 feature. WS-N is an integer data item,
      *> so no format admits it, and the refusal's list names Format 10 and
      *> Format 2 but not Format 14.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4954A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-N PIC 9(4) VALUE 1.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET WS-N UP BY 1
           STOP RUN.
