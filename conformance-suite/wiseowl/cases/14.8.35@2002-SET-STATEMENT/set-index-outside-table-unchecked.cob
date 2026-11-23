      *>--- conformance-case
      *> id: set-index-outside-table-unchecked
      *> rules: [2002-GR-14.8.35.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *>---
      *> Determination D-SET-INDEX-UNCHECKED (docs/conformance/3,
      *> kb/Work PB5625 reading C, PB5674): with checking for
      *> EC-BOUND-SUBSCRIPT NOT enabled (the default, §2002:14.5.12.1)
      *> the outcome of SET index-name past its table is
      *> implementor-defined (§2002:14.5.12.1.2); WiseOwl COBOL stores
      *> the occurrence number and continues, as GnuCOBOL does.
      *> T1 has 10 occurrences: TO 11 gives 11, UP BY 20 gives 31,
      *> DOWN BY 31 gives 0. The same program with checking enabled is
      *> the 2002 golden set-index-outside-table-checked.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SETIXTBLU.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T1.
          05 E1 PIC X OCCURS 10 INDEXED BY IX1.
       01 N PIC 99.
       01 NX PIC XX.
       PROCEDURE DIVISION.
       M1.
           SET IX1 TO 11.
           SET N TO IX1. MOVE N TO NX. DISPLAY "A=" NX.
           SET IX1 UP BY 20.
           SET N TO IX1. MOVE N TO NX. DISPLAY "B=" NX.
           SET IX1 DOWN BY 31.
           SET N TO IX1. MOVE N TO NX. DISPLAY "C=" NX.
           STOP RUN.
