      *>--- conformance-case
      *> id: g2-code-set-forms
      *> rules: [2002-FMT-13.16.13.1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.13.1 - every arm of the CODE-SET general format in
      *> a file description entry of a sequential file:
      *>   FA  CODE-SET IS alphabet-name-1
      *>   FB  CODE-SET FOR ALPHANUMERIC IS alphabet-name-1
      *>   FC  CODE-SET FOR NATIONAL IS alphabet-name-2
      *>   FD1 CODE-SET IS alphabet-name-1 alphabet-name-2
      *>   FE  CODE-SET FOR NATIONAL IS alphabet-name-2
      *>                FOR ALPHANUMERIC IS alphabet-name-1
      *>   FF  CODE-SET FOR ALPHANUMERIC IS alphabet-name-1
      *>                FOR NATIONAL IS alphabet-name-2
      *>       (the FOR group carries choice indicators: one or both, in
      *>       any order - the 2002 page 293 diagram)
      *> FD1, FE and FF name both alphabets, so they have no record
      *> description entry: §2002:13.16.13.2 SR3 "either alphabet-name-1
      *> or alphabet-name-2 may be specified, but not both" holds only
      *> "If there are record description entries associated with the
      *> file" (cite.py OK), and §2002:13.16.13.3 GR4 "If there are no
      *> record description entries associated with the file, the
      *> record description used for conversion is the description of
      *> the identifier or literal specified in the FROM phrase"
      *> (cite.py OK) contemplates such a file; it is read INTO.
      *> GR4's record-less sentence speaks only of WRITE/REWRITE FROM;
      *> the READ INTO legs rest on §2002:14.8.29.3 GR5: a READ INTO
      *> with no record description entries "proceeds as though there
      *> were one record description entry describing an alphanumeric
      *> group item of the maximum size established by the RECORD
      *> clause" (cite.py OK), and §2002:13.16.13.3 GR3 "the specified
      *> alphabet is used for code-set conversion of all data items in
      *> each record" (cite.py OK) converts that alphanumeric record
      *> with alphabet-name-1.
      *> A-EBC is EBCDIC, an implementor code-name-1 (§2002:12.2.6.2
      *> SR15, cite.py OK); N-U is UTF-16.
      *>
      *> DERIVATION. §2002:13.16.13.3 GR1 converts "from the native
      *> character set to the coded character set on the storage medium
      *> during output operations" and back on input (cite.py OK), and
      *> GR2 makes the medium's alphanumeric code set "the one
      *> referenced by alphabet-name-1" (cite.py OK). So:
      *>   FA writes ABCD, reads it back           -> FA=ABCD
      *>   FB writes EFGH, reads it back           -> FB=EFGH
      *>   FC writes N"WXYZ", reads it back        -> FC OK
      *>   RA (no CODE-SET) reads FA's medium bytes: EBCDIC A..D are
      *>   X"C1C2C3C4"                             -> RA EBCDIC
      *>   RB reads FB's medium: E..H X"C5C6C7C8"  -> RB EBCDIC
      *>   FD1 reads FA's medium, converting back  -> FD1=ABCD
      *>   FE reads FB's medium, converting back   -> FE=EFGH
      *>   FF reads FA's medium, converting back   -> FF=ABCD
      *> A form the compiler refused fails the compile; an arm that
      *> dropped its alphabet leaves native bytes on the medium (RA/RB
      *> NATIVE) or does not convert back (FD1/FE/FF wrong).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CDS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET A-EBC IS EBCDIC
           ALPHABET N-U FOR NATIONAL IS UTF-16.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA ASSIGN TO "g2cds01a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FB ASSIGN TO "g2cds01b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FC ASSIGN TO "g2cds01c.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FD1 ASSIGN TO "g2cds01a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FE ASSIGN TO "g2cds01b.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT FF ASSIGN TO "g2cds01a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RA ASSIGN TO "g2cds01a.dat"
               ORGANIZATION IS SEQUENTIAL.
           SELECT RB ASSIGN TO "g2cds01b.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  FA CODE-SET IS A-EBC.
       01  FA-REC PIC X(4).
       FD  FB CODE-SET FOR ALPHANUMERIC IS A-EBC.
       01  FB-REC PIC X(4).
       FD  FC CODE-SET FOR NATIONAL IS N-U.
       01  FC-REC PIC N(4).
       FD  FD1 CODE-SET IS A-EBC N-U
               RECORD CONTAINS 4 CHARACTERS.
       FD  FE CODE-SET FOR NATIONAL IS N-U FOR ALPHANUMERIC IS A-EBC
               RECORD CONTAINS 4 CHARACTERS.
       FD  FF CODE-SET FOR ALPHANUMERIC IS A-EBC FOR NATIONAL IS N-U
               RECORD CONTAINS 4 CHARACTERS.
       FD  RA.
       01  RA-REC PIC X(4).
       FD  RB.
       01  RB-REC PIC X(4).
       WORKING-STORAGE SECTION.
       01  WS-X PIC X(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN OUTPUT FA FB FC
           MOVE "ABCD" TO FA-REC
           MOVE "EFGH" TO FB-REC
           MOVE N"WXYZ" TO FC-REC
           WRITE FA-REC
           WRITE FB-REC
           WRITE FC-REC
           CLOSE FA FB FC
           OPEN INPUT FA FB FC
           READ FA
           READ FB
           READ FC
           DISPLAY "FA=" FA-REC
           DISPLAY "FB=" FB-REC
           IF FC-REC = N"WXYZ"
               DISPLAY "FC OK"
           ELSE
               DISPLAY "FC BAD"
           END-IF
           CLOSE FA FB FC
           OPEN INPUT RA RB
           READ RA
           READ RB
           IF RA-REC = X"C1C2C3C4"
               DISPLAY "RA EBCDIC"
           ELSE
               DISPLAY "RA NATIVE"
           END-IF
           IF RB-REC = X"C5C6C7C8"
               DISPLAY "RB EBCDIC"
           ELSE
               DISPLAY "RB NATIVE"
           END-IF
           CLOSE RA RB
           OPEN INPUT FD1
           READ FD1 INTO WS-X
           CLOSE FD1
           DISPLAY "FD1=" WS-X
           MOVE SPACES TO WS-X
           OPEN INPUT FE
           READ FE INTO WS-X
           CLOSE FE
           DISPLAY "FE=" WS-X
           MOVE SPACES TO WS-X
           OPEN INPUT FF
           READ FF INTO WS-X
           CLOSE FF
           DISPLAY "FF=" WS-X
           STOP RUN.
