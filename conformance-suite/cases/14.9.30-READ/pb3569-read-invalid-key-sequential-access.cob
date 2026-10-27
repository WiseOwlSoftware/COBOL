      *>--- conformance-case
      *> id: pb3569-read-invalid-key-sequential-access
      *> rules: [SR-14.9.30.3-8, GR-14.9.30.4-19, FMT-14.9.30.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3569 (the review's sibling arm). ISO 14.9.30.3 SR8
      *> implies the NEXT phrase when neither NEXT nor PREVIOUS is
      *> specified and ACCESS MODE SEQUENTIAL is specified, and ISO
      *> 14.9.30.4 GR19 makes an implicit NEXT a sequential read (each
      *> citation through cite.py --check). ISO 14.9.30.2 prints the
      *> INVALID KEY bracket in Format 2 only, so the READ below, on an
      *> INDEXED file whose access mode is sequential (assumed: the clause
      *> is not written, 12.4.5.5.3 GR1), writes a phrase its Format-1 read
      *> has no place for. It is the same refusal, with the same seam and
      *> wording, that pb334-read-not-invalid-key-sequential-org draws on a
      *> SEQUENTIAL-organization file: COBOL1720, an error under strict.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3569D.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT IXF ASSIGN TO "PB3569D.TMP"
               ORGANIZATION IS INDEXED
               RECORD KEY IS IX-KEY.
       DATA DIVISION.
       FILE SECTION.
       FD  IXF.
       01  IX-REC.
           05 IX-KEY     PIC X(3).
           05 IX-DATA    PIC X(3).
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT IXF
           READ IXF
               INVALID KEY DISPLAY "INVALID"
           END-READ
           CLOSE IXF
           STOP RUN.
