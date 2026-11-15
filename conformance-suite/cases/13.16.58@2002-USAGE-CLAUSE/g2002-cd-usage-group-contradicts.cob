      *>--- conformance-case
      *> id: g2002-cd-usage-group-contradicts
      *> rules: [2002-SR-13.16.58.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.2 SR2 - "If the USAGE clause is written in the
      *> data description entry for a group item, it may also be written
      *> in the data description entry for any subordinate elementary
      *> item or group item, but the same usage shall be specified in
      *> both entries." At 2002 the record description entries after a
      *> CD entry are ordinary data description entries
      *> (§2002:A.1.1.2): CR is USAGE DISPLAY and its subordinate X
      *> writes USAGE BINARY, so X is refused. The permitted form (the
      *> same usage written in both) runs in g2002-cd-usage-group-same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UCD05.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT
           TEXT LENGTH IS CO-LEN
           STATUS KEY IS CO-STAT.
       01  CR USAGE DISPLAY.
           05  X PIC 9(4) USAGE BINARY.
       PROCEDURE DIVISION.
           MOVE 1 TO X.
           STOP RUN.
