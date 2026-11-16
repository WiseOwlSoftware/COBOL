      *>--- conformance-case
      *> id: g85_bf21_07
      *> rules: [1985-GR-IX.2.3.4-8]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.2.3.4 GR8 - the ACCESS MODE, ALTERNATE RECORD KEY,
      *> ORGANIZATION IS INDEXED and RECORD KEY clauses of the indexed
      *> file control entry, in the forms the following pages present:
      *> ORGANIZATION IS INDEXED and the bare INDEXED; ACCESS MODE IS
      *> RANDOM, DYNAMIC (IS and MODE omitted) and the clause omitted;
      *> RECORD KEY with and without IS; two ALTERNATE RECORD KEY
      *> clauses, one WITH DUPLICATES. One physical file is written in
      *> random mode out of key order and read back through each entry:
      *> §1985:IX.2.4.4 GR1, GR2 (omitted clause: sequential, ascending
      *> prime key: "SEQ" lines); §1985:IX.4.5.4 GR15, GR17, GR4e
      *> (dynamic, alternate key with duplicates: first duplicate
      *> released, then the next duplicate, then the next key: "DYN"
      *> lines); GR15, GR16, GR17 (random: by the unique alternate key,
      *> by the prime key, and a missing key: "RND" lines). A refused
      *> form, a wrong default access mode or a key bound to the wrong
      *> positions changes the output.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2107.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F-RND ASSIGN TO "BF2107.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS RANDOM
               RECORD KEY IS R-KEY
               ALTERNATE RECORD KEY IS R-ALT WITH DUPLICATES
               ALTERNATE RECORD KEY IS R-ALT2.
           SELECT F-SEQ ASSIGN TO "BF2107.DAT"
               INDEXED
               RECORD S-KEY
               ALTERNATE RECORD S-ALT DUPLICATES
               ALTERNATE RECORD KEY S-ALT2.
           SELECT F-DYN ASSIGN TO "BF2107.DAT"
               ALTERNATE RECORD KEY IS D-ALT WITH DUPLICATES
               ACCESS DYNAMIC
               ALTERNATE RECORD KEY IS D-ALT2
               RECORD KEY IS D-KEY
               ORGANIZATION INDEXED.
       DATA DIVISION.
       FILE SECTION.
       FD  F-RND.
       01  R-REC.
           05  R-KEY            PIC X(3).
           05  R-ALT            PIC XX.
           05  R-ALT2           PIC X(3).
           05  R-DATA           PIC X(5).
       FD  F-SEQ.
       01  S-REC.
           05  S-KEY            PIC X(3).
           05  S-ALT            PIC XX.
           05  S-ALT2           PIC X(3).
           05  S-DATA           PIC X(5).
       FD  F-DYN.
       01  D-REC.
           05  D-KEY            PIC X(3).
           05  D-ALT            PIC XX.
           05  D-ALT2           PIC X(3).
           05  D-DATA           PIC X(5).
       WORKING-STORAGE SECTION.
       01  WS-EOF               PIC X VALUE "N".
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT F-RND.
           MOVE "C03XX333CHAR" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WC INVALID".
           MOVE "A01YY111ALF" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WA INVALID".
           MOVE "B02XX222BRAV" TO R-REC.
           WRITE R-REC INVALID KEY DISPLAY "WB INVALID".
           CLOSE F-RND.
           OPEN INPUT F-SEQ.
           PERFORM READ-SEQ UNTIL WS-EOF = "Y".
           CLOSE F-SEQ.
           OPEN INPUT F-DYN.
           MOVE "XX" TO D-ALT.
           READ F-DYN KEY IS D-ALT
               INVALID KEY DISPLAY "DYN ALT XX INVALID".
           DISPLAY "DYN ALT " D-KEY " [" D-DATA "]".
           MOVE "N" TO WS-EOF.
           PERFORM READ-DYN UNTIL WS-EOF = "Y".
           CLOSE F-DYN.
           OPEN INPUT F-RND.
           MOVE "222" TO R-ALT2.
           READ F-RND KEY IS R-ALT2
               INVALID KEY DISPLAY "RND ALT2 INVALID".
           DISPLAY "RND ALT2 " R-KEY " [" R-DATA "]".
           MOVE "C03" TO R-KEY.
           READ F-RND INVALID KEY DISPLAY "RND C03 INVALID".
           DISPLAY "RND KEY " R-KEY " [" R-DATA "]".
           MOVE "Z99" TO R-KEY.
           READ F-RND
               INVALID KEY DISPLAY "RND Z99 INVALID"
           END-READ.
           CLOSE F-RND.
           STOP RUN.
       READ-SEQ.
           READ F-SEQ
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "SEQ END"
               NOT AT END
                   DISPLAY "SEQ " S-KEY " [" S-DATA "]"
           END-READ.
       READ-DYN.
           READ F-DYN NEXT
               AT END
                   MOVE "Y" TO WS-EOF
                   DISPLAY "DYN END"
               NOT AT END
                   DISPLAY "DYN NEXT " D-KEY " " D-ALT " [" D-DATA "]"
           END-READ.
