      *>--- conformance-case
      *> id: g2-set-last-exception-off
      *> rules: [2002-GR-14.8.35.3-25]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.35.3 GR25 (Format 12, SET LAST EXCEPTION TO OFF) -
      *> "The predefined object reference EXCEPTION-OBJECT is set to
      *> null, and the last exception status is set to indicate no
      *> exception." Each half is shown going from set to cleared.
      *> Status half: EC-OVERFLOW-STRING is enabled, STRING overflows
      *> (§2002:14.8.39.3 GR8b), non-fatal with no applicable
      *> declarative, so execution continues and the last exception
      *> status names it; FUNCTION EXCEPTION-STATUS returns the 31-char
      *> left-justified name, and spaces when the status indicates no
      *> exception (§2002:15.28.2 RV1).
      *>   S1=[EC-OVERFLOW-STRING + 13 spaces]  then OFF:  S2=[31 sp]
      *> Object half: RAISE O runs the USE AFTER EXCEPTION OBJECT CX
      *> declarative (DECL), sets EXCEPTION-OBJECT to O and the status
      *> to "an exception object has been raised" (§2002:14.5.12.1.4),
      *> and execution continues after the RAISE:
      *>   O3=NONNULL S3=[EXCEPTION-OBJECT + 15 spaces]
      *> then OFF:  O4=NULL S4=[31 spaces]
      *> Fails if OFF leaves either EXCEPTION-OBJECT or the status set.
       >>TURN EC-OVERFLOW-STRING CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SET02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS CX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE CX.
       01 R USAGE OBJECT REFERENCE.
       01 W PIC X(2).
       01 Z PIC X(7).
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION OBJECT CX.
       H-P.
           DISPLAY "DECL".
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           STRING "ABCDEF" DELIMITED BY SIZE INTO W.
           DISPLAY "S1=[" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           DISPLAY "S2=[" FUNCTION EXCEPTION-STATUS "]".
           INVOKE CX "NEW" RETURNING O.
           RAISE O.
           SET R TO EXCEPTION-OBJECT.
           IF R = NULL MOVE "NULL" TO Z ELSE MOVE "NONNULL" TO Z
           END-IF.
           DISPLAY "O3=" Z(1:7) " S3=[" FUNCTION EXCEPTION-STATUS "]".
           SET LAST EXCEPTION TO OFF.
           SET R TO EXCEPTION-OBJECT.
           IF R = NULL MOVE "NULL" TO Z ELSE MOVE "NONNULL" TO Z
           END-IF.
           DISPLAY "O4=" Z " S4=[" FUNCTION EXCEPTION-STATUS "]".
           STOP RUN.
       END PROGRAM G2SET02.
       IDENTIFICATION DIVISION.
       CLASS-ID. CX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS CX.
