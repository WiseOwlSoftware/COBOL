      *>--- conformance-case
      *> id: pb4759_debug_cd_name_triggers
      *> rules: []
      *> accept: [85]
      *> files: [mcs.json]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB4759. X3.23-1985 USE FOR DEBUGGING general rule 8, the
      *> cd-name trigger (docs/CONTROL_FLOW_DESIGN.md D20): "When cd-name-1 is
      *> specified in a USE FOR DEBUGGING statement, that debugging section is
      *> executed: a. After the execution of any ENABLE, DISABLE, and SEND
      *> statement that references cd-name-1, b. After the execution of a
      *> RECEIVE statement referencing cd-name-1 that does not result in the
      *> execution of the NO DATA imperative-statement, and c. After the
      *> execution of an ACCEPT MESSAGE COUNT statement that references
      *> cd-name-1." PURGE is not listed and runs no section. General rule
      *> 23: DEBUG-NAME is the cd-name, and for any reference to a cd-name
      *> DEBUG-CONTENTS holds "the content of the area associated with the
      *> cd-name"; the first twelve characters of an input CD's area are its
      *> SYMBOLIC QUEUE (the queue the program named, mcs.json beside this
      *> program seeds it), which W-CONT receives.
      *> The WITH DATA statement follows the section: the RECEIVE itself is
      *> the statement the rule is about.
      *> Why each leg can fail: PURGE runs nothing; a RECEIVE that took the
      *> NO DATA path runs nothing and NODATA is its only line; the section
      *> of the OUTPUT CD runs for SEND and the input CD's does not.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4759CD.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. IBM-PC WITH DEBUGGING MODE.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  T-B   PIC X(5) VALUE "ABCDE".
       01  W12   PIC X(12).
       01  W-NAME PIC X(6).
       01  W-CONT PIC X(12).
       COMMUNICATION SECTION.
       CD COUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS OL
           STATUS KEY IS OK
           DESTINATION TABLE OCCURS 2 TIMES
           ERROR KEY IS OE
           SYMBOLIC DESTINATION IS OD.
       CD CIN FOR INPUT
           SYMBOLIC QUEUE IS Q
           SYMBOLIC SUB-QUEUE-1 IS S1
           SYMBOLIC SUB-QUEUE-2 IS S2
           SYMBOLIC SUB-QUEUE-3 IS S3
           MESSAGE DATE IS MD
           MESSAGE TIME IS MT
           SYMBOLIC SOURCE IS SRC
           TEXT LENGTH IS TL
           END KEY IS EK
           STATUS KEY IS SK
           MESSAGE COUNT IS MC.
       PROCEDURE DIVISION.
       DECLARATIVES.
       DBG-CD SECTION.
           USE FOR DEBUGGING ON CIN COUT.
       DBG-CD-BODY.
           MOVE DEBUG-NAME TO W-NAME.
           MOVE DEBUG-CONTENTS TO W-CONT.
           DISPLAY "CD " W-NAME "[" W-CONT "]".
       END DECLARATIVES.
       MAIN SECTION.
       M1.
           MOVE "SEGQ" TO Q.
           MOVE SPACES TO S1 S2 S3.
           ACCEPT CIN MESSAGE COUNT.
           DISPLAY "COUNT " MC.
           RECEIVE CIN MESSAGE INTO W12
               NO DATA DISPLAY "NODATA"
               WITH DATA DISPLAY "WITHDATA"
           END-RECEIVE.
           RECEIVE CIN MESSAGE INTO W12
               NO DATA DISPLAY "NODATA"
               WITH DATA DISPLAY "WITHDATA"
           END-RECEIVE.
           MOVE 1 TO DC OL.
           MOVE "LOOP" TO OD (1).
           SEND COUT FROM T-B WITH EMI.
           PURGE COUT.
           STOP RUN.
