      *>--- conformance-case
      *> id: pb1907_unstring_overlap_delimiter
      *> rules: [GR-14.9.48.4-18]
      *> accept: [2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> PB1907 -- UNSTRING overlap: identifier-2 (DELIMITED BY) shares
      *> storage with identifier-4 (INTO).
      *> ISO 14.9.48.4 GR18 / Annex A.2 item 61: the result is
      *> undefined; 4.4 2): a run unit that allows it is conforming.
      *> Documented implementor choice (docs/CONFORMANCE.md D-UNS2,
      *> GnuCOBOL 3.2 libcob strings.c: cob_unstring_delimited keeps
      *> the delimiter's storage; cob_unstring_into compares against
      *> it live and sets DELIMITER IN from it AFTER the INTO store):
      *> a delimiter identifier is read from storage for each
      *> receiving area, and DELIMITER IN receives the delimiter
      *> item's content as it stands after that receiver's INTO.
      *> B1 first area: "," found after ";", so ";" -> WS-DLM; WS-D1
      *>   then receives WS-DLM's content, ";".
      *> B1 second area: the delimiter is now ";": "a" -> WS-P2,
      *>   D2 ";".
      *> B1 third area: "b,c" holds no ";": WS-P3 = "b,c". T = 3.
      *> (A snapshot would give D1=[,] P2=[a;b] D2=[,] P3=[c  ].)
      *> The ALL run (GR7: contiguous repeats of the delimiter act as
      *> one occurrence) is consumed AFTER the receiving area's INTO
      *> store and before DELIMITER IN / COUNT IN, against the
      *> sender and delimiter as that store left them (libcob
      *> cob_unstring_into: cob_memcpy into the receiver, then the
      *> repeat loop over the delimiter item's own storage).
      *> B2: DELIMITED BY ALL WS-D3 INTO WS-D3. The first area "a"
      *>   overwrites WS-D3 with "a"; the run after the first ";" is
      *>   then measured against "a", so the second ";" of "a;;b"
      *>   is NOT skipped; the next scan runs with the delimiter "a"
      *>   over ";b;;;;c;", finds none, and WS-Y2 takes it all.
      *>   (Consuming the run before the store would give
      *>   Y2=[b;;;;c;  ].) T = 2.
      *> B3: the run follows the store: "a;aab", ALL WS-D4, first
      *>   INTO WS-D4 makes the delimiter "a", so the "aa" after the
      *>   first ";" IS the run and WS-Z2 starts at "b". POINTER 6.
      *> B4: the INTO store reaches the SENDER: WS-Q2 is the third
      *>   character of WS-REC, so "a" replaces the second ";" of
      *>   "a;;;b;;c;;xx" before the run is measured; the run is
      *>   empty and WS-Y5 takes the "a", WS-Y6 the "b".
      *> B5: a two-character ALL delimiter overwritten by the second
      *>   area: the first "--" is consumed, WS-D6 becomes "b " from
      *>   the second area, so the rest of the run after "b" ("--")
      *>   is NOT skipped and the third scan, for "b ", finds none:
      *>   WS-B6 takes the next six characters "--c---". T = 3.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1907U2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-S   PIC X(7) VALUE ";,a;b,c".
       01 WS-DLM PIC X    VALUE ",".
       01 WS-D1  PIC X    VALUE "z".
       01 WS-D2  PIC X    VALUE "z".
       01 WS-P2  PIC X(3) VALUE "zzz".
       01 WS-P3  PIC X(3) VALUE "zzz".
       01 WS-T   PIC 99   VALUE 0.
       01 WS-S3  PIC X(10) VALUE "a;;b;;;;c;".
       01 WS-D3  PIC X    VALUE ";".
       01 WS-Y2  PIC X(9) VALUE "zzzzzzzzz".
       01 WS-Y3  PIC X(3) VALUE "zzz".
       01 WS-T3  PIC 99   VALUE 0.
       01 WS-S4  PIC X(5) VALUE "a;aab".
       01 WS-D4  PIC X    VALUE ";".
       01 WS-Z2  PIC X(3) VALUE "zzz".
       01 WS-Z3  PIC X(3) VALUE "zzz".
       01 WS-T4  PIC 99   VALUE 0.
       01 WS-P4  PIC 99   VALUE 1.
       01 WS-REC PIC X(12).
       01 WS-VW REDEFINES WS-REC.
          05 WS-Q1 PIC X(2).
          05 WS-Q2 PIC X(1).
          05 WS-Q3 PIC X(9).
       01 WS-D5  PIC X    VALUE ";".
       01 WS-Y5  PIC X(5) VALUE "zzzzz".
       01 WS-Y6  PIC X(5) VALUE "zzzzz".
       01 WS-T5  PIC 99   VALUE 0.
       01 WS-S6  PIC X(13) VALUE "a--b----c---d".
       01 WS-D6  PIC XX   VALUE "--".
       01 WS-A6  PIC XX   VALUE "zz".
       01 WS-B6  PIC X(6) VALUE "zzzzzz".
       01 WS-C6  PIC X(6) VALUE "zzzzzz".
       01 WS-T6  PIC 99   VALUE 0.
       PROCEDURE DIVISION.
           UNSTRING WS-S DELIMITED BY WS-DLM
               INTO WS-DLM DELIMITER IN WS-D1
                    WS-P2  DELIMITER IN WS-D2
                    WS-P3
               TALLYING IN WS-T
           END-UNSTRING
           DISPLAY "B1 DLM=[" WS-DLM "] D1=[" WS-D1 "] P2=[" WS-P2
               "] D2=[" WS-D2 "] P3=[" WS-P3 "] T=" WS-T
           UNSTRING WS-S3 DELIMITED BY ALL WS-D3
               INTO WS-D3 WS-Y2 WS-Y3 TALLYING IN WS-T3
           END-UNSTRING
           DISPLAY "B2 D3=[" WS-D3 "] Y2=[" WS-Y2 "] Y3=[" WS-Y3
               "] T=" WS-T3
           UNSTRING WS-S4 DELIMITED BY ALL WS-D4
               INTO WS-D4 WS-Z2 WS-Z3
               WITH POINTER WS-P4 TALLYING IN WS-T4
           END-UNSTRING
           DISPLAY "B3 D4=[" WS-D4 "] Z2=[" WS-Z2 "] Z3=[" WS-Z3
               "] T=" WS-T4 " P=" WS-P4
           MOVE "a;;;b;;c;;xx" TO WS-REC
           UNSTRING WS-REC DELIMITED BY ALL WS-D5
               INTO WS-Q2 WS-Y5 WS-Y6 TALLYING IN WS-T5
           END-UNSTRING
           DISPLAY "B4 REC=[" WS-REC "] Y5=[" WS-Y5 "] Y6=[" WS-Y6
               "] T=" WS-T5
           UNSTRING WS-S6 DELIMITED BY ALL WS-D6
               INTO WS-A6 WS-D6 WS-B6 WS-C6 TALLYING IN WS-T6
           END-UNSTRING
           DISPLAY "B5 D6=[" WS-D6 "] A6=[" WS-A6 "] B6=[" WS-B6
               "] C6=[" WS-C6 "] T=" WS-T6
           STOP RUN.
