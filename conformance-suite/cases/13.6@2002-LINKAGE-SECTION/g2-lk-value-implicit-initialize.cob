      *>--- conformance-case
      *> id: g2-lk-value-implicit-initialize
      *> rules: [2002-GR-13.6.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.6.3 GR5 - "A data-item format or table format VALUE
      *> clause specified in the linkage section is ignored except in
      *> the execution of an explicit or implicit INITIALIZE statement."
      *> The implicit leg: LB is a based item of the linkage section
      *> (§2002:13.6.2 SR4), with a data-item format VALUE on LBX and a
      *> table format VALUE on LBT.  ALLOCATE LB INITIALIZED initializes
      *> the storage "as if an INITIALIZE data-name-1 WITH FILLER ALL TO
      *> VALUE THEN TO DEFAULT statement were executed" (§2002:14.8.3.3
      *> GR7), an implicit INITIALIZE, so both VALUEs take effect:
      *> LB=[LKVPQ].  The storage is then overwritten ([.....]) and an
      *> explicit INITIALIZE LB ALL TO VALUE restores [LKVPQ].
      *> The explicit leg on a formal and the "corresponding formal
      *> parameter" initial value are g2-value-linkage-initialize.
      *> Fails if a linkage VALUE is not applied by the implicit or the
      *> explicit INITIALIZE.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK02.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LB BASED.
           05 LBX PIC X(3) VALUE "LKV".
           05 LBT PIC X OCCURS 2
              VALUES ARE "P" "Q" FROM (1) TO (2).
       PROCEDURE DIVISION.
       MAIN-P.
           ALLOCATE LB INITIALIZED
           DISPLAY "ALLOCATED=[" LB "]"
           MOVE "....." TO LB
           DISPLAY "MOVED=[" LB "]"
           INITIALIZE LB ALL TO VALUE
           DISPLAY "INITIALIZED=[" LB "]"
           STOP RUN.
       END PROGRAM G2LK02.
