      *>--- conformance-case
      *> id: g2col01_one_file_level_clause
      *> rules: [2002-SR-12.3.4.6.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.6.2 SR3 - "Only one file-level format COLLATING
      *> SEQUENCE clause may be specified in one file control entry."
      *>   cite.py --check "§2002:12.3.4.6.2 SR3" (the sentence) -> OK
      *> The rule GRANTS one file-level clause per entry (and limits it
      *> to one), so this ACCEPTED case writes exactly one file-level
      *> clause, beside a key-level (Format 2) clause, which SR3 does
      *> not count; g2col02_two_file_level_clauses is the refusal of a
      *> second file-level clause.
      *> §2002:12.3.4.6.3 GR2: "An alphanumeric collating sequence
      *> referenced by alphabet-name-1 applies to any record keys of
      *> class alphanumeric, whether primary or alternate, not specified
      *> in a key-level format of another COLLATING SEQUENCE clause"
      *>   -> cite.py OK §2002:12.3.4.6.3 GR2
      *> §2002:12.3.4.6.3 GR6: "Alphabet-name-3 applies to record keys
      *> identified by data-name-1 or record-key-name-1"
      *>   -> cite.py OK §2002:12.3.4.6.3 GR6
      *> §2002:12.2.6.3 GR7k 3: native characters not in the literal
      *> phrase "shall assume a position in the collating sequence that
      *> is greater than that of the highest character specified"
      *>   -> cite.py OK §2002:12.2.6.3 GR7k
      *> §2002:12.3.4.4.3 GR2c: sequential order "is ascending within a
      *> given key of reference according to the collating sequence for
      *> that key"  -> cite.py OK §2002:12.3.4.4.3 GR2c
      *> DERIVATION OF THE EXPECTED OUTPUT: REV is Z < Y < X < (every
      *> other character, native order), so A follows X. The prime key
      *> IX-KEY takes REV (GR2); the alternate key IX-ALT names its own
      *> key-level clause with NAT (NATIVE), so it takes native (GR6).
      *> Records AA, XX, ZZ (key letter = alternate letter).
      *>   READ NEXT on the prime key          => P:ZZ / P:XX / P:AA
      *>   START on IX-ALT >= LOW-VALUE, READ NEXT on the alternate key
      *>                                       => A:AA / A:XX / A:ZZ
      *> A leg fails if the entry is refused, if the file-level sequence
      *> is not applied to the prime key (P:AA first), or if it is
      *> wrongly applied to the key-level alternate key (A:ZZ first).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COL01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" "Y" "X"
           ALPHABET NAT IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "G2COL01.DAT"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               ALTERNATE RECORD KEY IS IX-ALT
               COLLATING SEQUENCE IS REV
               COLLATING SEQUENCE OF IX-ALT IS NAT
               FILE STATUS IS IX-ST.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IX-REC.
           05  IX-KEY  PIC X.
           05  IX-ALT  PIC X.
       WORKING-STORAGE SECTION.
       01  IX-ST       PIC XX.
       01  WS-EOF      PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IXF.
           MOVE "AA" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W? " IX-ST END-WRITE.
           MOVE "XX" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W? " IX-ST END-WRITE.
           MOVE "ZZ" TO IX-REC.
           WRITE IX-REC INVALID KEY DISPLAY "W? " IX-ST END-WRITE.
           CLOSE IXF.
           OPEN INPUT IXF.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IXF NEXT RECORD
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "P:" IX-REC
               END-READ
           END-PERFORM.
           MOVE LOW-VALUE TO IX-ALT.
           START IXF KEY IS NOT LESS THAN IX-ALT
               INVALID KEY DISPLAY "S? " IX-ST
           END-START.
           MOVE "N" TO WS-EOF.
           PERFORM UNTIL WS-EOF = "Y"
               READ IXF NEXT RECORD
                   AT END MOVE "Y" TO WS-EOF
                   NOT AT END DISPLAY "A:" IX-REC
               END-READ
           END-PERFORM.
           CLOSE IXF.
           STOP RUN.
