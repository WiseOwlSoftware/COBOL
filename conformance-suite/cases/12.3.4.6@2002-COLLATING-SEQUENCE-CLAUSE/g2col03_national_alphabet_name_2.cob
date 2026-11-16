      *>--- conformance-case
      *> id: g2col03_national_alphabet_name_2
      *> rules: [2002-GR-12.3.4.6.3-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.6.3 GR3 - "A national collating sequence
      *> referenced by alphabet-name-2 applies to any record keys of
      *> class national, whether primary or alternate, not specified in
      *> a key-level format of another COLLATING SEQUENCE clause in the
      *> file control entry."  -> cite.py OK §2002:12.3.4.6.3 GR3
      *> The file-level clause is the Format 1 form IS alphabet-name-1
      *> alphabet-name-2: COLLATING SEQUENCE IS REV NU, where REV is an
      *> alphanumeric alphabet (Z first) and NU is ALPHABET NU FOR
      *> NATIONAL IS UCS-4. §2002:12.2.6.3 GR7f: "The collating sequence
      *> referenced by the UCS-4 phrase is defined by the order in which
      *> characters appear in ISO/IEC 10646-1 followed by the order in
      *> which characters appear in ISO/IEC 10646-2"
      *>   -> cite.py OK §2002:12.2.6.3 GR7f
      *> so under NU the national characters A, M, Z order A < M < Z.
      *> §2002:12.3.4.6.3 GR2 gives REV to the alphanumeric key GA only
      *>   -> cite.py OK §2002:12.3.4.6.3 GR2
      *> Keys: GK national prime, GN national alternate, GA
      *> alphanumeric alternate. Records, in the order written:
      *>   tag 1: GK "M", GN "Z", GA "A"
      *>   tag 2: GK "Z", GN "A", GA "M"
      *>   tag 3: GK "A", GN "M", GA "Z"
      *> Sequential order is ascending in the key of reference by the
      *> file's collating sequence (§2002:14.8.29.3 GR18). Derived:
      *>   W1..W3 00 (all key values distinct);
      *>   SN, SX 00: START KEY IS NOT LESS THAN the lowest value of the
      *>   key's own sequence (N"A" under NU, "Z" under REV) finds a
      *>   record, the comparison made "according to the collating
      *>   sequence of the file" (§2002:14.8.37.3 GR17e), and the key
      *>   becomes the key of reference (GR16);
      *>   P: prime GK by NU (A M Z)        -> tags 3 1 2
      *>   N: alternate GN by NU (A M Z)    -> tags 2 3 1
      *>   X: alternate GA by REV (Z M A)   -> tags 3 2 1
      *> Each walk can fail: had REV reached a national key, P would be
      *> 2 1 3 and N 1 3 2; had NU reached GA, X would be 1 2 3.
      *> Not observable here: the key-level exclusion in GR3 (every
      *> national alphabet this processor accepts orders the 65,536
      *> single-position characters as UCS-4 does, docs/conformance/7/
      *> DOC-A.1-188.md).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COL04.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "ZYXWVUTSRQPONMLKJIHGFEDCBA"
           ALPHABET NU FOR NATIONAL IS UCS-4.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G ASSIGN TO "G2COL04.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS GK
               ALTERNATE RECORD KEY IS GN
               ALTERNATE RECORD KEY IS GA
               COLLATING SEQUENCE IS REV NU
               FILE STATUS IS FS.
       DATA DIVISION.
       FILE SECTION.
       FD G.
       01 GR.
          05 GK PIC N(1).
          05 GN PIC N(1).
          05 GA PIC X(1).
          05 GT PIC X(1).
       WORKING-STORAGE SECTION.
       01 FS PIC XX.
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT G
           MOVE N"M" TO GK MOVE N"Z" TO GN MOVE "A" TO GA
           MOVE "1" TO GT WRITE GR
           DISPLAY "W1 " FS
           MOVE N"Z" TO GK MOVE N"A" TO GN MOVE "M" TO GA
           MOVE "2" TO GT WRITE GR
           DISPLAY "W2 " FS
           MOVE N"A" TO GK MOVE N"M" TO GN MOVE "Z" TO GA
           MOVE "3" TO GT WRITE GR
           DISPLAY "W3 " FS
           CLOSE G
           OPEN INPUT G
           READ G NEXT DISPLAY "P " GT " " FS
           READ G NEXT DISPLAY "P " GT " " FS
           READ G NEXT DISPLAY "P " GT " " FS
           MOVE N"A" TO GN
           START G KEY IS NOT LESS THAN GN
           DISPLAY "SN " FS
           READ G NEXT DISPLAY "N " GT " " FS
           READ G NEXT DISPLAY "N " GT " " FS
           READ G NEXT DISPLAY "N " GT " " FS
           MOVE "Z" TO GA
           START G KEY IS NOT LESS THAN GA
           DISPLAY "SX " FS
           READ G NEXT DISPLAY "X " GT " " FS
           READ G NEXT DISPLAY "X " GT " " FS
           READ G NEXT DISPLAY "X " GT " " FS
           CLOSE G
           STOP RUN.
