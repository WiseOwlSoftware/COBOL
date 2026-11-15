      *>--- conformance-case
      *> id: g2col02_two_file_level_clauses
      *> rules: [2002-SR-12.3.4.6.2-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.6.2 SR3 - "Only one file-level format COLLATING
      *> SEQUENCE clause may be specified in one file control entry."
      *>   cite.py --check "§2002:12.3.4.6.2 SR3" (the sentence) -> OK
      *> The limiting half of SR3, beside the accepted
      *> g2col01_one_file_level_clause: this entry writes TWO file-level
      *> (Format 1) clauses, one in the IS alphabet-name-1 form and one
      *> in the FOR ALPHANUMERIC IS form, so it is refused (COBOL1582,
      *> reported at line 28). With either clause removed it is the
      *> legal shape g2col01 shows accepted at 2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COL02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET REV IS "Z" "Y" "X"
           ALPHABET NAT IS NATIVE.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "G2COL02.DAT"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IX-KEY
               COLLATING SEQUENCE IS REV
               COLLATING SEQUENCE FOR ALPHANUMERIC IS NAT.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IX-REC.
           05  IX-KEY  PIC X.
           05  IX-VAL  PIC X.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IXF.
           CLOSE IXF.
           STOP RUN.
