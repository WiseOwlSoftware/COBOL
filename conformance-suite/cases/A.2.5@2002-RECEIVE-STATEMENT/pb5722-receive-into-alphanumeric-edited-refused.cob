      *>--- conformance-case
      *> id: pb5722-receive-into-alphanumeric-edited-refused
      *> rules: [2002-SR-A.2.5.2-2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5722 - §2002:A.2.5.2 SR2: "Identifier-1 shall
      *>   reference a data item of category alphanumeric." AE is
      *>   PIC X(2)/X(2), category alphanumeric-edited (§2002:8.5.2.3),
      *>   a category of its own, so RECEIVE CIN MESSAGE INTO AE is
      *>   refused at COBOL-2002 (COBOL6171). The 85 text has no such
      *>   sentence.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5722RC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  AE  PIC X(2)/X(2).
       COMMUNICATION SECTION.
       CD  CIN FOR INPUT
           SYMBOLIC QUEUE IS IN-Q
           SYMBOLIC SUB-QUEUE-1 IS IN-S1
           SYMBOLIC SUB-QUEUE-2 IS IN-S2
           SYMBOLIC SUB-QUEUE-3 IS IN-S3
           MESSAGE DATE IS IN-DATE
           MESSAGE TIME IS IN-TIME
           SYMBOLIC SOURCE IS IN-SRC
           TEXT LENGTH IS IN-LEN
           END KEY IS IN-END
           STATUS KEY IS IN-STAT
           MESSAGE COUNT IS IN-CNT.
       PROCEDURE DIVISION.
       MAIN-P.
           GO TO DONE-P.
           RECEIVE CIN MESSAGE INTO AE
               NO DATA CONTINUE
           END-RECEIVE.
       DONE-P.
           STOP RUN.
