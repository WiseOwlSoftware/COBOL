      *>--- conformance-case
      *> id: pb6010-cd-record-global-85
      *> rules: [1985-SR-X.4.6.3-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB6010 - §1985:X.4.6.3 SR1 "The GLOBAL clause may be specified only in data
      *> description entries whose level-number is 01 in the File Section or the Working-Storage
      *> Section, file description entries, or report description entries." A GLOBAL record of
      *> the communication section is refused citing that rule (it cited X.4.3.3 SR4, the level alone).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB6010CD.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDIN FOR INPUT
           SYMBOLIC QUEUE IS IN-QUEUE
           MESSAGE COUNT IS IN-COUNT.
       01  CDIN-REC GLOBAL PIC X(87).
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY "X".
           STOP RUN.
