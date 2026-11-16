      *>--- conformance-case
      *> id: g2cs401_selected_entry_conversion
      *> rules: [2002-GR-13.16.13.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.13.3 GR4 - "If record description entries associated
      *> with the file contain a SELECT WHEN clause, a record description
      *> entry is selected by evaluation of those SELECT WHEN clauses. If
      *> there are no record description entries associated with the
      *> file, the record description used for conversion is the
      *> description of the identifier or literal specified in the FROM
      *> phrase of a WRITE or REWRITE statement specifying the FILE
      *> option. Alphabet-name-1 is used for code-set conversion of each
      *> data item described with usage display in the selected record
      *> description entry." NOTE: "There is no conversion of data items
      *> described with other usages unless a FORMAT clause applies."
      *>   cite.py --check "§2002:13.16.13.3 GR4" -> OK
      *> A-EBC is code-name-1 EBCDIC; §2002:12.2.6.3 GR7 i) leaves its
      *> set and correspondence to the implementor (cite.py OK), defined
      *> as IBM CCSID 37 (docs/conformance DOC-A.1-183/184): A=X'C1',
      *> B=X'C2', P=X'D7', X=X'E7', 1=X'F1'. ORD of a native character is
      *> its code unit plus 1. FR/FV read the same files with no CODE-SET.
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   SELECT WHEN leg (FC): REC-A is selected (A-KEY = "A"); A-KEY
      *>   and A-TEXT are usage display and converted, A-NUM is binary
      *>   and crosses as its own bytes:
      *>     A KEY 194 / A NUM RAW SAME / A TEXT 232
      *>   REC-B is selected by OTHER (B-KEY = "B"); all display:
      *>     B KEY 195 / B TEXT 216
      *>   record-less leg (FW): WRITE FILE FW FROM W converts by W's
      *>   description, two alphanumeric and one numeric display item
      *>   "A" "B" 1:                          => W 194 195 242
      *> A leg fails if the binary item is converted (RAW DIFF), if a
      *> display item is not converted (66, 89, 67, 81, 50), or if the
      *> record-less WRITE FILE is refused or written unconverted.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CS401.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FC ASSIGN TO "G2CS401C.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FR ASSIGN TO "G2CS401C.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FW ASSIGN TO "G2CS401W.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FV ASSIGN TO "G2CS401W.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FC CODE-SET IS A-EBC.
       01  REC-A SELECT WHEN IS-A.
           05 A-KEY    PIC X.
              88 IS-A  VALUE "A".
           05 A-NUM    PIC 9(4) COMP.
           05 A-TEXT   PIC X(3).
       01  REC-B SELECT WHEN OTHER.
           05 B-KEY    PIC X.
           05 B-TEXT   PIC X(5).
       FD  FR.
       01  RR          PIC X(6).
       FD  FW CODE-SET IS A-EBC RECORD CONTAINS 3 CHARACTERS.
       FD  FV RECORD CONTAINS 3 CHARACTERS.
       01  RV          PIC X(3).
       WORKING-STORAGE SECTION.
       01  W.
           05 W1       PIC X(2) VALUE "AB".
           05 W2       PIC 9 VALUE 1.
       01  WN          PIC 9(4) COMP.
       01  WN-X REDEFINES WN PIC X(2).
       01  O           PIC 999.
       01  O2          PIC 999.
       01  O3          PIC 999.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT FC.
           MOVE "A" TO A-KEY.
           MOVE 16706 TO A-NUM.
           MOVE "XYZ" TO A-TEXT.
           WRITE REC-A.
           MOVE "B" TO B-KEY.
           MOVE "PQRST" TO B-TEXT.
           WRITE REC-B.
           CLOSE FC.
           MOVE 16706 TO WN.
           OPEN INPUT FR.
           READ FR AT END CONTINUE END-READ.
           MOVE FUNCTION ORD(RR(1:1)) TO O.
           DISPLAY "A KEY " O.
           IF RR(2:2) = WN-X
               DISPLAY "A NUM RAW SAME"
           ELSE
               DISPLAY "A NUM RAW DIFF"
           END-IF.
           MOVE FUNCTION ORD(RR(4:1)) TO O.
           DISPLAY "A TEXT " O.
           READ FR AT END CONTINUE END-READ.
           MOVE FUNCTION ORD(RR(1:1)) TO O.
           DISPLAY "B KEY " O.
           MOVE FUNCTION ORD(RR(2:1)) TO O.
           DISPLAY "B TEXT " O.
           CLOSE FR.
           OPEN OUTPUT FW.
           WRITE FILE FW FROM W.
           CLOSE FW.
           OPEN INPUT FV.
           READ FV AT END CONTINUE END-READ.
           MOVE FUNCTION ORD(RV(1:1)) TO O.
           MOVE FUNCTION ORD(RV(2:1)) TO O2.
           MOVE FUNCTION ORD(RV(3:1)) TO O3.
           DISPLAY "W " O " " O2 " " O3.
           CLOSE FV.
           STOP RUN.
