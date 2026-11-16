      *>--- conformance-case
      *> id: g2inv43-cd-null-receiver
      *> rules: [2002-GR-14.8.22.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.3 GR5 - "If identifier-1 is null, the EC-OO-NULL
      *> exception condition is set to exist and execution of the
      *> INVOKE statement is terminated." Identifier-1 here is OR1, a
      *> level-1 object reference record under a 2002 output CD
      *> (§2002:13.16.58.2 SR13; kb/Work PB5604), and WO, the same
      *> description in working-storage, is the control. The CD
      *> facility is obsolete at 2002 (COBOL0903 warning; the case is
      *> accepted). >>TURN enables the checking, the USE declarative
      *> runs for the raised condition and RESUME AT NEXT STATEMENT
      *> continues after the terminated INVOKE.
      *> EXPECTED:
      *>   HANDLED=EC-OO-NULL  INVOKE OR1 "PING" with OR1 null: PING is
      *>                       never activated (no PINGED line)
      *>   AFTER1 R=KEEP       the INVOKE was terminated, so its
      *>                       RETURNING item R was never stored
      *>   HANDLED=EC-OO-NULL  the same through WO
      *>   AFTER2
      *>   PINGED              OR1 once it references an object: the
      *>   AFTER3 R=PONG       method runs and returns PONG
      *> Why each leg can fail: a CD object reference refused, the null
      *> INVOKE not raising EC-OO-NULL, a method activated through a
      *> null reference, or R changed by a terminated INVOKE.
       >>TURN EC-OO-NULL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV43.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV43C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 R  PIC X(4) VALUE "KEEP".
       01 WO USAGE OBJECT REFERENCE G2INV43C.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT
           TEXT LENGTH IS OTL.
       01 OR1 USAGE OBJECT REFERENCE G2INV43C.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO-NULL.
       H-P.
           DISPLAY "HANDLED=" FUNCTION EXCEPTION-STATUS (1:10).
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           SET OR1 TO NULL.
           INVOKE OR1 "PING" RETURNING R.
           DISPLAY "AFTER1 R=" R.
           SET WO TO NULL.
           INVOKE WO "PING" RETURNING R.
           DISPLAY "AFTER2".
           INVOKE G2INV43C "NEW" RETURNING OR1.
           INVOKE OR1 "PING" RETURNING R.
           DISPLAY "AFTER3 R=" R.
           STOP RUN.
       END PROGRAM G2INV43.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV43C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LR PIC X(4).
       PROCEDURE DIVISION RETURNING LR.
           DISPLAY "PINGED".
           MOVE "PONG" TO LR.
       END METHOD PING.
       END OBJECT.
       END CLASS G2INV43C.
