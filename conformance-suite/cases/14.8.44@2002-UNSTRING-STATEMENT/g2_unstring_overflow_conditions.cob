      *>--- conformance-case
      *> id: g2_unstring_overflow_conditions
      *> rules: [2002-GR-14.8.44.3-15]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.44.3 GR15 - "Either of the following situations
      *>   causes an overflow condition": a) "the value in the data item
      *>   referenced by identifier-7 is less than 1 or greater than the
      *>   number of character positions described for the data item
      *>   referenced by identifier-1"; b) "all receiving areas have been
      *>   acted upon, and the data item referenced by identifier-1
      *>   contains characters that have not been examined".
      *> The observable consequence is GR16a ("The UNSTRING operation is
      *>   terminated") and GR16c (ON OVERFLOW runs); when neither
      *>   situation arises GR17 runs NOT ON OVERFLOW. The pointer moves
      *>   by the characters examined (GR13) and TALLYING counts the
      *>   receivers accessed (GR14).
      *> Legs (S = "AB,CD,EF,G", 10 positions):
      *>   A0  pointer 0       -> GR15a, nothing moved, P and T unchanged.
      *>   A11 pointer 11 > 10 -> GR15a, nothing moved.
      *>   A10 pointer 10 = 10 -> not GR15a: "G" moved, P 11, NOT ON.
      *>   B   two receivers, "EF,G" left unexamined -> GR15b, P 7, T 2.
      *>   B2  "AB,CD," into two receivers: the trailing delimiter is
      *>       examined, nothing is left -> no overflow, P 7, NOT ON.
      *>   B3  "AB,CD" into two receivers: exhausted -> no overflow, P 6.
      *> Each leg can fail: an off-by-one bound in GR15a flips A10 or
      *>   A11; a move before the bound check alters R1 in A0/A11; a test
      *>   of "receivers exhausted" alone (not unexamined characters)
      *>   flips B2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UNS02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 S  PIC X(10) VALUE "AB,CD,EF,G".
       01 S2 PIC X(6)  VALUE "AB,CD,".
       01 S3 PIC X(5)  VALUE "AB,CD".
       01 R1 PIC X(4).
       01 R2 PIC X(4).
       01 P  PIC 99.
       01 T  PIC 99.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE "####" TO R1 MOVE 0 TO P MOVE 0 TO T
           UNSTRING S DELIMITED BY "," INTO R1
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "A0 OVF " P " " T " [" R1 "]"
               NOT ON OVERFLOW DISPLAY "A0 NOOVF"
           END-UNSTRING
           MOVE "####" TO R1 MOVE 11 TO P MOVE 0 TO T
           UNSTRING S DELIMITED BY "," INTO R1
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "A11 OVF " P " " T " [" R1 "]"
               NOT ON OVERFLOW DISPLAY "A11 NOOVF"
           END-UNSTRING
           MOVE "####" TO R1 MOVE 10 TO P MOVE 0 TO T
           UNSTRING S DELIMITED BY "," INTO R1
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "A10 OVF"
               NOT ON OVERFLOW
                   DISPLAY "A10 NOOVF " P " " T " [" R1 "]"
           END-UNSTRING
           MOVE "####" TO R1 R2 MOVE 1 TO P MOVE 0 TO T
           UNSTRING S DELIMITED BY "," INTO R1 R2
               WITH POINTER P TALLYING IN T
               ON OVERFLOW
                   DISPLAY "B OVF " P " " T " [" R1 "][" R2 "]"
               NOT ON OVERFLOW DISPLAY "B NOOVF"
           END-UNSTRING
           MOVE "####" TO R1 R2 MOVE 1 TO P MOVE 0 TO T
           UNSTRING S2 DELIMITED BY "," INTO R1 R2
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "B2 OVF"
               NOT ON OVERFLOW
                   DISPLAY "B2 NOOVF " P " " T " [" R1 "][" R2 "]"
           END-UNSTRING
           MOVE "####" TO R1 R2 MOVE 1 TO P MOVE 0 TO T
           UNSTRING S3 DELIMITED BY "," INTO R1 R2
               WITH POINTER P TALLYING IN T
               ON OVERFLOW DISPLAY "B3 OVF"
               NOT ON OVERFLOW
                   DISPLAY "B3 NOOVF " P " " T " [" R1 "][" R2 "]"
           END-UNSTRING
           STOP RUN.
