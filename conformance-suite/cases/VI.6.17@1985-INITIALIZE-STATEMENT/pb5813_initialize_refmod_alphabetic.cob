      *>--- conformance-case
      *> id: pb5813_initialize_refmod_alphabetic
      *> rules: [1985-GR-VI.6.17.4-1, 1985-GR-VI.6.17.4-2, GR-14.9.20.4-2, GR-14.9.20.4-4, GR-14.9.20.4-5, GR-14.9.20.4-6, SR-14.9.20.3-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.4.4 GR5 keeps the category of a reference-
      *> modified item "except that the categories numeric, numeric
      *> edited, and alphanumeric edited are considered class and
      *> category alphanumeric"; alphabetic is not named, so a slice
      *> of a PIC A item is still alphabetic. §1985:VI.6.17.4 GR2
      *> initializes an item "only if it belongs to the category
      *> specified in the REPLACING phrase". (cite.py --check OK for
      *> both.) Legs: INITIALIZE WA (2:1) REPLACING ALPHABETIC writes
      *> Q (M1); the same on WB with ALPHANUMERIC changes nothing
      *> (M2); a slice of a PIC X item is alphanumeric, so REPLACING
      *> ALPHANUMERIC writes (M3) and REPLACING ALPHABETIC does not
      *> (M4).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5813A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  WA PIC A(3) VALUE "abc".
       01  WB PIC A(3) VALUE "abc".
       01  WC PIC X(3) VALUE "abc".
       01  WD PIC X(3) VALUE "abc".
       PROCEDURE DIVISION.
       P0.
           INITIALIZE WA (2:1) REPLACING ALPHABETIC DATA BY "Q".
           DISPLAY "M1[" WA "]".
           INITIALIZE WB (2:1) REPLACING ALPHANUMERIC DATA BY "Q".
           DISPLAY "M2[" WB "]".
           INITIALIZE WC (2:1) REPLACING ALPHANUMERIC DATA BY "Q".
           DISPLAY "M3[" WC "]".
           INITIALIZE WD (2:1) REPLACING ALPHABETIC DATA BY "Q".
           DISPLAY "M4[" WD "]".
           STOP RUN.
