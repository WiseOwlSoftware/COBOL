      *>--- conformance-case
      *> id: g2002_cd_record_binary_ranges
      *> rules: [2002-GR-13.16.58.3-11]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.58.3 GR11 - "Any integer value of n within the
      *> specified range shall be expressible in the given usage", with
      *> the minimum ranges of its table, and for signed items "any
      *> number that may be expressed as BINARY-CHAR shall also be
      *> expressible as BINARY-SHORT", and so on up to BINARY-DOUBLE.
      *> The items are the record of an output CD, an ordinary record
      *> description (§2002:A.1.2.2 SR9); working storage is shown at
      *> 2002 by cases/13.18.60-USAGE/binary_usage.
      *> The table prints a strict "<" on each signed lower bound, so
      *> the signed lower end of a minimum range is -2**(w-1)+1.
      *> Each line stores both ends of one minimum range and compares
      *> each item with the literal it received: OK when both hold.
      *>  CS -127 / 127            CU 0 / 255
      *>  SS -32767 / 32767        SU 0 / 65535
      *>  LS -2147483647 / 2147483647
      *>  LU 0 / 4294967295
      *>  DS -9223372036854775807 / 9223372036854775807
      *>  DU 0 / 18446744073709551615
      *> NEST: -127 and 127 held in BINARY-CHAR SIGNED, moved to SHORT,
      *> then LONG, then DOUBLE, are still -127 and 127.
      *> A line fails if an item cannot hold an end of its range.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2UA07.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CO FOR OUTPUT.
       01  CR.
           05 CS1 USAGE BINARY-CHAR SIGNED.
           05 CS2 USAGE BINARY-CHAR SIGNED.
           05 CU1 USAGE BINARY-CHAR UNSIGNED.
           05 CU2 USAGE BINARY-CHAR UNSIGNED.
           05 SS1 USAGE BINARY-SHORT SIGNED.
           05 SS2 USAGE BINARY-SHORT SIGNED.
           05 SU1 USAGE BINARY-SHORT UNSIGNED.
           05 SU2 USAGE BINARY-SHORT UNSIGNED.
           05 LS1 USAGE BINARY-LONG SIGNED.
           05 LS2 USAGE BINARY-LONG SIGNED.
           05 LU1 USAGE BINARY-LONG UNSIGNED.
           05 LU2 USAGE BINARY-LONG UNSIGNED.
           05 DS1 USAGE BINARY-DOUBLE SIGNED.
           05 DS2 USAGE BINARY-DOUBLE SIGNED.
           05 DU1 USAGE BINARY-DOUBLE UNSIGNED.
           05 DU2 USAGE BINARY-DOUBLE UNSIGNED.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -127 TO CS1
           MOVE 127 TO CS2
           IF CS1 = -127 AND CS2 = 127
               DISPLAY "CS OK" ELSE DISPLAY "CS BAD" END-IF
           MOVE 0 TO CU1
           MOVE 255 TO CU2
           IF CU1 = 0 AND CU2 = 255
               DISPLAY "CU OK" ELSE DISPLAY "CU BAD" END-IF
           MOVE -32767 TO SS1
           MOVE 32767 TO SS2
           IF SS1 = -32767 AND SS2 = 32767
               DISPLAY "SS OK" ELSE DISPLAY "SS BAD" END-IF
           MOVE 0 TO SU1
           MOVE 65535 TO SU2
           IF SU1 = 0 AND SU2 = 65535
               DISPLAY "SU OK" ELSE DISPLAY "SU BAD" END-IF
           MOVE -2147483647 TO LS1
           MOVE 2147483647 TO LS2
           IF LS1 = -2147483647 AND LS2 = 2147483647
               DISPLAY "LS OK" ELSE DISPLAY "LS BAD" END-IF
           MOVE 0 TO LU1
           MOVE 4294967295 TO LU2
           IF LU1 = 0 AND LU2 = 4294967295
               DISPLAY "LU OK" ELSE DISPLAY "LU BAD" END-IF
           MOVE -9223372036854775807 TO DS1
           MOVE 9223372036854775807 TO DS2
           IF DS1 = -9223372036854775807
              AND DS2 = 9223372036854775807
               DISPLAY "DS OK" ELSE DISPLAY "DS BAD" END-IF
           MOVE 0 TO DU1
           MOVE 18446744073709551615 TO DU2
           IF DU1 = 0 AND DU2 = 18446744073709551615
               DISPLAY "DU OK" ELSE DISPLAY "DU BAD" END-IF
           MOVE CS1 TO SS1
           MOVE CS2 TO SS2
           MOVE SS1 TO LS1
           MOVE SS2 TO LS2
           MOVE LS1 TO DS1
           MOVE LS2 TO DS2
           IF DS1 = -127 AND DS2 = 127
               DISPLAY "NEST OK" ELSE DISPLAY "NEST BAD" END-IF
           STOP RUN.
