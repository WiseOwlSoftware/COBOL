      *>--- conformance-case
      *> id: g85_bf12_03
      *> rules: [1985-GR-VI.5.14.4-2]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VI.5.14.4 GR2 - the USAGE clause "specifies the manner in
      *> which a data item is represented in the storage of a computer.
      *> It does not affect the use of the data item".
      *> Five items share one PICTURE, S9(4), and one value, -1234, and
      *> differ ONLY in usage: implicit DISPLAY (GR6), USAGE IS DISPLAY,
      *> BINARY, COMPUTATIONAL and PACKED-DECIMAL. GR2 says the same
      *> statements give the same results whatever the representation.
      *> DERIVED:
      *>  ADD: ADD 1 to each gives -1233; moved to a -9999 numeric
      *>   edited item each prints "-1233".
      *>  ANUM: each moved to a PIC X(4) item. §1985:VI.6.19.4 GR4a: the
      *>   operational sign is not moved and "conversion of the sending
      *>   operand to the internal representation of the receiving
      *>   operand takes place", so every usage gives "1233".
      *>  CMP: each compares equal to -1233 and to the DISPLAY item.
      *>  MUL: MULTIPLY 2 BY each, DIVIDE 4 INTO each:
      *>   -1233 * 2 = -2466,
      *>   / 4 = -616.5 truncated to -616 (no ROUNDED: §1985:VI.6.4.1
      *>   "truncation is relative to the size provided for the
      *>   resultant identifier"); prints "-0616".
      *> A usage whose representation leaked into its use (a sign lost
      *> or a digit mangled in conversion) breaks a column.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF1203.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  ND   PIC S9(4) VALUE -1234.
       01  NX   PIC S9(4) USAGE IS DISPLAY VALUE -1234.
       01  NB   PIC S9(4) USAGE IS BINARY VALUE -1234.
       01  NC   PIC S9(4) USAGE IS COMPUTATIONAL VALUE -1234.
       01  NP   PIC S9(4) USAGE IS PACKED-DECIMAL VALUE -1234.
       01  E1   PIC -9999.
       01  E2   PIC -9999.
       01  E3   PIC -9999.
       01  E4   PIC -9999.
       01  E5   PIC -9999.
       01  A1   PIC X(4).
       01  A2   PIC X(4).
       01  A3   PIC X(4).
       01  A4   PIC X(4).
       01  A5   PIC X(4).
       01  FLG  PIC X(3).
       PROCEDURE DIVISION.
       MAIN-PARA.
           ADD 1 TO ND NX NB NC NP.
           MOVE ND TO E1.
           MOVE NX TO E2.
           MOVE NB TO E3.
           MOVE NC TO E4.
           MOVE NP TO E5.
           DISPLAY "ADD=" E1 " " E2 " " E3 " " E4 " " E5.
           MOVE ND TO A1.
           MOVE NX TO A2.
           MOVE NB TO A3.
           MOVE NC TO A4.
           MOVE NP TO A5.
           DISPLAY "ANUM=" A1 " " A2 " " A3 " " A4 " " A5.
           MOVE "NO" TO FLG.
           IF ND = -1233 AND NX = -1233 AND NB = -1233
              AND NC = -1233 AND NP = -1233
              AND NB = ND AND NC = ND AND NP = ND
               MOVE "YES" TO FLG
           END-IF.
           DISPLAY "CMP=" FLG.
           MULTIPLY 2 BY ND NX NB NC NP.
           DIVIDE 4 INTO ND NX NB NC NP.
           MOVE ND TO E1.
           MOVE NX TO E2.
           MOVE NB TO E3.
           MOVE NC TO E4.
           MOVE NP TO E5.
           DISPLAY "MUL=" E1 " " E2 " " E3 " " E4 " " E5.
           STOP RUN.
