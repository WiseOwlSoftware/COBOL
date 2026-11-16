      *>--- conformance-case
      *> id: g2imi02_inline_invocation_valid_invoke_forms
      *> rules: [2002-SR-8.4.2.4.2-3]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:8.4.2.4.2 SR3 - "One of the INVOKE statements specified in
      *> general rule 1 shall be valid according to 14.8.22, INVOKE
      *> statement, syntax rules." Each inline invocation below has a
      *> valid equivalent INVOKE under the 2002 INVOKE syntax rules, so
      *> each must be accepted and return what that INVOKE returns
      *> (§2002:8.4.2.4.3 GR1):
      *>  - O :: "ECHO" (CA): CA is in the COMMUNICATION SECTION, which
      *>    §2002:14.8.22.2 SR9 admits for identifier-3 ("defined in the
      *>    file, working-storage, local-storage, linkage, or communication
      *>    section"); no BY phrase, so BY REFERENCE is implied (SR14).
      *>    ECHO returns its argument: "ECHO=QRST"
      *>  - O :: "DBL" (CN): the formal is BY VALUE and CN, a numeric
      *>    communication-section item, is of class numeric (SR15).
      *>    DBL returns 2 * 21: "DBL=0042"
      *>  - G2IMI02C :: "TAG" (CA): class-name-1 with a method of the
      *>    factory interface (SR3). TAG returns "F-" followed by its
      *>    argument: "TAG=F-QRST"
      *> A leg fails if the inline form is refused where its INVOKE is
      *> valid (the communication-section reach is 2002's alone) or if
      *> the value differs from the INVOKE's returned item.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IMI02.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2IMI02C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  O  USAGE OBJECT REFERENCE G2IMI02C.
       01  W  PIC X(4).
       01  W6 PIC X(6).
       01  N  PIC 9(4).
       COMMUNICATION SECTION.
       CD  CDOUT FOR OUTPUT
           DESTINATION COUNT IS DC
           TEXT LENGTH IS TL
           STATUS KEY IS SK
           ERROR KEY IS EK
           SYMBOLIC DESTINATION IS SD1.
       01  CDREC.
           05  CA PIC X(4).
           05  CN PIC 9(4).
       PROCEDURE DIVISION.
       MAIN-PARA.
           INVOKE G2IMI02C "NEW" RETURNING O.
           MOVE "QRST" TO CA.
           MOVE 21 TO CN.
           MOVE O :: "ECHO" (CA) TO W.
           DISPLAY "ECHO=" W.
           COMPUTE N = O :: "DBL" (CN).
           DISPLAY "DBL=" N.
           MOVE G2IMI02C :: "TAG" (CA) TO W6.
           DISPLAY "TAG=" W6.
           STOP RUN.
       END PROGRAM G2IMI02.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2IMI02C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. TAG.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LX PIC X(4).
       01  LR PIC X(6).
       PROCEDURE DIVISION USING LX RETURNING LR.
           STRING "F-" LX DELIMITED BY SIZE INTO LR.
       END METHOD TAG.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. ECHO.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LX PIC X(4).
       01  LR PIC X(4).
       PROCEDURE DIVISION USING LX RETURNING LR.
           MOVE LX TO LR.
       END METHOD ECHO.
       METHOD-ID. DBL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LN PIC 9(4).
       01  LR PIC 9(4).
       PROCEDURE DIVISION USING BY VALUE LN RETURNING LR.
           COMPUTE LR = LN * 2.
       END METHOD DBL.
       END OBJECT.
       END CLASS G2IMI02C.
