      *>--- conformance-case
      *> id: g2-inspect-replacing-overlap
      *> rules: [2002-GR-14.8.21.3-17]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.21.3 GR17 - identifier-3, identifier-4 or
      *> identifier-5 of a REPLACING occupying the storage of
      *> identifier-1: "the result of the execution of this statement is
      *> undefined" (also §2002:B.2 item 22 e), §2002:3.3). Every
      *> line pins the DOCUMENTED IMPLEMENTOR CHOICE
      *> docs/conformance/3.1/D-INS3
      *> (kb/Work PB1907): image and operand values are taken when the
      *> statement starts and stored once at its end. The non-overlap
      *> parts of each line follow §2002:14.8.21.3 GR7, GR8 and GR16.
      *> Why it can fail: an operand that observes a replacement its own
      *> statement made (E1 RR, E3 a moved delimiter, E5 FIRST on a
      *> replaced character).
      *> EXPECTED (X1 = "QS", X2 = "AB", X3 = "AQQA", R4 = "ABAB" with
      *> R4P its characters 3-4, X9 = "AQAB"):
      *>   E1  ALL "Q" BY "R", ALL "S" BY X1(1:1): the replacement
      *>       X1(1:1) is "Q" as read at the start      [RQ]
      *>   E2  ALL X2 BY "BA": the pattern is "AB"      [BA]
      *>   E3  ALL "A" BY "Z", CHARACTERS BY "*" BEFORE X3(1:1): the
      *>       delimiter is "A" as read at the start, its first
      *>       occurrence is position 1, so CHARACTERS has an empty
      *>       region; ALL "A" is unrestricted         [ZQQZ]
      *>   E4  ALL R4P BY "XY": R4P is "AB", both occurrences are
      *>       replaced from the original image       [XYXY]
      *>   E5  ALL "A" BY "B", FIRST "B" BY X9(1:1): "A" -> "B" at 1
      *>       and 3 matching the original; FIRST "B" is the "B" at 4
      *>       of the original, replaced by X9(1:1) read at the start,
      *>       "A"                                    [BQBA]
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INS03.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 X1   PIC X(2) VALUE "QS".
       01 X2   PIC X(2) VALUE "AB".
       01 X3   PIC X(4) VALUE "AQQA".
       01 R4.
          05 R4A PIC X(4) VALUE "ABAB".
       01 R4R REDEFINES R4.
          05 FILLER PIC X(2).
          05 R4P PIC X(2).
       01 X9   PIC X(4) VALUE "AQAB".
       PROCEDURE DIVISION.
       MAIN.
           INSPECT X1 REPLACING ALL "Q" BY "R" ALL "S" BY X1(1:1)
           DISPLAY "E1=[" X1 "]"
           INSPECT X2 REPLACING ALL X2 BY "BA"
           DISPLAY "E2=[" X2 "]"
           INSPECT X3 REPLACING ALL "A" BY "Z"
                      CHARACTERS BY "*" BEFORE X3(1:1)
           DISPLAY "E3=[" X3 "]"
           INSPECT R4 REPLACING ALL R4P BY "XY"
           DISPLAY "E4=[" R4 "]"
           INSPECT X9 REPLACING ALL "A" BY "B" FIRST "B" BY X9(1:1)
           DISPLAY "E5=[" X9 "]"
           STOP RUN.
