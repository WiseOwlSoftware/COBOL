      *> reject-at: 2002 2014 2023
      *> ISO §8.4.3.4.3 SR4: "The data item referenced in the RETURNING phrase
      *> of the invoked method's procedure division header shall not be
      *> described with the ANY LENGTH clause or with the ACTIVE-CLASS
      *> phrase."  Both arms would leave §8.4.3.4.4 GR1 b)'s "same
      *> description, class, and category" temporary undescribable at the
      *> point of reference — an ANY LENGTH item has no length until the
      *> activation and an ACTIVE-CLASS reference has no class until the
      *> runtime class of the receiver is known — which is why the same
      *> method is legal through an INVOKE statement with a written RETURNING
      *> identifier.  COBOLNET2140.
      *> ⚠ THIS FIXTURE TAKES THE ACTIVE-CLASS ARM DELIBERATELY. The ANY
      *> LENGTH arm is currently SHADOWED: an ANY LENGTH RETURNING item is
      *> staged loud at COBOLNET0899 in the data binder (the
      *> ANY-LENGTH-RETURNING wave) before any invocation binds, so a fixture
      *> written that way would pin the STAGE and never exercise SR4 — a
      *> green test holding the rule's own arm unverified.  When that wave
      *> lands, add the ANY LENGTH twin here.  kb/Work PB428.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB428N4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB428N4C.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OBJ USAGE OBJECT REFERENCE PB428N4C.
       01 W   USAGE OBJECT REFERENCE PB428N4C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB428N4C "NEW" RETURNING OBJ.
           MOVE OBJ :: "CLONEME" TO W.
           STOP RUN.
       END PROGRAM PB428N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB428N4C.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. CLONEME.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-OBJ USAGE OBJECT REFERENCE ACTIVE-CLASS.
       PROCEDURE DIVISION RETURNING LK-OBJ.
       MAIN.
           SET LK-OBJ TO SELF.
       END METHOD CLONEME.
       END OBJECT.
       END CLASS PB428N4C.
