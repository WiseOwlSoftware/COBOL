*>--- conformance-case
*> id: pb2985_active_class_formal_universal_invocation
*> rules: [GR-14.8.2.3.2-4, GR-14.9.23.4-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB2985 (review finding R1-2: the formal half of the returning-item rule) - an ACTIVE-CLASS FORMAL
      *> parameter handed an argument through a UNIVERSAL invocation (a method invoked through an object reference
      *> described with no class).
      *> ISO 14.8.2.3.2 rule 4 (cite.py --check 14.8.2.3.2 -> OK 14.8.2.3.2 4)): "If the formal parameter is described
      *> with the ACTIVE-CLASS phrase, one of the following conditions shall be true: a) The argument shall be an
      *> object reference described with the ACTIVE-CLASS phrase, ... and the method to be activated shall be invoked
      *> with the predefined object references SELF or SUPER, or with an object reference described with the
      *> ACTIVE-CLASS phrase. b) The argument shall be an object reference described with an object-class-name and
      *> the ONLY phrase, ... and the method to be activated shall be invoked with that object-class-name or with an
      *> object reference described with that object-class-name and the ONLY phrase." A universal invoking reference
      *> is none of SELF, SUPER, an ACTIVE-CLASS reference, an object-class-name or an ONLY reference, so no argument
      *> conforms, whatever class the object holds. Every argument of a universal INVOKE is BY REFERENCE (14.9.23.3 SR6),
      *> so 14.8.2.3.3's BY CONTENT paragraph (the same two invocations) is not reached.
      *> 14.9.23.4 GR7 c) (OK 14.9.23.4 7)): through a universal object reference "the rules for conformance specified
      *> in 14.8.2, Parameters ... apply. If a violation of these rules is detected, the EC-OO-UNIVERSAL exception
      *> condition is set to exist if checking for it is enabled in both the activated method and the activating
      *> runtime element, the method invocation is not successful". 9.3.6 binds the method first (7 b): a method whose
      *> match fails ends in EC-OO-METHOD, never in EC-OO-UNIVERSAL.
      *> Classes: P2985F (BASE) declares FA (ACTIVE-CLASS formal), FF (FACTORY OF ACTIVE-CLASS formal), FO (OPTIONAL
      *> ACTIVE-CLASS formal) and FN (a universal formal); Q2985F is written with EC-OO checking OFF and declares FA;
      *> G2985 is an unrelated class. The program's own checking is ON except in t06.
      *> DERIVATION (every method that runs displays its own tag):
      *>  t01 U (a P2985F) FA USING U       match 3 d) 4. holds (an object of the class the method runs on); GR7 c) rule 4
      *>      is violated                                                         EC-OO-UNIVERSAL, FA does not run
      *>  t02 U FF USING FC (P2985F's factory object, from FactoryObject)  match 3 d) 5. holds; rule 4 is violated
      *>                                                                          EC-OO-UNIVERSAL, FF does not run
      *>  t03 U FN USING U                   a universal formal takes a universal argument (14.8.2.3.2 rule 1); no ACTIVE-CLASS
      *>      formal                                                              FN-RAN
      *>  t04 U FO USING OMITTED             an OMITTED argument has no description to conform (9.3.6 3 b))   FO-RAN
      *>  t05 U FO USING U                   an OPTIONAL ACTIVE-CLASS formal handed an argument is rule 4 too  EC-OO-UNIVERSAL
      *>  t06 U FA USING U, EC-OO checking OFF in the activating element: nothing is checked                   FA-RAN
      *>  t07 UQ (a Q2985F) FA USING UQ      a violation, but checking is enabled only in the activating element and not in
      *>      the activated method (GR7 c) "both")                                 FA-RAN (Q's)
      *>  t08 U FA USING UG (a G2985)        9.3.6 3 d) 4.: not an object of the class the method runs on, so the method does
      *>      not match and no method is bound                                    EC-OO-METHOD, not EC-OO-UNIVERSAL
       >>TURN EC-OO CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB2985F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P2985F
           CLASS Q2985F
           CLASS G2985.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TAG PIC X(3).
       01 U   USAGE OBJECT REFERENCE.
       01 UQ  USAGE OBJECT REFERENCE.
       01 UG  USAGE OBJECT REFERENCE.
       01 FC  USAGE OBJECT REFERENCE.
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
           INVOKE P2985F "NEW" RETURNING U
           INVOKE Q2985F "NEW" RETURNING UQ
           INVOKE G2985 "NEW" RETURNING UG
           INVOKE U "FactoryObject" RETURNING FC
           MOVE "t01" TO TAG
           INVOKE U "FA" USING U
           MOVE "t02" TO TAG
           INVOKE U "FF" USING FC
           MOVE "t03" TO TAG
           INVOKE U "FN" USING U
           MOVE "t04" TO TAG
           INVOKE U "FO" USING OMITTED
           MOVE "t05" TO TAG
           INVOKE U "FO" USING U
           MOVE "t06" TO TAG
           >>TURN EC-OO CHECKING OFF
           INVOKE U "FA" USING U
           >>TURN EC-OO CHECKING ON
           MOVE "t07" TO TAG
           INVOKE UQ "FA" USING UQ
           MOVE "t08" TO TAG
           INVOKE U "FA" USING UG
           DISPLAY "END".
           STOP RUN.
       END PROGRAM PB2985F.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2985F INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. FA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LA.
           DISPLAY "FA-RAN".
       END METHOD FA.
       METHOD-ID. FF.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF USAGE OBJECT REFERENCE FACTORY OF ACTIVE-CLASS.
       PROCEDURE DIVISION USING LF.
           DISPLAY "FF-RAN".
       END METHOD FF.
       METHOD-ID. FO.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LO USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING OPTIONAL LO.
           DISPLAY "FO-RAN".
       END METHOD FO.
       METHOD-ID. FN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LN USAGE OBJECT REFERENCE.
       PROCEDURE DIVISION USING LN.
           DISPLAY "FN-RAN".
       END METHOD FN.
       END OBJECT.
       END CLASS P2985F.

       >>TURN EC-OO CHECKING OFF
       IDENTIFICATION DIVISION.
       CLASS-ID. Q2985F INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. FA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LA USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION USING LA.
           DISPLAY "FA-RAN-Q".
       END METHOD FA.
       END OBJECT.
       END CLASS Q2985F.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2985 INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS G2985.
