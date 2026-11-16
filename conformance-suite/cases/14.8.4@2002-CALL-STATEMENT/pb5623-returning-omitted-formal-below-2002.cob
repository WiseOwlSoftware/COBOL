*>--- conformance-case
*> id: pb5623-returning-omitted-formal-below-2002
*> rules: [2002-GR-14.8.4.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5623 - the omitted argument (CALL ... USING OMITTED) and the OPTIONAL phrase of the procedure
      *> division header are COBOL-2002 introductions, so §2002:14.8.4.3 GR12's reference to an omitted formal,
      *> including the RETURNING identifier-3 of a CALL, cannot be written at COBOL-85: the program draws the
      *> omitted-arguments edition diagnostic (as pb971-arg-omitted-below-2002 does for the argument form).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623NG.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM Q5623NS.
       PROCEDURE DIVISION.
           CALL "Q5623NS" AS Q5623NS USING OMITTED
           STOP RUN.
       END PROGRAM Q5623NG.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. Q5623NS.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RX PIC 9(4).
       PROCEDURE DIVISION USING OPTIONAL RX.
           CALL "Q5623NR" RETURNING RX
           EXIT PROGRAM.
       END PROGRAM Q5623NS.
