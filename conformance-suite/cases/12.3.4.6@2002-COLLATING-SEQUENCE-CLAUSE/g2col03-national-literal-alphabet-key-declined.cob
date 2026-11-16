      *>--- conformance-case
      *> id: g2col03-national-literal-alphabet-key-declined
      *> rules: [2002-GR-12.3.4.6.3-3]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.6.3 GR3 - "A national collating sequence referenced
      *> by alphabet-name-2 applies to any record keys of class national"
      *>   cite.py --check "§2002:12.3.4.6.3 GR3" (the quote) -> OK
      *> GR3 is observable only through a national alphabet that orders
      *> differently from the native national sequence (GR5), and at
      *> 2002 that is an alphabet defined by literals (§2002:12.2.6.2
      *> SR14c) or by LOCALE. Collating an indexed file's keys by such
      *> an alphabet is processor-dependent: §2002:B.3 item 27 ("The
      *> capability of specifying a collating sequence for primary and
      *> alternate keys of an indexed file", cite.py --check
      *> "§2002:B.3" -> OK), and §2002:3.1.5 lets an implementation
      *> not claim it. WiseOwl does not claim it for a national key, so
      *> this entry is DECLINED BY NAME (COBOL1584, at line 36) and
      *> never accepted inert. Were it provided, READ NEXT would give
      *> Z M A. A NATIVE or UCS-4 alphabet-name-2 is accepted, and it is
      *> the native national sequence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2COL03.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           ALPHABET NREV FOR NATIONAL IS N"Z" N"M" N"A".
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "g2col03.dat"
               ORGANIZATION IS INDEXED
               ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY
               FILE STATUS IS WS-FS
               COLLATING SEQUENCE FOR NATIONAL IS NREV.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC N(1).
       WORKING-STORAGE SECTION.
       01 WS-FS  PIC X(2).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN OUTPUT IXF
           MOVE N"M" TO IX-KEY WRITE IX-REC
           MOVE N"A" TO IX-KEY WRITE IX-REC
           MOVE N"Z" TO IX-KEY WRITE IX-REC
           CLOSE IXF
           OPEN INPUT IXF
           PERFORM 3 TIMES
             READ IXF NEXT AT END DISPLAY "EOF" END-READ
             DISPLAY FUNCTION DISPLAY-OF(IX-KEY)
           END-PERFORM
           CLOSE IXF
           STOP RUN.
