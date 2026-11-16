      *>--- conformance-case
      *> id: g2-global-cd-record-refused
      *> rules: [2002-SR-13.16.25.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.25.2 SR1 - "The GLOBAL clause may be specified
      *> only in the following entries"; item b) admits a level-1
      *> data description entry only "in the file, working-storage,
      *> local-storage, or linkage section". CDREC is a level-1
      *> record description entry under a CD (§2002:A.1.1.2), in
      *> the communication section, so its GLOBAL clause is in none
      *> of the listed entries and the program shall be refused at
      *> the GLOBAL line. The accepted forms are witnessed by
      *> g2-global-cd-constant.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2GLB02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC IS GLOBAL.
           05 A1 PIC X(4).
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE "ABCD" TO A1
           DISPLAY "A1=[" A1 "]"
           STOP RUN.
