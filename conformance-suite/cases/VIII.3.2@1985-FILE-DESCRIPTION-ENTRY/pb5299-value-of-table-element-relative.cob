      *>--- conformance-case
      *> id: pb5299-value-of-table-element-relative
      *> rules: [1985-GR-VIII.3.2.4-6]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5299. §1985:VII.3.9.3 SR1: data-name-1 "cannot be
      *> subscripted", and "Each table element reference must be
      *> subscripted except" in a SEARCH subject, a REDEFINES clause or an
      *> OCCURS KEY IS phrase (§1985:IV.4.3.8.2.3 SR4), so a table element
      *> can never be the data-name of a VALUE OF clause. E is one. The
      *> relative FD takes the sequential FD's clause (§1985:VIII.3.2.4
      *> GR6). Refused at 85.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5299REL.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT RFX ASSIGN TO "pb5299r.dat" ORGANIZATION RELATIVE
               ACCESS RANDOM RELATIVE KEY RK FILE STATUS FS.
       DATA DIVISION.
       FILE SECTION.
       FD  RFX
           VALUE OF FILE-ID E.
       01  RFX-REC PIC X(10).
       WORKING-STORAGE SECTION.
       01  RK PIC 9(4) VALUE 1.
       01  FS PIC XX.
       01  T. 05 E PIC X(10) OCCURS 2 TIMES.
       PROCEDURE DIVISION.
       P.  STOP RUN.
