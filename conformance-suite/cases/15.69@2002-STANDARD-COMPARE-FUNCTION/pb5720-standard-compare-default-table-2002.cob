      *>--- conformance-case
      *> id: pb5720-standard-compare-default-table-2002
      *> rules: [2002-AR-15.69.2-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5720 - §2002:15.69.2 AR4 (cite.py --check: OK): "If ordering-name-1 is not specified, the default ordering
      *> table 'ISO14651_2002_TABLE1' specified in ISO/IEC 14651:2001 shall be used"; §2002:12.2.6.3 GR17 leaves the allowable
      *> content of literal-9 to the implementor, and this implementation's derived table (owner decision Q4: CLDR/UCA data,
      *> consistent with ISO/IEC 14651) is the default table of every edition, named by that edition's own spelling. So the
      *> 2002 name is a provided ordering table: no COBOL1662, no EC-ORDER-NOT-SUPPORTED, and both references compare "a"
      *> and "B" at the highest level the table defines, where the primary weight of "a" is below that of "B": "<".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5720P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ORDER TABLE T02 IS "ISO14651_2002_TABLE1".
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "NAMED " FUNCTION STANDARD-COMPARE("a" "B" T02)
           DISPLAY "DEFAULT " FUNCTION STANDARD-COMPARE("a" "B")
           STOP RUN.
