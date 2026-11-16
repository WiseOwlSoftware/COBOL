      *>--- conformance-case
      *> id: g2-record-depending-cd-text-length
      *> rules: [2002-SR-13.16.41.2-5]
      *> source-format: fixed
      *>---
      *> §2002:13.16.41.2 SR5 - "Data-name-1 shall describe an elementary
      *> unsigned integer in the working-storage, local-storage, or
      *> linkage section."  OUT-LEN is the TEXT LENGTH data item the CD
      *> entry OUT-CD describes implicitly, in the communication section,
      *> which is none of the three, so DEPENDING ON OUT-LEN is refused.
      *> The accepted sibling (data-name-1 in working-storage) is
      *> g2-record-contains-odo-maximum.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REC03.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2rec03.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
             DEPENDING ON OUT-LEN.
       01  R1 PIC X(20).
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-ST
           ERROR KEY IS OUT-EK
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  CD-REC.
           05 CD-X1 PIC X(10).
       PROCEDURE DIVISION.
       P-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
