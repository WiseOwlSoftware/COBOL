      *>--- conformance-case
      *> id: pb5886-standard-decimal-intermediate
      *> rules: [AR-15.3-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5886 review R3-1 - an integer argument is shown
      *> to be an integer only under the arithmetic in force.
      *>
      *> §2023:15.3 AR6 (cite.py --check): "An arithmetic
      *> expression that will always result in an integer value
      *> or an integer data item shall be specified". Under
      *> ARITHMETIC IS STANDARD-DECIMAL every intermediate is a
      *> standard-decimal intermediate data item (§2023:8.8.1.5.2)
      *> of 34 significant digits. B27 / 0.0001 / 16384 has up to
      *> 32 integer and 14 fraction digits, so its fraction is
      *> rounded away: at B27 = 999...9 the argument is
      *> ...998.9999, not an integer. The program is refused
      *> (COBOL1627). Its native twin, pb5886_native_quotient_chain,
      *> keeps the 14 fraction digits and is admitted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5886L.
       OPTIONS.
           ARITHMETIC IS STANDARD-DECIMAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  B27 PIC 9(27) VALUE 999999999999999999999999999.
       01  R   PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           COMPUTE R = FUNCTION MOD
               (B27 / 0.0001 / 16384 * 16384 * 0.0001, 1000).
           STOP RUN.
