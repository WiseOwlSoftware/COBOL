      *>--- conformance-case
      *> id: g2inv08-activating-runtime-element
      *> rules: [2002-GR-14.8.22.3-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.3 GR1 - "The instance of the program, function,
      *> or method that executes the INVOKE statement is the activating
      *> runtime element." With §2002:14.8.22.3 GR7d/GR7e, control
      *> passes to the invoked method and, on return, "control is
      *> transferred to the end of the INVOKE statement" in that
      *> activating element. Three activators are exercised:
      *>   the program G2INV08 (P1/P2/P3 around its INVOKEs),
      *>   the method MA of instance A (MA-IN A / MA-OUT A around an
      *>     INVOKE of instance B and an INVOKE SELF: after each return
      *>     MA continues in instance A, so MA-OUT shows A, not B),
      *>   the user-defined function G2INV08F (F-IN / F-OUT, then R=7).
      *> A wrong activator (control resuming in the invoked instance,
      *> or not returning) changes or drops a line.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2INV08F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV08C.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 FO USAGE OBJECT REFERENCE G2INV08C.
       LINKAGE SECTION.
       01 FR PIC 9.
       PROCEDURE DIVISION RETURNING FR.
       MAIN.
           DISPLAY "F-IN".
           INVOKE G2INV08C "NEW" RETURNING FO.
           INVOKE FO "SETID" USING "F".
           INVOKE FO "MB".
           DISPLAY "F-OUT".
           MOVE 7 TO FR.
       END FUNCTION G2INV08F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV08.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV08C
           FUNCTION G2INV08F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O1 USAGE OBJECT REFERENCE G2INV08C.
       01 O2 USAGE OBJECT REFERENCE G2INV08C.
       01 R  PIC 9.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P1".
           INVOKE G2INV08C "NEW" RETURNING O1.
           INVOKE O1 "SETID" USING "A".
           INVOKE G2INV08C "NEW" RETURNING O2.
           INVOKE O2 "SETID" USING "B".
           INVOKE O1 "MA" USING O2.
           DISPLAY "P2".
           MOVE FUNCTION G2INV08F () TO R.
           DISPLAY "R=" R.
           DISPLAY "P3".
           STOP RUN.
       END PROGRAM G2INV08.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV08C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ID1 PIC X VALUE SPACE.
       PROCEDURE DIVISION.
       METHOD-ID. SETID.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-ID PIC X.
       PROCEDURE DIVISION USING LK-ID.
       MAIN.
           MOVE LK-ID TO ID1.
       END METHOD SETID.
       METHOD-ID. MA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-O USAGE OBJECT REFERENCE G2INV08C.
       PROCEDURE DIVISION USING LK-O.
       MAIN.
           DISPLAY "MA-IN " ID1.
           INVOKE LK-O "MB".
           INVOKE SELF "MB".
           DISPLAY "MA-OUT " ID1.
           EXIT METHOD.
       END METHOD MA.
       METHOD-ID. MB.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "MB " ID1.
           EXIT METHOD.
           DISPLAY "NOT-REACHED".
       END METHOD MB.
       END OBJECT.
       END CLASS G2INV08C.
