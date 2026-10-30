      *>--- conformance-case
      *> id: pb1908_string_overlap_sender
      *> rules: [GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908 -- STRING overlap: a SENDING operand (identifier-1)
      *> shares storage with the receiving item (identifier-3).
      *> ISO 14.9.43.4 GR10 / Annex A.2 item 60: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-STR1,
      *> GnuCOBOL 3.2 libcob strings.c cob_string_append moves in
      *> place and reads each sender when its transfer begins): a
      *> sender some earlier transfer stored into is read from the
      *> storage as that transfer left it. Each leg also names the
      *> SNAPSHOT result (every sender read before the first
      *> character moves), which differs.
      *> S1: "XY" fills 1-2; WS-R1(1:2) is then "XY" and fills 3-4;
      *>     WS-R1(3:2) is then "XY" and fills 5-6.
      *>     Result XYXYXYGHIJKL. Snapshot: XYABCDGHIJKL.
      *> S2: the table WS-T holds three X(3) elements, ZZZ except
      *>     WS-TE(2) = YYY; "123" fills WS-TE(1); the sender
      *>     WS-TE(1) is then "123" and fills WS-TE(2).
      *>     Result 123123ZZZ. Snapshot: 123ZZZZZZ.
      *> S3: the numeric DISPLAY item WS-N (123456) is sender and
      *>     receiver; "99" fills 1-2, WS-N is then 993456 and its
      *>     first four characters (9934) fill 3-6; the sender is
      *>     longer than the room left, so GR8's overflow runs ON
      *>     OVERFLOW. Result 999934. Snapshot: 991234.
      *> S4: the group WS-G (eight characters) is the sender and
      *>     its first four characters WS-GA are the receiver; "ab"
      *>     fills WS-GA(1:2), WS-G is then abAABBBB and its first
      *>     two characters (ab) fill WS-GA(3:2); overflow (GR8).
      *>     Result WS-G = ababBBBB. Snapshot: abAABBBB.
      *> S5: ONE sender overlaps the positions its own transfer
      *>     writes: WS-R5(1:6) fills WS-R5 from position 3 (POINTER
      *>     3). The library moves the sender as it stood when that
      *>     transfer began (C memcpy over overlapping storage is
      *>     not a rule of COBOL; glibc agrees); so does WiseOwl.
      *>     Result ABABCDEFIJKL, pointer 09.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-R1  PIC X(12) VALUE "ABCDEFGHIJKL".
       01 WS-T.
          05 WS-TE PIC X(3) OCCURS 3 VALUE "ZZZ".
       01 WS-I   PIC 9 VALUE 1.
       01 WS-N   PIC 9(6) VALUE 123456.
       01 WS-G.
          05 WS-GA PIC X(4) VALUE "AAAA".
          05 WS-GB PIC X(4) VALUE "BBBB".
       01 WS-R5  PIC X(12) VALUE "ABCDEFGHIJKL".
       01 WS-P   PIC 99 VALUE 3.
       PROCEDURE DIVISION.
           STRING "XY" DELIMITED BY SIZE
                  WS-R1(1:2) DELIMITED BY SIZE
                  WS-R1(3:2) DELIMITED BY SIZE
               INTO WS-R1
           END-STRING
           DISPLAY "S1 [" WS-R1 "]"
           MOVE "YYY" TO WS-TE(2)
           STRING "123" DELIMITED BY SIZE
                  WS-TE(WS-I) DELIMITED BY SIZE
               INTO WS-T
           END-STRING
           DISPLAY "S2 [" WS-T "]"
           STRING "99" DELIMITED BY SIZE
                  WS-N DELIMITED BY SIZE
               INTO WS-N
               ON OVERFLOW DISPLAY "S3 OVERFLOW"
           END-STRING
           DISPLAY "S3 [" WS-N "]"
           STRING "ab" DELIMITED BY SIZE
                  WS-G DELIMITED BY SIZE
               INTO WS-GA
               ON OVERFLOW DISPLAY "S4 OVERFLOW"
           END-STRING
           DISPLAY "S4 [" WS-G "]"
           STRING WS-R5(1:6) DELIMITED BY SIZE
               INTO WS-R5 WITH POINTER WS-P
           END-STRING
           DISPLAY "S5 [" WS-R5 "] P=" WS-P
           STOP RUN.
