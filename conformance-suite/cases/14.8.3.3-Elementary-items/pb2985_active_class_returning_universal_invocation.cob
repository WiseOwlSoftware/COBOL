*>--- conformance-case
*> id: pb2985_active_class_returning_universal_invocation
*> rules: [GR-14.8.3.3-2, GR-14.9.23.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2985 - an ACTIVE-CLASS returning item delivered through a UNIVERSAL invocation (a method
      *> invoked through an object reference described with no class).
      *> ISO 14.8.3.3 rule 2 (cite.py --check 14.8.3.3 -> OK 14.8.3.3 2)): "If the returning item in the activated
      *> element is described with an ACTIVE-CLASS phrase, the conformance rules are the same as the conformance
      *> rules for a SET statement specified in the activating element with the following operands: a) A receiving
      *> operand that is the returning item in the activating element. b) A sending operand that is an object
      *> reference described as follows: ... 4. If the activated method is invoked with any other object reference,
      *> the sending operand has the same description as that object reference." (OK 14.8.3.3 2) b) 4.)
      *> A universal invoking reference is "any other object reference", so the sending operand is a UNIVERSAL object
      *> reference whatever the class of the object it holds. 14.9.39.3 SR12 (OK 14.9.39.3 12)) lists the senders a
      *> receiving item described with an object-class-name takes - an object-class-name reference, an ACTIVE-CLASS
      *> reference, SELF, NULL - and SR10 and SR14 close their lists the same way, so a universal sender is valid only
      *> into a UNIVERSAL receiving item (SR8: OK 14.9.39.3 8)): a typed receiving item does not conform.
      *> 14.9.23.4 GR7 c) (OK 14.9.23.4 7)): through a universal object reference "the rules for conformance
      *> specified in 14.8.2, Parameters and 14.8.3, Returning items apply. If a violation of these rules is
      *> detected, the EC-OO-UNIVERSAL exception condition is set to exist if checking for it is enabled in both the
      *> activated method and the activating runtime element, the method invocation is not successful". The
      *> conformance is a property of the DESCRIPTIONS, not of the object returned, so it is due when the object is
      *> in fact of the receiving item's class; an invocation that is not successful leaves the receiving item
      *> unchanged.
      *> Classes: P2985P (instance method RA returns SELF, factory method FA returns a new object through SELF "NEW",
      *> both with an ACTIVE-CLASS returning item); P2985S INHERITS P2985P; Q2985 declares RA the same way in a class
      *> written with EC-OO checking OFF. The program's own checking is ON except in t10.
      *> DERIVATION (every case's receiving item starts NULL; "delivered" = not NULL afterwards):
      *>  t01 U  (a P2985S object) RA -> universal item           SR8: conforms                      delivered
      *>  t02 U  RA -> OP (P2985P)          universal sender, SR12 admits none      EC-OO-UNIVERSAL, NULL
      *>  t03 OS (typed P2985S) RA -> OP    COMPILE time, rule 2 b) 4. gives the sender OS's own description
      *>      (P2985S): SR12 a)2. a subclass of P2985P - conforms; the typed lane is unchanged   delivered
      *>  t04 UF (P2985S's factory) FA -> universal item          SR8: conforms                      delivered
      *>  t05 UF FA -> OP                   universal sender, SR12 admits none      EC-OO-UNIVERSAL, NULL
      *>  t06 UF NEW (BASE's New, 16.2: "usage object reference active-class") -> OB (BASE): the 9.3.6 rule 6 match
      *>      holds (the class containing the item is BASE, SR12 b)2.), the conformance does not   EC-OO-UNIVERSAL
      *>  t07 UF NEW -> universal item      SR8: conforms                                              delivered
      *>  t08 U  FactoryObject (16.2: "factory of active-class") -> FB (FACTORY OF BASE)  EC-OO-UNIVERSAL, NULL
      *>  t09 UQ (a Q2985 object) RA -> OQ (Q2985): a violation, but checking is enabled only in the activating
      *>      element and not in the activated method (GR7 c) "both") - no exception condition; the object is of
      *>      the receiving item's class, so the delivery narrows it                                    delivered
      *>  t10 U  RA -> OP with checking OFF in the activating element: nothing is checked, the object is a P2985S
      *>      and OP takes it                                                                        delivered
      *> A conforming case prints its tag and "delivered"; a violation prints its HANDLED= line first.
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2985M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS P2985P
           CLASS P2985S
           CLASS Q2985.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TAG PIC X(3).
       01 OS  USAGE OBJECT REFERENCE P2985S.
       01 OQ  USAGE OBJECT REFERENCE Q2985.
       01 OP  USAGE OBJECT REFERENCE P2985P.
       01 OB  USAGE OBJECT REFERENCE BASE.
       01 FB  USAGE OBJECT REFERENCE FACTORY OF BASE.
       01 U   USAGE OBJECT REFERENCE.
       01 UQ  USAGE OBJECT REFERENCE.
       01 UF  USAGE OBJECT REFERENCE.
       01 U2  USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION.
       DECLARATIVES.
       H SECTION.
           USE AFTER EXCEPTION CONDITION EC-OO.
       H-P.
           DISPLAY TAG " HANDLED=" FUNCTION EXCEPTION-STATUS.
           RESUME AT NEXT STATEMENT.
       END DECLARATIVES.
       MAIN SECTION.
       MAIN-P.
           INVOKE P2985S "NEW" RETURNING OS
           SET U TO OS
           INVOKE Q2985 "NEW" RETURNING OQ
           SET UQ TO OQ
           SET UF TO P2985S
           MOVE "t01" TO TAG
           SET U2 TO NULL
           INVOKE U "RA" RETURNING U2
           IF U2 = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t02" TO TAG
           SET OP TO NULL
           INVOKE U "RA" RETURNING OP
           IF OP = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t03" TO TAG
           SET OP TO NULL
           INVOKE OS "RA" RETURNING OP
           IF OP = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t04" TO TAG
           SET U2 TO NULL
           INVOKE UF "FA" RETURNING U2
           IF U2 = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t05" TO TAG
           SET OP TO NULL
           INVOKE UF "FA" RETURNING OP
           IF OP = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t06" TO TAG
           SET OB TO NULL
           INVOKE UF "NEW" RETURNING OB
           IF OB = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t07" TO TAG
           SET U2 TO NULL
           INVOKE UF "NEW" RETURNING U2
           IF U2 = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t08" TO TAG
           SET FB TO NULL
           INVOKE U "FactoryObject" RETURNING FB
           IF FB = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t09" TO TAG
           SET OQ TO NULL
           INVOKE UQ "RA" RETURNING OQ
           IF OQ = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           MOVE "t10" TO TAG
           SET OP TO NULL
           >>TURN EC-OO CHECKING OFF
           INVOKE U "RA" RETURNING OP
           >>TURN EC-OO CHECKING ON
           IF OP = NULL DISPLAY TAG " NULL" ELSE DISPLAY TAG " delivered" END-IF
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB2985M.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2985P INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       PROCEDURE DIVISION.
       METHOD-ID. FA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           INVOKE SELF "NEW" RETURNING R
           GOBACK.
       END METHOD FA.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF
           GOBACK.
       END METHOD RA.
       END OBJECT.
       END CLASS P2985P.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2985S INHERITS FROM P2985P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2985P.
       END CLASS P2985S.

       >>TURN EC-OO CHECKING OFF
       IDENTIFICATION DIVISION.
       CLASS-ID. Q2985 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING R.
           SET R TO SELF
           GOBACK.
       END METHOD RA.
       END OBJECT.
       END CLASS Q2985.
