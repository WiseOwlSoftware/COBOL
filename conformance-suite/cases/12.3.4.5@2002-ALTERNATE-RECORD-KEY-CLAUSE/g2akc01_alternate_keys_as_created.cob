      *>--- conformance-case
      *> id: g2akc01_alternate_keys_as_created
      *> rules: [2002-GR-12.3.4.5.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.5.3 GR3 - "The data description of data-name-1 or
      *> data-name-2 as well as their relative location within a record
      *> shall be the same as that used when the physical file was
      *> created. The number of alternate record keys for the file shall
      *> also be the same as that used when the physical file was
      *> created."  -> cite.py --check "§2002:12.3.4.5.3 GR3" OK
      *> §2002:14.8.26.3 GR8: on OPEN the connector's attributes "are
      *> compared with the fixed file attributes of the physical file";
      *> a mismatch makes the OPEN unsuccessful with '39', the validated
      *> set being the implementor's (cite.py OK). The implementor's set
      *> for INDEXED (docs/conformance DOC-A.1-129 (3)) is the key table:
      *> the number of keys and each key's byte window.
      *> IX-M creates the file: prime M-KEY (bytes 1-4), one alternate
      *> M-ALT (bytes 5-6) WITH DUPLICATES. IX-S states the SAME keys
      *> (GR3 kept); IX-N has no alternate, IX-T two, IX-L the one
      *> alternate at bytes 7-8 (another relative location).
      *> DERIVATION OF THE EXPECTED OUTPUT:
      *>   create, two records (K001/BB, K002/AA)  => MAKE 00 00 00
      *>   IX-S agrees: its OPEN succeeds, READ KEY IS S-ALT "AA" gives K002,
      *>   the alternate key built at creation     => SAME 00 K002
      *>   IX-N, IX-T: the number of alternate keys differs
      *>                                           => NONE 39 / TWO 39
      *>   IX-L: the alternate's location differs  => MOVED 39
      *> A leg fails if the agreeing connector is refused or reads by a
      *> key the file was not built on, or if a differing key table
      *> opens (00) and reads through a key the file never had.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2AKC01.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IX-M ASSIGN TO "G2AKC01.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS M-KEY
               ALTERNATE RECORD KEY IS M-ALT WITH DUPLICATES
               FILE STATUS IS M-ST.
           SELECT IX-S ASSIGN TO "G2AKC01.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS S-KEY
               ALTERNATE RECORD KEY IS S-ALT WITH DUPLICATES
               FILE STATUS IS S-ST.
           SELECT IX-N ASSIGN TO "G2AKC01.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS N-KEY
               FILE STATUS IS N-ST.
           SELECT IX-T ASSIGN TO "G2AKC01.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS T-KEY
               ALTERNATE RECORD KEY IS T-ALT WITH DUPLICATES
               ALTERNATE RECORD KEY IS T-AL2 WITH DUPLICATES
               FILE STATUS IS T-ST.
           SELECT IX-L ASSIGN TO "G2AKC01.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS L-KEY
               ALTERNATE RECORD KEY IS L-ALT WITH DUPLICATES
               FILE STATUS IS L-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IX-M.
       01  M-REC.
           05 M-KEY    PIC X(4).
           05 M-ALT    PIC X(2).
           05 M-DATA   PIC X(4).
       FD  IX-S.
       01  S-REC.
           05 S-KEY    PIC X(4).
           05 S-ALT    PIC X(2).
           05 S-DATA   PIC X(4).
       FD  IX-N.
       01  N-REC.
           05 N-KEY    PIC X(4).
           05 N-DATA   PIC X(6).
       FD  IX-T.
       01  T-REC.
           05 T-KEY    PIC X(4).
           05 T-ALT    PIC X(2).
           05 T-AL2    PIC X(2).
           05 T-DATA   PIC X(2).
       FD  IX-L.
       01  L-REC.
           05 L-KEY    PIC X(4).
           05 L-PAD    PIC X(2).
           05 L-ALT    PIC X(2).
           05 L-DATA   PIC X(2).
       WORKING-STORAGE SECTION.
       01  M-ST        PIC XX.
       01  W1-ST       PIC XX.
       01  W2-ST       PIC XX.
       01  S-ST        PIC XX.
       01  N-ST        PIC XX.
       01  T-ST        PIC XX.
       01  L-ST        PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IX-M.
           MOVE "K001" TO M-KEY. MOVE "BB" TO M-ALT.
           MOVE "ONE " TO M-DATA.
           WRITE M-REC INVALID KEY CONTINUE END-WRITE.
           MOVE M-ST TO W1-ST.
           MOVE "K002" TO M-KEY. MOVE "AA" TO M-ALT.
           MOVE "TWO " TO M-DATA.
           WRITE M-REC INVALID KEY CONTINUE END-WRITE.
           MOVE M-ST TO W2-ST.
           CLOSE IX-M.
           DISPLAY "MAKE " W1-ST " " W2-ST " " M-ST.
           OPEN INPUT IX-S.
           MOVE SPACES TO S-KEY.
           MOVE "AA" TO S-ALT.
           READ IX-S KEY IS S-ALT INVALID KEY CONTINUE END-READ.
           DISPLAY "SAME " S-ST " " S-KEY.
           CLOSE IX-S.
           OPEN INPUT IX-N.
           DISPLAY "NONE " N-ST.
           OPEN INPUT IX-T.
           DISPLAY "TWO " T-ST.
           OPEN INPUT IX-L.
           DISPLAY "MOVED " L-ST.
           STOP RUN.
