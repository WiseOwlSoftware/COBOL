      *>--- conformance-case
      *> id: pb1908_string_overlap_pointer
      *> rules: [GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908 -- STRING overlap: the POINTER item (identifier-4)
      *> shares storage with the receiving item (identifier-3), or
      *> with a sending item.
      *> ISO 14.9.43.4 GR10 / Annex A.2 item 60: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-STR3,
      *> GnuCOBOL 3.2 libcob strings.c: cob_string_init reads the
      *> pointer ONCE, cob_string_finish stores it LAST): the
      *> pointer's initial value governs the whole statement,
      *> stores into the pointer item during it do not move the
      *> transfer, and the final pointer value overwrites them.
      *> P1: WS-P1 (1) is the first two bytes of WS-G1; "WXYZ" is
      *>     moved from position 1, over the pointer; the pointer
      *>     is then stored: 05. Result 05YZcdefghij, pointer 05.
      *> P2: WS-P2 (3) is bytes 3-4 of WS-G2; "ab" fills 3-4 (the
      *>     pointer item), "cd" fills 5-6 although the item now
      *>     holds "ab"; the pointer is then stored: 07.
      *>     Result ..07cd.., pointer 07.
      *> P3: the pointer is also a sender: WS-P3 (1) is bytes 1-2
      *>     of WS-G3; "AB" fills 1-2, so the sender WS-P3 is then
      *>     "AB" and fills 3-4; the pointer is then stored: 05.
      *>     Result 05ABefgh. (A snapshot sender gives 0501efgh.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S3.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-G1.
          05 WS-P1 PIC 99 VALUE 1.
          05 WS-F1 PIC X(10) VALUE "abcdefghij".
       01 WS-G2.
          05 WS-F2 PIC XX VALUE "..".
          05 WS-P2 PIC 99 VALUE 3.
          05 WS-F3 PIC XX VALUE "..".
          05 WS-F4 PIC XX VALUE "..".
       01 WS-G3.
          05 WS-P3 PIC 99 VALUE 1.
          05 WS-F5 PIC X(6) VALUE "cdefgh".
       PROCEDURE DIVISION.
           STRING "WXYZ" DELIMITED BY SIZE
               INTO WS-G1 WITH POINTER WS-P1
           END-STRING
           DISPLAY "P1 [" WS-G1 "] P=" WS-P1
           STRING "ab" DELIMITED BY SIZE
                  "cd" DELIMITED BY SIZE
               INTO WS-G2 WITH POINTER WS-P2
           END-STRING
           DISPLAY "P2 [" WS-G2 "] P=" WS-P2
           STRING "AB" DELIMITED BY SIZE
                  WS-P3 DELIMITED BY SIZE
               INTO WS-G3 WITH POINTER WS-P3
           END-STRING
           DISPLAY "P3 [" WS-G3 "] P=" WS-P3
           STOP RUN.
