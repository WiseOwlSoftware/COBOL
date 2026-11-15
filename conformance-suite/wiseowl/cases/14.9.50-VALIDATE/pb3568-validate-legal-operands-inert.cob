      *>--- conformance-case
      *> id: pb3568-validate-legal-operands-inert
      *> rules: [SR-14.9.50.3-1]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> The control for the pb3568-validate-* refusals (kb/Work PB3568): every legal shape of identifier-1 still
      *> compiles clean - a group holding a table, a qualified elementary item, a subscripted table element - and the
      *> statement changes no item (§2023:14.9.50.4 GR5: "the content of the invalid data item does not change"; here every
      *> item is valid, and no exception checking is on). ISO §2023:14.9.50.3 SR1 admits a data item described in the
      *> working-storage section, referenced without reference modification.
      *>   cite.py: OK  §2023:14.9.50.3 1)  (Syntax rules)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3568OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 FLD PIC X(3) VALUE "ABC".
          05 TBL PIC 9 OCCURS 3 TIMES VALUE 7.
       01 SECOND-REC.
          05 FLD PIC X(2) VALUE "ZZ".
       01 IX PIC 9 VALUE 2.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "BEFORE"
           VALIDATE REC
           VALIDATE FLD OF REC TBL (IX) FLD IN SECOND-REC
           DISPLAY "AFTER " FLD OF REC " " TBL (IX) " "
               FLD OF SECOND-REC.
           STOP RUN.
