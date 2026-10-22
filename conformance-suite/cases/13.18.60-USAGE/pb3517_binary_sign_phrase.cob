      *>--- conformance-case
      *> id: pb3517_binary_sign_phrase
      *> rules: [FMT-13.18.60.2, GR-13.18.60.4-12]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 13.18.60.2 general format (the printed page, rendered):
      *> BINARY-CHAR, BINARY-SHORT, BINARY-LONG and BINARY-DOUBLE
      *> each take "{ SIGNED | UNSIGNED }", SIGNED not underlined
      *> (an optional word, so no phrase means SIGNED) and
      *> UNSIGNED underlined. 13.18.60.4 GR12: "Any integer value of
      *> n within the specified range shall be expressible in the
      *> given usage", the table printing -2**7 < n < 2**7 for
      *> BINARY-CHAR SIGNED and 0 <= n < 2**8 for BINARY-CHAR
      *> UNSIGNED (the printed page sets the unsigned lower bound in
      *> a symbol font that renders as a pound sign), and so on for
      *> 2**15 / 2**16, 2**31 / 2**32 and 2**63 / 2**64. Each line
      *> below stores a value at an end of its usage's range,
      *> written with the phrase, without it, or with SIGNED
      *> explicitly, and fails if the value is not held exactly
      *> (an UNSIGNED item read as signed wraps negative).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3517P1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 C-S  USAGE IS BINARY-CHAR SIGNED.
       01 C-D  USAGE BINARY-CHAR.
       01 C-U  BINARY-CHAR UNSIGNED.
       01 S-S  USAGE BINARY-SHORT SIGNED.
       01 S-U  USAGE BINARY-SHORT UNSIGNED.
       01 L-S  USAGE BINARY-LONG SIGNED.
       01 L-U  USAGE BINARY-LONG UNSIGNED.
       01 D-S  USAGE BINARY-DOUBLE SIGNED.
       01 D-U  USAGE BINARY-DOUBLE UNSIGNED.
       PROCEDURE DIVISION.
       MAIN.
           MOVE -127 TO C-S
           IF C-S = -127 DISPLAY "C-S OK" ELSE DISPLAY "C-S FAIL".
           MOVE 127 TO C-D
           IF C-D = 127 DISPLAY "C-D OK" ELSE DISPLAY "C-D FAIL".
           MOVE 255 TO C-U
           IF C-U = 255 DISPLAY "C-U OK" ELSE DISPLAY "C-U FAIL".
           MOVE -32767 TO S-S
           IF S-S = -32767 DISPLAY "S-S OK" ELSE DISPLAY "S-S FAIL".
           MOVE 65535 TO S-U
           IF S-U = 65535 DISPLAY "S-U OK" ELSE DISPLAY "S-U FAIL".
           MOVE -2147483647 TO L-S
           IF L-S = -2147483647
               DISPLAY "L-S OK" ELSE DISPLAY "L-S FAIL".
           MOVE 4294967295 TO L-U
           IF L-U = 4294967295
               DISPLAY "L-U OK" ELSE DISPLAY "L-U FAIL".
           MOVE -9223372036854775807 TO D-S
           IF D-S = -9223372036854775807
               DISPLAY "D-S OK" ELSE DISPLAY "D-S FAIL".
           MOVE 18446744073709551615 TO D-U
           IF D-U = 18446744073709551615
               DISPLAY "D-U OK" ELSE DISPLAY "D-U FAIL".
           STOP RUN.
