      *>--- conformance-case
      *> id: g2idf01_function_identifier_sending
      *> rules: [2002-SR-8.4.2.1.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.1.2 SR2 - "Function-identifier-1 is defined by
      *> 8.4.2.2, Function-identifier": Format 1 makes a
      *> function-identifier an identifier, so it stands wherever an
      *> identifier is permitted that is not a receiving operand
      *> (§2002:8.4.2.2.2 SR1 "A function-identifier shall not be
      *> specified as a receiving operand"). Its value is the temporary
      *> item the function returns (§2002:8.4.2.2.3 GR1).
      *> Each leg puts FUNCTION UPPER-CASE / LOWER-CASE in a SENDING
      *> identifier position and can fail by refusing the form or by
      *> using the argument rather than the function result:
      *>   WRITE ... FROM  UPPER-CASE("abcdefgh") -> "WRITE-FROM=ABCDEFGH"
      *>   INITIALIZE ... REPLACING ALPHANUMERIC BY  -> "INIT-BY=ABCDEFGH"
      *>   INSPECT <fn> TALLYING for "C" then "D": the upper-cased
      *>     value holds one of each, the lower-case argument none
      *>                                            -> "INSPECT-TALLY=002"
      *>   RELEASE ... FROM LOWER-CASE("ABCDEFGH")  -> "RELEASE-FROM=abcdefgh"
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IDF01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F ASSIGN TO "G2IDF01A.DAT"
               ORGANIZATION IS SEQUENTIAL.
           SELECT SF ASSIGN TO "G2IDF01S.TMP".
           SELECT OF1 ASSIGN TO "G2IDF01B.DAT"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F.
       01  FREC PIC X(8).
       SD  SF.
       01  SREC PIC X(8).
       FD  OF1.
       01  OREC PIC X(8).
       WORKING-STORAGE SECTION.
       01  A   PIC X(8) VALUE "abcdefgh".
       01  T   PIC X(8).
       01  CNT PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F.
           WRITE FREC FROM FUNCTION UPPER-CASE(A).
           CLOSE F.
           OPEN INPUT F.
           READ F INTO T.
           CLOSE F.
           DISPLAY "WRITE-FROM=" T.
           MOVE SPACES TO T.
           INITIALIZE T REPLACING ALPHANUMERIC BY
               FUNCTION UPPER-CASE(A).
           DISPLAY "INIT-BY=" T.
           MOVE 0 TO CNT.
           INSPECT FUNCTION UPPER-CASE(A) TALLYING CNT FOR ALL "C".
           INSPECT FUNCTION UPPER-CASE(A) TALLYING CNT FOR ALL "D".
           DISPLAY "INSPECT-TALLY=" CNT.
           SORT SF ASCENDING KEY SREC
               INPUT PROCEDURE IS FEED-SORT
               GIVING OF1.
           OPEN INPUT OF1.
           READ OF1 INTO T.
           CLOSE OF1.
           DISPLAY "RELEASE-FROM=" T.
           STOP RUN.
       FEED-SORT.
           RELEASE SREC FROM FUNCTION LOWER-CASE("ABCDEFGH").
