*>--- conformance-case
*> id: pb929_set_sr10_interface_receiver_senders
*> rules: [SR-14.9.39.3-10]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 10 — the closed list of senders (identifier-4) a SET format 5 admits into a
      *> receiver (identifier-3) described with an interface-name that identifies the interface int-1.  kb/Work PB929.
      *> This program is the positive witness of the two alternatives no other golden writes into an
      *> interface-named receiver (the others are witnessed by pb389_object_reference_descriptor, a b) 1./2. and
      *> c) 1./2. witness, and by oo_set_self_interface and pb451_set_format5_self_sender, a d) 1./2. witness).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES — NOT FROM A RUN:
      *>
      *> 1. SET RJ TO OC: RJ is described with the interface PB929J, OC with the class PB929C and no FACTORY
      *>    phrase, so SR10 b) 2. ("the objects of the specified class shall implement int-1", here int-1 is
      *>    PB929J) is satisfied by PB929C's OBJECT IMPLEMENTS PB929J.
      *>
      *> 2. SET RI TO RJ is SR10 a): RI is described with the interface PB929I, and the sender RJ "is an object
      *>    reference described with an interface-name that identifies int-1 or an interface inheriting from
      *>    int-1" — PB929J INHERITS FROM PB929I, so the second alternative holds.  INVOKE RI "PING" resolves
      *>    on the object RJ holds, a PB929C instance, so PB929C's PING runs:                            PING-C
      *>    DISCRIMINATOR: a check that tested only for the identical interface (a direct match) refuses line 2.
      *>
      *> 3. SET RI TO NULL is SR10 e) ("the predefined object reference NULL").  §13.18.60.4 GR22 makes null a
      *>    legal content of every object reference, so the assignment is unconditional.  The IF then observes
      *>    it, true when RI references no object:                                                     NULLOK
      *>    DISCRIMINATOR: the line runs after RI has held a real PB929C object, so it proves the SET cleared it.
      *>
      *> The refusing arms are fixtures, one rule alternative each, each differing from a conforming program by
      *> the one clause SR10 names:
      *> conformance:cases/14.9.39-SET/pb929-set-sr10-interface-sender-unrelated (a),
      *> …-class-sender-not-implementing (b) 2.), …-factory-sender-not-implementing (b) 1.),
      *> …-active-class-sender-not-implementing (c) 2.).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB929A.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB929C
           INTERFACE PB929I
           INTERFACE PB929J.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 OC USAGE OBJECT REFERENCE PB929C.
       01 RJ USAGE OBJECT REFERENCE PB929J.
       01 RI USAGE OBJECT REFERENCE PB929I.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB929C "NEW" RETURNING OC.
           SET RJ TO OC.
           SET RI TO RJ.
           INVOKE RI "PING".
           SET RI TO NULL.
           IF RI = NULL
               DISPLAY "NULLOK"
           END-IF.
           STOP RUN.
       END PROGRAM PB929A.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB929I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE PB929I.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. PB929J INHERITS FROM PB929I.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE PB929I.
       PROCEDURE DIVISION.
       END INTERFACE PB929J.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB929C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           INTERFACE PB929J.
       IDENTIFICATION DIVISION.
       OBJECT. IMPLEMENTS PB929J.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "PING-C".
       END METHOD PING.
       END OBJECT.
       END CLASS PB929C.
