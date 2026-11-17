      *>--- conformance-case
      *> id: pb5881-refmod-alphabetic-numeric-test-85
      *> rules: [1985-GR-IV.4.3.8.4.4-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.4 GR5: a reference-modified item has "the
      *> same class and category as that defined for the data item
      *> referenced by data-name-1" (only numeric, numeric edited and
      *> alphanumeric edited become alphanumeric), so AB (1:2) is
      *> alphabetic, and §1985:VI.6.3.1.2: "The NUMERIC test cannot be
      *> used with an item whose data description describes the item
      *> as alphabetic" (kb/Work PB5881). A compiler that accepts
      *> line 23 fails; the unsliced twin is pb5122-numeric-alphabetic-
      *> item-at-85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5881RA85.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AB PIC A(5) VALUE "ABCDE".
       PROCEDURE DIVISION.
       P0.
           IF AB (1:2) NUMERIC DISPLAY "T" ELSE DISPLAY "F".
           STOP RUN.
