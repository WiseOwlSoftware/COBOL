      *>--- conformance-case
      *> id: pb5731_sort_giving_format3_odo_smaller_than_smallest
      *> rules: [1985-SR-XI.4.4.3-10]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5731. A refusal. §1985:VII.3.8.4 GR15 sizes the format 3
      *> records of G1 at their maximum (cite.py --check OK: "the sum of the
      *> maximum number of characters in any variable length item subordinate
      *> to the record"): G1R is 1 + 7 = 8 characters and G1S is 9. The
      *> record of SW is 5 characters, which §1985:XI.4.4.3 SR10 ("must not
      *> be less than the smallest record nor larger than the largest record
      *> described for file-name-3") refuses: 5 is less than 8. A compiler
      *> that sizes G1R at its minimum (2) accepts it and fails.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5731R.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SW ASSIGN TO "PB5731R.TMP".
           SELECT U1 ASSIGN TO "PB5731R1.DAT".
           SELECT G1 ASSIGN TO "PB5731R2.DAT".
       DATA DIVISION.
       FILE SECTION.
       SD SW.
       01 SR.
          05 SK PIC XX.
          05 SX PIC X(3).
       FD U1.
       01 U1R PIC X(5).
       FD G1 RECORD CONTAINS 8 TO 9 CHARACTERS.
       01 G1R.
          05 GN PIC 9.
          05 GT PIC X OCCURS 1 TO 7 TIMES DEPENDING ON GN.
       01 G1S PIC X(9).
       PROCEDURE DIVISION.
       P0.
           SORT SW ASCENDING KEY SK
               USING U1 GIVING G1.
           STOP RUN.
