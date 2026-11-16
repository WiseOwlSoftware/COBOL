      *>--- conformance-case
      *> id: g2_write_indexed_keys
      *> rules: [2002-GR-14.8.47.3-30, 2002-GR-14.8.47.3-32]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.47.3 GR30 - "Successful execution of a WRITE
      *> statement causes the content of the record area to be released.
      *> The operating environment utilizes the contents of the record
      *> keys in such a way that subsequent access of the record may be
      *> made based upon any of these specified record keys." Three
      *> records written (any order, dynamic access: GR35) are found by
      *> the prime key and by the alternate key, randomly (RND-*) and
      *> sequentially in each key's order (PRI-n, ALT-n).
      *> §2002:14.8.47.3 GR32 - "The value of the prime record key shall
      *> not be equal to the value of the prime record key of any record
      *> existing in the file." A WRITE whose prime key K01 exists fails
      *> with '22' (GR37b) and, by GR13, "the write operation does not
      *> take place": K01 still holds BBBB and alternate B09 is absent
      *> (DUP-WRITE 22, RND-K01, ALT-n, COUNTs of 3).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2WR05.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT XF ASSIGN TO "g2wr05.tmp" ORGANIZATION INDEXED
               ACCESS MODE IS DYNAMIC RECORD KEY IS XK
               ALTERNATE RECORD KEY IS XA
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD XF.
       01 XR.
          05 XK PIC X(3).
          05 XA PIC X(3).
          05 XD PIC X(4).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       01 N  PIC 9 VALUE 0.
       01 EOF-SW PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT XF
           WRITE XR FROM "K02B01AAAA"
           DISPLAY "WRITE-K02 " FS
           WRITE XR FROM "K01B03BBBB"
           DISPLAY "WRITE-K01 " FS
           WRITE XR FROM "K03B02CCCC"
           DISPLAY "WRITE-K03 " FS
           WRITE XR FROM "K01B09XXXX"
           DISPLAY "DUP-WRITE " FS
           CLOSE XF
           OPEN INPUT XF
           MOVE "K01" TO XK
           READ XF KEY IS XK
           DISPLAY "RND-K01 " FS " " XR
           MOVE "B02" TO XA
           READ XF KEY IS XA
           DISPLAY "RND-B02 " FS " " XR
           MOVE "B09" TO XA
           READ XF KEY IS XA
               INVALID KEY DISPLAY "RND-B09 " FS
           END-READ
           MOVE LOW-VALUES TO XK
           START XF KEY IS NOT LESS THAN XK
           MOVE "N" TO EOF-SW MOVE 0 TO N
           PERFORM UNTIL EOF-SW = "Y"
               READ XF NEXT RECORD
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END ADD 1 TO N DISPLAY "PRI-" N "=" XR
               END-READ
           END-PERFORM
           DISPLAY "PRI-COUNT=" N
           MOVE LOW-VALUES TO XA
           START XF KEY IS NOT LESS THAN XA
           MOVE "N" TO EOF-SW MOVE 0 TO N
           PERFORM UNTIL EOF-SW = "Y"
               READ XF NEXT RECORD
                   AT END MOVE "Y" TO EOF-SW
                   NOT AT END ADD 1 TO N DISPLAY "ALT-" N "=" XR
               END-READ
           END-PERFORM
           DISPLAY "ALT-COUNT=" N
           CLOSE XF
           STOP RUN.
