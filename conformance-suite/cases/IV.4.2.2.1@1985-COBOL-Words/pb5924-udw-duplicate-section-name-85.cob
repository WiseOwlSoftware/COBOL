      *>--- conformance-case
      *> id: pb5924-udw-duplicate-section-name-85
      *> rules: [1985-SR-IV.4.2.2.1.1-p4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:IV.4.2.2.1.1 (cite.py: OK): "all user-defined words within a given disjoint set must be unique"
      *> Two sections of one name: a section-name takes no qualifier,
      *> so a PERFORM of it cannot be unique and is refused.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. UDWDSN.
       PROCEDURE DIVISION.
       MAIN-SEC SECTION.
       P0.
           PERFORM DUP-SEC.
           STOP RUN.
       DUP-SEC SECTION.
       P1.
           DISPLAY "A".
       DUP-SEC SECTION.
       P2.
           DISPLAY "B".
