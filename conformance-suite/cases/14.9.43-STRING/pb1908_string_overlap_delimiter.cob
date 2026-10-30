      *>--- conformance-case
      *> id: pb1908_string_overlap_delimiter
      *> rules: [GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908 -- STRING overlap: a DELIMITED BY operand
      *> (identifier-2) shares storage with the receiving item
      *> (identifier-3).
      *> ISO 14.9.43.4 GR10 / Annex A.2 item 60: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-STR2,
      *> GnuCOBOL 3.2 libcob strings.c cob_string_delimited keeps
      *> the delimiter's storage and cob_string_append examines it
      *> when the transfer begins): an identifier delimiter an
      *> earlier transfer stored into delimits by what it holds
      *> then. Each leg also names the SNAPSHOT result.
      *> D1: "x" fills WS-R1(1:1), the delimiter WS-R1(1:1) is then
      *>     "x"; "12x34" is cut at its "x": "12" fills 2-3.
      *>     Result x12defghijkl. Snapshot: x12x34ghijkl.
      *> D2: WS-DD is the first byte of the receiving group WS-DG;
      *>     ";" fills it, so "ab;cd" is cut at ";" and "ab" fills
      *>     2-3. Result ;ab....... Snapshot: ;ab;cd....
      *> D3: the delimiter is the two bytes WS-R3(1:2); "ab" fills
      *>     them, so "1ab2" is cut at "ab": "1" fills 3.
      *>     Result ab1....... Snapshot: ab1ab2...
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R1  PIC X(12) VALUE ",bcdefghijkl".
       01 WS-DG.
          05 WS-DD PIC X VALUE ",".
          05 WS-DR PIC X(9) VALUE ".........".
       01 WS-R3  PIC X(10) VALUE "--........".
       PROCEDURE DIVISION.
           STRING "x" DELIMITED BY SIZE
                  "12x34" DELIMITED BY WS-R1(1:1)
               INTO WS-R1
           END-STRING
           DISPLAY "D1 [" WS-R1 "]"
           STRING ";" DELIMITED BY SIZE
                  "ab;cd" DELIMITED BY WS-DD
               INTO WS-DG
           END-STRING
           DISPLAY "D2 [" WS-DG "]"
           STRING "ab" DELIMITED BY SIZE
                  "1ab2" DELIMITED BY WS-R3(1:2)
               INTO WS-R3
           END-STRING
           DISPLAY "D3 [" WS-R3 "]"
           STOP RUN.
