      *>--- conformance-case
      *> id: pb5849_packed_under_binary_group_refused_85
      *> rules: [1985-SR-VI.5.14.3-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5849, the control of
      *> pb5849_comp_under_binary_group_85. §1985:VI.5.14.3 SR2: a
      *> subordinate of a group whose USAGE is written may repeat it,
      *> "but the same usage must be specified". BINARY and
      *> PACKED-DECIMAL are two manners of representation
      *> (§1985:VI.5.14.4 GR2), so E2 under the BINARY group H breaks
      *> the rule and WiseOwl refuses it at 85 with COBOL1927; a
      *> compiler that accepts it fails. The group G above it, a
      *> COMPUTATIONAL item under BINARY, is legal by the determination
      *> of docs/conformance/7/DOC-A.1-208.md and is not the refusal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5849U1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 G USAGE BINARY.
          05 S.
             10 E1 PIC S9(4) USAGE COMPUTATIONAL.
       01 H USAGE BINARY.
          05 E2 PIC S9(4) USAGE PACKED-DECIMAL.
       PROCEDURE DIVISION.
       P0.
           MOVE 1 TO E1.
           DISPLAY E1.
           STOP RUN.
