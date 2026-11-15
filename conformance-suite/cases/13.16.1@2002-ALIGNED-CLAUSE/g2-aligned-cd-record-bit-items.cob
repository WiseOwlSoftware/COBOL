      *>--- conformance-case
      *> id: g2-aligned-cd-record-bit-items
      *> rules: [2002-SR-13.16.1.2-1, 2002-GR-13.16.1.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.1.2 SR1 - "The ALIGNED clause may be specified
      *> only for a bit group item or an elementary bit data item."
      *> Here it is written on elementary bit items (B2, T) and on a
      *> bit group (BSUB) in record description entries that follow a
      *> CD entry; §2002:A.1.1.2 makes those records "a set of data
      *> description entries" of 13.14, so SR1 grants the form there.
      *> The program must be ACCEPTED (its refusal sibling is
      *> g2-aligned-cd-record-non-bit).
      *> §2002:13.16.1.3 GR2 - "An ALIGNED clause specified for a
      *> multiple-occurrence data item applies to each occurrence of
      *> that item."  §2002:13.16.1.3 GR1 puts the subject "on the
      *> first bit of the first available byte boundary".
      *> Derivation (§2002:8.5.1.5.3; LENGTH §2002:15.39.3 RV1/RV3):
      *>  ALIGNGRP is an alphanumeric group: B1 takes bits 1-4 of byte
      *>  1, B2 ALIGNED starts byte 2, trailing filler completes byte 2
      *>  -> LENGTH 2 (1 without ALIGNED).
      *>  TBLALIGN: each of the 3 occurrences of T starts a byte (GR2)
      *>  -> LENGTH 3; TBLPLAIN packs 12 bits -> LENGTH 2.  A GR2 that
      *>  aligned only the first occurrence would give 2.
      *>  BGRP is a bit group (GROUP-USAGE BIT): BA 3 bits, filler to
      *>  the byte boundary (5 bits), BSUB ALIGNED 2 bits, no filler at
      *>  the end of a record that is entirely a bit group -> LENGTH 10
      *>  boolean positions (3 + 2 = 5 without ALIGNED).
      *> The CD's records implicitly redefine one area (§2002:A.1.1.2),
      *> so each record is set and checked before the next is used.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ALG01.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N2 PIC 99.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           STATUS KEY IS OUT-ST
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  ALIGNGRP.
           05 B1 PIC 1(4) USAGE BIT.
           05 B2 PIC 1(4) USAGE BIT ALIGNED.
       01  TBLALIGN.
           05 T PIC 1(4) USAGE BIT OCCURS 3 ALIGNED.
       01  TBLPLAIN.
           05 U PIC 1(4) USAGE BIT OCCURS 3.
       01  BGRP GROUP-USAGE IS BIT.
           05 BA PIC 1(3) USAGE BIT.
           05 BSUB ALIGNED.
              10 BB PIC 1(2) USAGE BIT.
       PROCEDURE DIVISION.
       P-PARA.
           MOVE FUNCTION LENGTH(ALIGNGRP) TO N2
           DISPLAY "ALIGNED=" N2
           MOVE FUNCTION LENGTH(TBLALIGN) TO N2
           DISPLAY "TABLE=" N2
           MOVE FUNCTION LENGTH(TBLPLAIN) TO N2
           DISPLAY "NOTABLE=" N2
           MOVE FUNCTION LENGTH(BGRP) TO N2
           DISPLAY "BITGROUP=" N2
           MOVE B"1100" TO B1
           MOVE B"0011" TO B2
           IF B1 = B"1100" AND B2 = B"0011"
               DISPLAY "B1 B2 OK"
           ELSE
               DISPLAY "B1 B2 WRONG"
           END-IF
           MOVE B"1000" TO T(1)
           MOVE B"0100" TO T(2)
           MOVE B"0010" TO T(3)
           IF T(1) = B"1000" AND T(2) = B"0100" AND T(3) = B"0010"
               DISPLAY "T OK"
           ELSE
               DISPLAY "T WRONG"
           END-IF
           STOP RUN.
