      *>--- conformance-case
      *> id: g85_f09_01
      *> rules: [1985-SR-VI.5.10.3-8, 1985-SR-VI.5.10.3-10,
      *>    1985-SR-VI.5.10.3-12]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.10.3 SR8 - two redefinitions (RD1, RD2) of ORIG,
      *>   both naming ORIG, the entry that originally defined the
      *>   area: "Multiple redefinitions of the same character
      *>   positions are permitted." Lines SR8-* pin it.
      *> §1985:VI.5.10.3 SR10 - TGT REDEFINES SRC with SRC's own
      *>   level-10 subordinates between them; only an entry with a
      *>   level-number numerically LOWER than 05 is barred there.
      *>   Lines SR10-* pin it.
      *> §1985:VI.5.10.3 SR12 (level-2 text, legal at 85) - "In
      *>   level 2, data-name-2 may be subordinate to an entry which
      *>   contains a REDEFINES clause": Q3 REDEFINES Q2, and Q2 is
      *>   subordinate to Q, which REDEFINES P. Lines SR12-* pin it.
      *> Values follow §1985:VI.5.10.4 GR1 (storage starts at
      *>   data-name-2) and GR2 (any of the names references the
      *>   shared positions). A compiler that refuses any of the
      *>   three forms, or lays a redefinition anywhere but over its
      *>   data-name-2, fails a line below.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85F0901.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W8.
           05  ORIG          PIC X(4) VALUE "1234".
           05  RD1 REDEFINES ORIG PIC 9(4).
           05  RD2 REDEFINES ORIG.
               10  RD2A      PIC XX.
               10  RD2B      PIC XX.
           05  AFTER8        PIC XX VALUE "ZZ".
       01  W10.
           05  SRC.
               10  SRC1      PIC XX VALUE "AB".
               10  SRC2      PIC XX VALUE "CD".
           05  TGT REDEFINES SRC PIC X(4).
       01  W12.
           05  P             PIC X(6) VALUE "PQRSTU".
           05  Q REDEFINES P.
               10  Q1        PIC XX.
               10  Q2        PIC X(4).
               10  Q3 REDEFINES Q2.
                   15  Q3A   PIC X.
                   15  Q3B   PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "SR8-RD1=" RD1 " RD2A=" RD2A " RD2B=" RD2B.
           ADD 1 TO RD1.
           DISPLAY "SR8-ORIG=" ORIG " RD2B=" RD2B " AFTER=" AFTER8.
           DISPLAY "SR10-TGT=" TGT.
           MOVE "WXYZ" TO TGT.
           DISPLAY "SR10-SRC1=" SRC1 " SRC2=" SRC2.
           DISPLAY "SR12-Q1=" Q1 " Q2=" Q2 " Q3A=" Q3A " Q3B=" Q3B.
           MOVE "9" TO Q3A.
           DISPLAY "SR12-P=" P.
           STOP RUN.
