      *>--- conformance-case
      *> id: pb5709-constant-length-data-name-subscript
      *> rules: [2002-SR-13.9.2-3, 2002-GR-13.9.3-3, 2002-GR-13.9.3-5, 2002-GR-13.9.3-6]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.9.2 SR3 - "Data-name-1 and data-name-2 may be
      *> qualified." The 2002 constant entry has no rule that the
      *> operand's subscripts be literals (that rule is §2014:13.10.2
      *> SR3, "All subscripts of data-name-1 and data-name-2 shall be
      *> literals", and §2023:13.10.3 SR3), so a subscript is an ordinary
      *> one (§2002:8.4.1.2.2) and a data-name may be written.
      *> §2002:13.9.3 GR6 / GR5: the value is that of the LENGTH /
      *> BYTE-LENGTH function, and every occurrence of a table element
      *> has the same length, so the subscript never changes it.
      *>   K1 = 3  LENGTH OF E (I)           E is PIC X(3)
      *>   K2 = 3  BYTE-LENGTH OF E (I + 1)  an arithmetic subscript
      *>   K3 = 4  LENGTH OF C OF R (I J)    C PIC X(4) in a two-level
      *>                                     table, qualified
      *> A leg fails if the constant entry is refused (the 2023 literal
      *> rule applied at 2002) or the value is that of another item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5709OK.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 I PIC 9 VALUE 2.
       01 J PIC 9 VALUE 3.
       01 T.
          05 E PIC X(3) OCCURS 4.
       01 TT.
          05 R OCCURS 2.
             10 C PIC X(4) OCCURS 5.
       01 K1 CONSTANT AS LENGTH OF E (I).
       01 K2 CONSTANT AS BYTE-LENGTH OF E (I + 1).
       01 K3 CONSTANT AS LENGTH OF C OF R (I J).
       PROCEDURE DIVISION.
           DISPLAY "K1=" K1 " K2=" K2 " K3=" K3
           STOP RUN.
