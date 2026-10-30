      *>--- conformance-case
      *> id: pb1908_string_overlap_linkage
      *> rules: [GR-14.9.43.4-10]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1908 -- STRING overlap through CALL BY REFERENCE: the
      *> sending, delimiter and receiving LINKAGE items are one
      *> storage area.
      *> ISO 14.9.43.4 GR10 / Annex A.2 item 60: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-STR1
      *> and D-STR2; GnuCOBOL 3.2 libcob strings.c moves in place):
      *> the compiler cannot tell where a LINKAGE item lives, so
      *> every STRING that names one reads it at its own transfer.
      *> K1: LK-A and LK-D are the whole of WS-REC, as is LK-R.
      *>     "XY" fills 1-2 (WS-REC = XYCDEFGHIJKL); the sender LK-A
      *>     is then XYCDEFGHIJKL and its first ten characters fill
      *>     3-12 (the library moves the sender as it stood when
      *>     the transfer began); the sender is longer than the
      *>     room left, so ON OVERFLOW runs. Result XYXYCDEFGHIJ.
      *>     (A snapshot gives XYABCDEFGHIJ.)
      *> K2: the delimiter LK-D(1:1) and LK-R2 are the whole of
      *>     WS-RC2 (Xbcdef). "q" fills 1, so "12q34" is cut at
      *>     "q": "12" fills 2-3. Result q12def.
      *>     (A snapshot delimiter "X" gives q12q34.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S4.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-REC PIC X(12) VALUE "ABCDEFGHIJKL".
       01 WS-RC2 PIC X(6) VALUE "Xbcdef".
       PROCEDURE DIVISION.
           CALL "PB1908S4S" USING WS-REC WS-REC WS-RC2 WS-RC2
           DISPLAY "K1 REC=[" WS-REC "]"
           DISPLAY "K2 REC=[" WS-RC2 "]"
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1908S4S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-A   PIC X(12).
       01 LK-R   PIC X(12).
       01 LK-D   PIC X(6).
       01 LK-R2  PIC X(6).
       PROCEDURE DIVISION USING LK-A LK-R LK-D LK-R2.
           STRING "XY" DELIMITED BY SIZE
                  LK-A DELIMITED BY SIZE
               INTO LK-R
               ON OVERFLOW DISPLAY "K1 OVERFLOW"
           END-STRING
           STRING "q" DELIMITED BY SIZE
                  "12q34" DELIMITED BY LK-D(1:1)
               INTO LK-R2
           END-STRING
           GOBACK.
       END PROGRAM PB1908S4S.
       END PROGRAM PB1908S4.
