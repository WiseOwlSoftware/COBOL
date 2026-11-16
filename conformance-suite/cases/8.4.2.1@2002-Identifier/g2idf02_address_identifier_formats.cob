      *>--- conformance-case
      *> id: g2idf02_address_identifier_formats
      *> rules: [2002-SR-8.4.2.1.2-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.1.2 SR10 - "Address-identifiers are defined by
      *> 8.4.2.11, Data-address-identifier and 8.4.2.12,
      *> Program-address-identifier." Format 9 has both arms; each is
      *> written below as a sending operand (both are barred as
      *> receiving operands, §2002:8.4.2.11.2 SR5,
      *> §2002:8.4.2.12.2 SR4).
      *> Data-address-identifier (§2002:8.4.2.11.3 GR1: "a unique data
      *> item of class pointer and category data-pointer that contains
      *> the address of identifier-1"):
      *>   P1, P2 both ADDRESS OF X1 -> equal          "DA-EQ"
      *>   P3 = ADDRESS OF Y1        -> differs from P1 "DA-NE"
      *>   BASED L1 set to P1 shows X1's content       "L1=ABCD"
      *> Program-address-identifier (§2002:8.4.2.12.3 GR1 a/b: the
      *> program named by the literal or by the identifier's content):
      *>   SET PP TO ADDRESS OF PROGRAM "G2IDF02S", CALL PP "IN-SUB"
      *>   SET PP TO ADDRESS OF PROGRAM PN (PN = "G2IDF02S"), CALL PP
      *>                                                  "IN-SUB"
      *>   PP is not NULL                                 "PP-SET"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IDF02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  X1 PIC X(4) VALUE "ABCD".
       01  Y1 PIC X(4) VALUE "WXYZ".
       01  P1 USAGE POINTER.
       01  P2 USAGE POINTER.
       01  P3 USAGE POINTER.
       01  PP USAGE PROGRAM-POINTER.
       01  PN PIC X(8) VALUE "G2IDF02S".
       01  L1 PIC X(4) BASED.
       PROCEDURE DIVISION.
       MAIN-PARA.
           SET P1 TO ADDRESS OF X1.
           SET P2 TO ADDRESS OF X1.
           SET P3 TO ADDRESS OF Y1.
           IF P1 = P2
               DISPLAY "DA-EQ"
           ELSE
               DISPLAY "DA-NOT-EQ"
           END-IF.
           IF P1 = P3
               DISPLAY "DA-SAME"
           ELSE
               DISPLAY "DA-NE"
           END-IF.
           SET ADDRESS OF L1 TO P1.
           DISPLAY "L1=" L1.
           SET PP TO ADDRESS OF PROGRAM "G2IDF02S".
           CALL PP.
           SET PP TO ADDRESS OF PROGRAM PN.
           CALL PP.
           IF PP = NULL
               DISPLAY "PP-NULL"
           ELSE
               DISPLAY "PP-SET"
           END-IF.
           STOP RUN.
       END PROGRAM G2IDF02.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IDF02S.
       PROCEDURE DIVISION.
           DISPLAY "IN-SUB".
           EXIT PROGRAM.
       END PROGRAM G2IDF02S.
