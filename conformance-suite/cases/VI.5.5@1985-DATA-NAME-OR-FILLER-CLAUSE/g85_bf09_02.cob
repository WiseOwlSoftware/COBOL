      *>--- conformance-case
      *> id: g85_bf09_02
      *> rules: [1985-GR-VI.5.5.4-1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.5.4 GR1 - an entry with no data-name-1 or FILLER
      *> clause is treated as though FILLER had been specified.
      *>   cite.py: OK  §1985:VI.5.5.4 GR1 "the data item being
      *>     described is treated as though FILLER had been specified"
      *>   cite.py: OK  §1985:VI.6.17.4 GR3 "Index data items and
      *>     elementary FILLER data items are not affected by the
      *>     execution of an INITIALIZE statement"
      *> The CORRESPONDING phrase pairs only items "not designated by
      *> the key word FILLER" with the same data-name (§1985:VI.6.4.3
      *> GR1).
      *> DERIVATION. GRP is AB CD 12 34, the 2nd and 4th items unnamed.
      *> MOVE CORRESPONDING GRP TO GRP2 moves N1 and N2 only, so GRP2
      *> (.. .. 55 66) becomes AB..1266: the unnamed items are FILLER
      *> and correspond to nothing. INITIALIZE GRP sets N1 to spaces and
      *> N2 to zeros (§1985:VI.6.17.4 GR5) and leaves the unnamed items,
      *> FILLER, alone: "  CD0034". Fails with ABAB1234 / AB..1234-style
      *> lines if an unnamed item is paired, or with "    0000" if
      *> INITIALIZE touches it.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF0902.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  GRP.
           05  N1 PIC X(2) VALUE "AB".
           05  PIC X(2) VALUE "CD".
           05  N2 PIC 9(2) VALUE 12.
           05  PIC 9(2) VALUE 34.
       01  GRP2.
           05  N1 PIC X(2) VALUE "..".
           05  PIC X(2) VALUE "..".
           05  N2 PIC 9(2) VALUE 55.
           05  PIC 9(2) VALUE 66.
       PROCEDURE DIVISION.
       P1.
           MOVE CORRESPONDING GRP TO GRP2.
           DISPLAY GRP2.
           INITIALIZE GRP.
           DISPLAY "[" GRP "]".
           STOP RUN.
