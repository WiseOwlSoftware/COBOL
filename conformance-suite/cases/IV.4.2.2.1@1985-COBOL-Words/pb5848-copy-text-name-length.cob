      *>--- conformance-case
      *> id: pb5848-copy-text-name-length
      *> rules: [1985-SR-IV.4.2.2.1-2]
      *> files: [LENGTH.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.2.2.1 SR1 (cite.py --check: OK): "Reserved words
      *> form disjoint sets with user-defined words"; a text-name is a
      *> user-defined word (§1985:IV.4.2.2.1.1, cite.py --check: OK),
      *> so a text-name spelled like the reserved word LENGTH is
      *> refused with COBOL0901 (kb/Work PB5848). SR2 (cite.py --check:
      *> OK) adds that LENGTH, RANDOM and SUM may not be used as a
      *> system-name or as a user-defined word regardless of context.
      *> The control copies a text-name that is no reserved word and
      *> prints TXT.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5848L85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY LENGTH.
       PROCEDURE DIVISION.
       P0.
           DISPLAY TXT.
           STOP RUN.
