      *>--- conformance-case
      *> id: g2-qual-cd-name-two-cds
      *> rules: [2002-FMT-8.4.1.1.1, 2002-SR-8.4.1.1.2-1,
      *>    2002-SR-8.4.1.1.2-4, 2002-SR-8.4.1.2.2-1,
      *>    2002-FMT-8.4.2.1.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> Two output CDs each carry a record with FLD, its condition-name
      *> FLD-YES and a table TAB INDEXED BY TX: every one of those names
      *> is nonunique. Record description entries under an output CD
      *> "implicitly redefine the record area established by the output
      *> CD entry" (§2002:A.1.2.2 SR9), so the cd-name heads each
      *> record's hierarchy.
      *> §2002:8.4.1.1.1 - Formats 1-3 (qualified data-, condition- and
      *> index-name) end in a "file-cd-report-qualifier", IN|OF
      *> file-name-1 | cd-name-1 | report-name-1. Each name below is
      *> qualified by a cd-name, alone or after a data-qualifier.
      *> §2002:8.4.1.1.2 SR1 - "uniqueness shall be established through
      *> a sequence of qualifiers that precludes any ambiguity of
      *> reference": FLD IN CDA, FLD OF RECB OF CDB, FLD IN RECB, TX OF
      *> CDA each name exactly one item (the refusal of an unqualified
      *> FLD is g2-qual-cd-name-ambiguous beside this case).
      *> §2002:8.4.1.1.2 SR4 - qualifiers in the order of "successively
      *> more inclusive levels", and "For a condition-name, the hierarchy
      *> is that of the associated conditional variable": FLD-YES IN FLD
      *> IN RECB IN CDB and FLD-YES OF CDA (the misordered refusal is
      *> g2-qual-cd-name-misordered).
      *> §2002:8.4.1.2.2 SR1 - the base of a subscripted reference is a
      *> qualified-data-name as 8.4.1.1 defines it, so TAB OF CDA (TX OF
      *> TAB OF CDA) and TAB IN RECB OF CDB (TX IN CDB) are legal.
      *> §2002:8.4.2.1.1 - identifier Format 2 (qualified-data-name-with-
      *> subscripts-1) and Format 3 (reference modification) over the
      *> items of a CD record: Q5.
      *> Expected, from the MOVEs:
      *>   RECA ...... -> FLD IN CDA = YES; RECB ...... -> FLD = NO
      *>   Q1 YES|NO |          the two FLDs are distinct items
      *>   Q2 A-YES B-NO        FLD-YES tests its own CD's FLD
      *>   then FLD IN RECB = YES, FLD OF RECA IN CDA = NO
      *>   Q3 A-NO B-YES
      *>   TX OF CDA = 2, TX IN RECB IN CDB = 3; TAB(TX) receives Q / R,
      *>   TAB IN CDA (1) receives P:
      *>   Q4 [NO PQ.][YESS.R]  (TAB OF RECB (1) receives S)
      *>   Q5 N QR              FLD OF CDA (1:1), TAB OF CDA (2),
      *>                        TAB OF CDB (3)
      *> Each leg can fail: a qualifier resolved to the other CD prints
      *> the other value, a refused qualifier fails the compile, and an
      *> index shared between the CDs moves the wrong TAB element.
      *> The communication facility is obsolete at 2002 (COBOL0903).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2QUAL03.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDA FOR OUTPUT
           DESTINATION COUNT IS DCA
           TEXT LENGTH IS TLA
           STATUS KEY IS SKA
           ERROR KEY IS EKA
           SYMBOLIC DESTINATION IS SDA.
       01  RECA.
           05 FLD PIC X(3).
              88 FLD-YES VALUE "YES".
           05 TAB PIC X OCCURS 3 TIMES INDEXED BY TX.
       CD  CDB FOR OUTPUT
           DESTINATION COUNT IS DCB
           TEXT LENGTH IS TLB
           STATUS KEY IS SKB
           ERROR KEY IS EKB
           SYMBOLIC DESTINATION IS SDB.
       01  RECB.
           05 FLD PIC X(3).
              88 FLD-YES VALUE "YES".
           05 TAB PIC X OCCURS 3 TIMES INDEXED BY TX.
       PROCEDURE DIVISION.
           MOVE "......" TO RECA.
           MOVE "......" TO RECB.
           MOVE "YES" TO FLD IN CDA.
           MOVE "NO " TO FLD OF RECB OF CDB.
           DISPLAY "Q1 " FLD OF CDA "|" FLD IN CDB "|".
           IF FLD-YES OF CDA
               DISPLAY "Q2 A-YES " WITH NO ADVANCING
           ELSE
               DISPLAY "Q2 A-NO " WITH NO ADVANCING
           END-IF.
           IF FLD-YES IN FLD IN RECB IN CDB
               DISPLAY "B-YES"
           ELSE
               DISPLAY "B-NO"
           END-IF.
           MOVE "YES" TO FLD IN RECB.
           MOVE "NO " TO FLD OF RECA IN CDA.
           IF FLD-YES OF FLD OF RECA OF CDA
               DISPLAY "Q3 A-YES " WITH NO ADVANCING
           ELSE
               DISPLAY "Q3 A-NO " WITH NO ADVANCING
           END-IF.
           IF FLD-YES IN CDB
               DISPLAY "B-YES"
           ELSE
               DISPLAY "B-NO"
           END-IF.
           SET TX OF CDA TO 2.
           SET TX IN RECB IN CDB TO 3.
           MOVE "Q" TO TAB OF CDA (TX OF TAB OF CDA).
           MOVE "R" TO TAB IN RECB OF CDB (TX IN CDB).
           MOVE "P" TO TAB IN CDA (1).
           MOVE "S" TO TAB OF RECB (1).
           DISPLAY "Q4 [" RECA "][" RECB "]".
           DISPLAY "Q5 " FLD OF CDA (1:1) " " TAB OF CDA (2)
               TAB OF CDB (3).
           STOP RUN.
