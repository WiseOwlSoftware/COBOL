      *>--- conformance-case
      *> id: pb5891_scaled_product_exponent
      *> rules: [AR-15.3-6]
      *> accept: [85, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5891 review R3-1 - an exponent that is a product
      *> of a scaled and a P-scaled item has scale 0, so it is an
      *> integer by its carrier and the power is exact.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified".
      *> K PIC 9V9 (scale 1) times PZ PIC 9P (scale -1) is carried
      *> at scale 0. EXPECTED, with K = 0.5 and PZ = 20: K * PZ = 10,
      *> 1 ** 10 = 1 (Z 0001); MOD (1 + 4, 10) = 5 (Y 0005).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5891I.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  K   PIC 9V9 VALUE 0.5.
       01  PZ  PIC 9P VALUE 20.
       01  R   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = 1 ** (K * PZ).
           DISPLAY "Z " R.
           COMPUTE R = FUNCTION MOD (1 ** (K * PZ) + 4, 10).
           DISPLAY "Y " R.
           STOP RUN.
