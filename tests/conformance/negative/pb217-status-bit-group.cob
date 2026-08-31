      *> reject-at: 2002 2014 2023
      *> ISO 14.9.42.3 SR2: "Identifier-1 shall reference an integer data item or
      *> a data item with usage display or usage national." A BIT group is
      *> "treated as though it were an elementary data item of usage bit and
      *> class and category boolean described with PICTURE 1(m)" (13.18.29.4
      *> GR1 b) - usage BIT is none of SR2's three descriptions.
      *> The companion is the shape SR2 DOES admit and this screen used to
      *> reject: a GROUP-USAGE NATIONAL group, which 13.18.29.4 GR2 b makes an
      *> item of usage national (see StopGobackExitCodeTests). Reading the ONE
      *> operand-category reader (DataItem.OperandPic) settles both, and the
      *> alphanumeric group of pb169-status-group-identifier stays rejected too.
      *> kb/Work PB217.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB217N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-BITGRP GROUP-USAGE IS BIT.
          05 WS-B PIC 1(8) USAGE BIT VALUE B"00000111".
       PROCEDURE DIVISION.
       MAIN.
           STOP RUN WITH ERROR STATUS WS-BITGRP.
