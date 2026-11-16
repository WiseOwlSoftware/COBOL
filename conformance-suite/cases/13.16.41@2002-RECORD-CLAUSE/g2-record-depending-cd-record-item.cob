      *>--- conformance-case
      *> id: g2-record-depending-cd-record-item
      *> rules: [2002-SR-13.16.41.2-5]
      *> source-format: fixed
      *>---
      *> §2002:13.16.41.2 SR5 - "Data-name-1 shall describe an elementary
      *> unsigned integer in the working-storage, local-storage, or
      *> linkage section."  CD-RL is an elementary unsigned integer, but
      *> in a record description entry under the CD entry OUT-CD, in the
      *> communication section (§2002:A.1.1.2), which is none of the
      *> three, so DEPENDING ON CD-RL is refused.  The accepted sibling
      *> (data-name-1 in working-storage) is g2-record-contains-odo-maximum.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2REC04.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT F1 ASSIGN TO "g2rec04.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  F1 RECORD IS VARYING IN SIZE FROM 1 TO 20 CHARACTERS
             DEPENDING ON CD-RL.
       01  R1 PIC X(20).
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-ST
           ERROR KEY IS OUT-EK
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  CD-REC.
           05 CD-RL PIC 9(4).
           05 CD-X1 PIC X(6).
       PROCEDURE DIVISION.
       P-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
