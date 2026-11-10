      *>--- conformance-case
      *> id: 1985-FMT-XVI_2_3_2
      *> rules: [1985-FMT-XVI.2.3.2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:XVI.2.3.2 (THE SEGMENT-LIMIT CLAUSE). An output test: the
      *> program must translate and run at 85 and DISPLAY exactly the
      *> lines its .out holds, the outcome the rule requires. Placed
      *> from the audit's draft, confirmed by
      *> scripts/spec/place_witnesses.py (kb/Work PB4995).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGLIM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XCOMP.
       OBJECT-COMPUTER. XCOMP
           SEGMENT-LIMIT IS 10.
       PROCEDURE DIVISION.
       MAIN-SECT SECTION 0.
       P1.
           DISPLAY "FIXED".
           PERFORM OVL-SECT.
           STOP RUN.
       OVL-SECT SECTION 20.
       P2.
           DISPLAY "OVERLAY".
