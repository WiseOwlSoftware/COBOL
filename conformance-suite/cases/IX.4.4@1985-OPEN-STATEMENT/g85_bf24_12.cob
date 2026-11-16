      *>--- conformance-case
      *> id: g85_bf24_12
      *> rules: [1985-GR-IX.4.4.4-10]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IX.4.4.4 GR10 - "When files are opened with the INPUT or
      *> I-O phrase, the file position indicator is set to the
      *> characters that have the lowest ordinal position in the
      *> collating sequence associated with the file, and the prime
      *> record key is established as the key of reference."
      *> Records written K005 (alt A), K003 (alt C), K007 (alt B):
      *> dynamic access, any order (§1985:IX.4.9.4 GR15), so "first
      *> written" (K005) and "lowest prime key" (K003) differ, and the
      *> alternate order (A B C = K005 K007 K003) differs from the
      *> prime order (K003 K005 K007).
      *>   OPEN INPUT, READ NEXT: X1=K003 (lowest prime key).
      *>   START on the alternate key =B makes it the key of reference
      *>   (§1985:IX.4.7.4 GR10): X2=K007, X3=K003 (alternate order).
      *>   CLOSE, OPEN INPUT: X4=K003, X5=K005 - prime order, so the
      *>   OPEN re-established the PRIME key (under the alternate key
      *>   the walk would start K005, K007).
      *>   CLOSE, OPEN I-O: X6=K003 - the I-O phrase does the same.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85BF2412.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FX ASSIGN TO "g85bf2412.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS X-KEY
               ALTERNATE RECORD KEY IS X-ALT
               FILE STATUS IS ST-X.
       DATA DIVISION.
       FILE SECTION.
       FD  FX.
       01  X-REC.
           05 X-KEY PIC X(4).
           05 X-ALT PIC X(1).
           05 X-VAL PIC X(3).
       WORKING-STORAGE SECTION.
       01  ST-X PIC XX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FX.
           MOVE "K005AV05" TO X-REC.
           WRITE X-REC INVALID KEY DISPLAY "W1-INVALID".
           MOVE "K003CV03" TO X-REC.
           WRITE X-REC INVALID KEY DISPLAY "W2-INVALID".
           MOVE "K007BV07" TO X-REC.
           WRITE X-REC INVALID KEY DISPLAY "W3-INVALID".
           CLOSE FX.
           OPEN INPUT FX.
           READ FX NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "X1=" X-KEY.
           MOVE "B" TO X-ALT.
           START FX KEY IS EQUAL TO X-ALT
               INVALID KEY DISPLAY "START-INVALID".
           DISPLAY "XSTART=" ST-X.
           READ FX NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "X2=" X-KEY.
           READ FX NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "X3=" X-KEY.
           CLOSE FX.
           OPEN INPUT FX.
           READ FX NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "X4=" X-KEY.
           READ FX NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "X5=" X-KEY.
           CLOSE FX.
           OPEN I-O FX.
           READ FX NEXT RECORD AT END DISPLAY "EOF-EARLY".
           DISPLAY "X6=" X-KEY.
           CLOSE FX.
           STOP RUN.
