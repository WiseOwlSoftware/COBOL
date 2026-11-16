      *>--- conformance-case
      *> id: pb5848-sr1-copy-text-name-add
      *> rules: [SR-8.3.2.1-1]
      *> files: [ADD.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2023:8.3.2.4.1 (cite.py --check: OK): "Reserved words shall
      *> not be used as system-names or user-defined words"; a text-
      *> name is a system-name (§2023:8.3.2.3.1), so a text-name
      *> spelled like the reserved word ADD is refused with COBOL0901
      *> (kb/Work PB5848). The control copies a text-name that is no
      *> reserved word and prints TXT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5848A2023.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY ADD.
       PROCEDURE DIVISION.
       P0.
           DISPLAY TXT.
           STOP RUN.
