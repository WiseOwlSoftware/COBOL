      *>--- conformance-case
      *> id: g2mid07-set-property-single-using
      *> rules: [2002-SR-11.6.2-7]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR7 - "If the SET phrase is specified, then the
      *> method shall have a single USING parameter specified in the
      *> procedure division header and no RETURNING phrase." The SET
      *> PROPERTY BAL method below has exactly that shape (one USING, no
      *> RETURNING) and must be accepted; §2002:11.6.3 GR7 - "If the SET
      *> phrase is specified, this method is a set property method for
      *> property-name-1." so MOVE 42 TO BAL OF OB runs it and the GET
      *> PROPERTY BAL method (SR6 shape: no USING, one RETURNING) reads
      *> W-BAL back. The SET body stores the DOUBLE of its argument, so
      *> "BAL=0084" shows the SET method body ran: 42 in, 0084 out.
      *> Second leg: 2002's SR7 asks the SHAPE only. It has no ban on an
      *> ACTIVE-CLASS USING parameter (2014 added that; 2023 11.7.3 SR7's
      *> second sentence), and §2002:13.16.58.2 SR15 admits ACTIVE-CLASS in
      *> an instance definition and a method's linkage, so SET PROPERTY PEER USING an OBJECT REFERENCE
      *> ACTIVE-CLASS item is legal at 2002 and the compilation group
      *> must be accepted (at 2014 and 2023 the same source is refused,
      *> COBOL2790). Fails if the legal SET shape is refused, if the 2014
      *> ACTIVE-CLASS ban is applied at 2002, or if the SET method body
      *> does not run (a value other than 0084).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2MID07.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2MIDS
           PROPERTY BAL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  OB USAGE OBJECT REFERENCE G2MIDS.
       01  V  PIC 9(4).
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2MIDS "NEW" RETURNING OB.
           MOVE 42 TO BAL OF OB.
           MOVE BAL OF OB TO V.
           DISPLAY "BAL=" V.
           STOP RUN.
       END PROGRAM G2MID07.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2MIDS INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  W-BAL PIC 9(4) VALUE 0.
       01  W-PEER USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION.
       METHOD-ID. SET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-V PIC 9(4).
       PROCEDURE DIVISION USING LK-V.
           COMPUTE W-BAL = LK-V * 2.
       END METHOD.
       METHOD-ID. GET PROPERTY BAL.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-R PIC 9(4).
       PROCEDURE DIVISION RETURNING LK-R.
           MOVE W-BAL TO LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY PEER.
       DATA DIVISION.
       LINKAGE SECTION.
       01  LK-P USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LK-P.
           SET W-PEER TO LK-P.
       END METHOD.
       END OBJECT.
       END CLASS G2MIDS.
