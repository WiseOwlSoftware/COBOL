      *>--- conformance-case
      *> id: g85_bf15_03
      *> rules: [1985-GR-VI.6.4.3-5]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.6.4.3 GR5 - "The name of each data item which
      *> satisfies the above conditions must be unique after
      *> application of the implied qualifiers." QQ occurs twice in G1,
      *> but QQ IN S1 IN G1 and QQ IN S2 IN G1 are each unique once
      *> the implied qualifiers (S1, S2 up to G1) are applied, so each
      *> pairs only with its namesake under the same qualifier in G2
      *> (condition 1: "the same qualifiers up to, but not including,
      *> D1 and D2"). S1 and S2 are groups on both sides and are no
      *> pair (condition 2); ZZ and DD have no partner.
      *> MOVE CORRESPONDING G1 TO G2: G2 = BB QQ(S2) RR(S1) QQ(S1) AA ZZ
      *>   = 5 4 3 2 1 0 -> 543210.
      *> ADD CORRESPONDING G1 TO G3: QQ IN S1 IN G3 = 10 + 2 = 12; QQ
      *>   IN G3 has no partner (G1 has no QQ directly under it) and
      *>   stays 20; AA = 10 + 1 = 11 -> 122011.
      *> A compiler that mixes the two QQ, or treats them as ambiguous
      *> and moves neither, fails a line.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1503.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  G1.
           05  AA  PIC 9 VALUE 1.
           05  S1.
               10  QQ  PIC 9 VALUE 2.
               10  RR  PIC 9 VALUE 3.
           05  S2.
               10  QQ  PIC 9 VALUE 4.
           05  BB  PIC 9 VALUE 5.
           05  DD  PIC 9 VALUE 6.
       01  G2.
           05  BB  PIC 9 VALUE 0.
           05  S2.
               10  QQ  PIC 9 VALUE 0.
           05  S1.
               10  RR  PIC 9 VALUE 0.
               10  QQ  PIC 9 VALUE 0.
           05  AA  PIC 9 VALUE 0.
           05  ZZ  PIC 9 VALUE 0.
       01  G3.
           05  S1.
               10  QQ  PIC 99 VALUE 10.
           05  QQ  PIC 99 VALUE 20.
           05  AA  PIC 99 VALUE 10.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE CORRESPONDING G1 TO G2.
           DISPLAY "MOVE " G2.
           ADD CORRESPONDING G1 TO G3.
           DISPLAY "ADD " G3.
           STOP RUN.
