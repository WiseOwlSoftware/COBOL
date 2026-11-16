      *>--- conformance-case
      *> id: pb5615-receive-into-alphabetic-2002
      *> rules: [2002-SR-A.2.5.2-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2002:8.5.2.1: "An alphabetic data item may be
      *>   specified wherever an elementary alphanumeric data item is
      *>   permitted as an operand." §2002:A.2.5.2 SR2: "Identifier-1
      *>   shall reference a data item of category alphanumeric." RA is
      *>   PIC A, so RECEIVE CIN MESSAGE INTO RA translates at COBOL-2002.
      *>   The program proves translation only: the GO TO skips the
      *>   statement and the output is empty. It fails if RA is refused
      *>   (COBOL6171). The communication facility is removed from
      *>   COBOL-2014 and the sentence with it (§2014:E.2).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615RC.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  RA  PIC A(8).
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
           RECEIVE CIN MESSAGE INTO RA
               NO DATA CONTINUE
           END-RECEIVE.
       DONE-P.
           STOP RUN.
