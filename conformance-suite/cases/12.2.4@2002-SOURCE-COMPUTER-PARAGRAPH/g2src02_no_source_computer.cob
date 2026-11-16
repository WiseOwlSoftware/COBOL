      *>--- conformance-case
      *> id: g2src02_no_source_computer
      *> rules: [2002-GR-12.2.4.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.2.4.3 GR2 - "When the SOURCE-COMPUTER paragraph is
      *> not specified and the source unit is not contained within a
      *> source unit including a SOURCE-COMPUTER paragraph, the
      *> computer on which the source unit is being compiled is the
      *> source computer."
      *>   cite.py --check "§2002:12.2.4.3 GR2" "the computer on which
      *>   the source unit is being compiled is the source computer"
      *>   -> OK
      *> The paragraph is optional (§2002:12.2.1 prints
      *> [ source-computer-paragraph ]), and GR2 supplies the source
      *> computer when it is absent, so a unit with no paragraph and a
      *> contained unit with none in either compile with the compiling
      *> computer as their source computer and run. The source computer
      *> has no other observable effect in this implementation; what
      *> the case pins is that both units, each in GR2's situation
      *> (G2SCP02 has no paragraph; G2SRC2A has none and its container
      *> has none), are compiled and execute:
      *>   OUTER   (G2SCP02)
      *>   INNER   (G2SRC2A, called from G2SCP02)
      *>   OUTER-END
      *> No debugging line is written: §2002:6.2.7 permits one only
      *> "after the SOURCE-COMPUTER paragraph".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SCP02.
       PROCEDURE DIVISION.
           DISPLAY "OUTER".
           CALL "G2SRC2A".
           DISPLAY "OUTER-END".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SRC2A.
       PROCEDURE DIVISION.
           DISPLAY "INNER".
           GOBACK.
       END PROGRAM G2SRC2A.
       END PROGRAM G2SCP02.
