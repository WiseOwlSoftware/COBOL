      *>--- conformance-case
      *> id: pb5924-seg-control-transfers-85
      *> rules: [1985-GR-XVI.1.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XVI.1.6 (cite.py: OK): "the implementor must provide control transfers to maintain the logical flow specified in the source program"
      *> Sections of intermixed fixed and independent segments run in their physical (logical) order,
      *> and GO TO between independent segments keeps that flow.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGFLOW.
       PROCEDURE DIVISION.
       S0 SECTION.
       P0.
           DISPLAY "S0".
       S60 SECTION 60.
       P60.
           DISPLAY "S60".
       S10 SECTION 10.
       P10.
           DISPLAY "S10".
       S70 SECTION 70.
       P70A.
           DISPLAY "S70A".
           GO TO P80B.
       P70B.
           DISPLAY "S70B".
       S20 SECTION 20.
       P20.
           DISPLAY "S20".
           STOP RUN.
       S80 SECTION 80.
       P80A.
           DISPLAY "S80A".
       P80B.
           DISPLAY "S80B".
           GO TO P70B.
