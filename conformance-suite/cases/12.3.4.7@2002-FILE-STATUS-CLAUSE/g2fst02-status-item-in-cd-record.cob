      *>--- conformance-case
      *> id: g2fst02-status-item-in-cd-record
      *> rules: [2002-SR-12.3.4.7.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.4.7.2 SR2 - "Data-name-1 shall be a two-character
      *> data item of the category alphanumeric, defined in the
      *> working-storage, local-storage, or linkage section."
      *>   cite.py --check "§2002:12.3.4.7.2 SR2" -> OK
      *> CFS is a two-character alphanumeric item, but it is in the
      *> record under a CD of the communication section, which the list
      *> does not admit: the FILE STATUS clause shall be refused at its
      *> data-name-1. (The communication section is obsolete at 2002,
      *> COBOL0903, which does not by itself refuse.)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FST02.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "G2FST02-ABSENT.DAT"
               FILE STATUS IS CFS.
       DATA DIVISION.
       FILE SECTION.
       FD  F1.
       01  R1          PIC X(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05 CFS      PIC XX.
       PROCEDURE DIVISION.
       MAIN-P.
           OPEN INPUT F1.
           DISPLAY "CFS=" CFS.
           STOP RUN.
