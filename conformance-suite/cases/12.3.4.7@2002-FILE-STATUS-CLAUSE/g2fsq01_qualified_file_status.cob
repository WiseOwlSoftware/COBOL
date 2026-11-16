      *>--- conformance-case
      *> id: g2fsq01_qualified_file_status
      *> rules: [2002-SR-12.3.4.7.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.7.2 SR1 - "Data-name-1 may be qualified."
      *>   cite.py --check "§2002:12.3.4.7.2 SR1" -> OK
      *> The rule GRANTS a form, so this ACCEPTED case writes it twice:
      *> FILE STATUS IS FS OF G2 for F1 and FILE STATUS IS FS OF G1 for
      *> F2, with G1 declared first. Each status must land in the item
      *> its qualifier names and leave the other FS untouched.
      *> §2002:12.3.4.7.3 GR1: "the data item referenced by data-name-1
      *> is updated to contain the value of the I-O status for the file
      *> connector referenced by the subject of the entry"
      *>   -> cite.py OK §2002:12.3.4.7.3 GR1
      *> Derived (G1's FS starts "AA", G2's "BB"; each line can fail):
      *>  O1: OPEN OUTPUT F1 succeeds, "00" (§2002:9.1.12.1) goes to FS
      *>   OF G2 only -> "O1 G1=AA G2=00".
      *>  O2: OPEN INPUT of the OPTIONAL file F2, whose physical file is
      *>   not present: "the file is described as optional and the
      *>   physical file is not present" -> "05" (§2002:9.1.12.1), to FS
      *>   OF G1 only -> "O2 G1=05 G2=00".
      *>  O3: the first READ of F2: "a sequential READ statement is
      *>   attempted for the first time on a file described as optional
      *>   and the physical file is not present" -> "10" (§2002:9.1.12.3)
      *>   -> "O3 G1=10 G2=00".
      *>  O4: FS OF G2 is reset to "BB", then CLOSE F1 succeeds -> "00"
      *>   to FS OF G2; G1 keeps "10" -> "O4 G1=10 G2=00".
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FSQ01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2FSQ01.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS OF G2.
           SELECT OPTIONAL F2 ASSIGN TO "G2FSQ01-ABSENT.DAT"
               ORGANIZATION IS SEQUENTIAL
               FILE STATUS IS FS OF G1.
       DATA DIVISION.
       FILE SECTION.
       FD F1.
       01 R1 PIC X(4).
       FD F2.
       01 R2 PIC X(4).
       WORKING-STORAGE SECTION.
       01 G1.
          05 FS PIC XX VALUE "AA".
       01 G2.
          05 FS PIC XX VALUE "BB".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT F1
           DISPLAY "O1 G1=" FS OF G1 " G2=" FS OF G2
           OPEN INPUT F2
           DISPLAY "O2 G1=" FS OF G1 " G2=" FS OF G2
           READ F2 AT END CONTINUE END-READ
           DISPLAY "O3 G1=" FS OF G1 " G2=" FS OF G2
           MOVE "BB" TO FS OF G2
           CLOSE F1
           DISPLAY "O4 G1=" FS OF G1 " G2=" FS OF G2
           CLOSE F2
           STOP RUN.
