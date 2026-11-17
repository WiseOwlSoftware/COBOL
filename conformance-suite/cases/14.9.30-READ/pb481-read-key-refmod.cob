      *>--- conformance-case
      *> id: pb481-read-key-refmod
      *> rules: [SR-14.9.30.3-12, SR-8.4.3.3.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB481 - ISO 14.9.30.3 SR11: "Data-name-1 or record-key-name-1 shall be specified in the RECORD KEY
      *> clause or an ALTERNATE RECORD KEY clause associated with file-name-1." An identity over data items, and
      *> 8.4.3.3.4 GR5 makes IX-KEY(1:3) a different one: "Reference modification creates a unique data item that is
      *> a subset of the data item referenced by identifier-1." START's twin rule was fixed by kb/Work PB602; this
      *> READ arm still asked the base item and compiled clean, reading on the whole key.
      *> kb/Work PB5883: the reference-modifier is now refused first, by the one shape screen every KEY
      *> phrase asks (COBOL2024): "where data-name-n is used in a general format or syntax rule, then
      *> reference-modification is not permitted" (ISO 8.4.3.3.3 NOTE).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB481N4.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "pb481n4.dat"
               ORGANIZATION IS INDEXED ACCESS MODE IS DYNAMIC
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD IXF.
       01 IX-REC.
          05 IX-KEY  PIC X(6).
          05 IX-DATA PIC X(8).
       PROCEDURE DIVISION.
       MAIN-PARA.
           OPEN INPUT IXF.
           READ IXF KEY IS IX-KEY(1:3)
               INVALID KEY DISPLAY "INVALID"
           END-READ.
           CLOSE IXF.
           STOP RUN.
