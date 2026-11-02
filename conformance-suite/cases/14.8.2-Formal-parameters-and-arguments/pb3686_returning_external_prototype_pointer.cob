*>--- conformance-case
*> id: pb3686_returning_external_prototype_pointer
*> rules: [SR-14.9.39.3-22, GR-14.8.2.3.2-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3686, re-verdicted by kb/Work PB4107 - a CALL's RETURNING item compares two restricted
      *> program-pointers by their USAGE clauses, never by the SET relation's signature: the TO phrase is compared by the
      *> prototype it names, so two prototypes of ONE signature are still two names.
      *> 14.8.3.3: "If the sending operand is not an object reference, the receiving operand shall have the same ALIGN,
      *>   BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses"   cite.py: OK  14.8.3.3 2)
      *> 13.18.60.4 GR25: "A restricted program-pointer shall contain only the predefined address NULL or the address of
      *>   a program with the same signature" - what the pointer may CONTAIN, which a SET asks (14.9.39.3 SR22), not its
      *>   USAGE clause.   cite.py: OK  13.18.60.4 25)
      *> X3686A and X3686B are program prototype definitions of ONE signature (12.3.8.4 GR10 b); kb/Work PB2103 made every
      *> prototype carry its signature, so the "signature only the run unit knows" this case first named is gone). U3686
      *> is called by literal name, so the check is 14.9.4.4 GR3 d)'s at the activation. U3686's returning item is USAGE
      *> PROGRAM-POINTER TO X3686B and the activating element's receiving item USAGE PROGRAM-POINTER TO X3686A: two USAGE
      *> clauses, whatever the two signatures, so the CALL sets EC-PROGRAM-ARG-MISMATCH (kb/Work PB3686 had admitted it).
      *> The control: an UNRESTRICTED program-pointer receives the same returning item opposite a different USAGE clause,
      *> so 14.9.4.4 GR3 d) sets EC-PROGRAM-ARG-MISMATCH.   cite.py: OK  14.9.4.4 3) d)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X3686A IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
       END PROGRAM X3686A.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. X3686B IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
       END PROGRAM X3686B.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T3686.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM X3686A
           PROGRAM X3686B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA TYPEDEF USAGE PROGRAM-POINTER TO X3686A.
       01 PA TYPE TA.
       01 PU USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
           CALL "U3686" RETURNING PA
               ON EXCEPTION DISPLAY "dyn exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "dyn ok"
           END-CALL
           CALL "U3686" RETURNING PU
               ON EXCEPTION DISPLAY "unrestricted exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "unrestricted ok"
           END-CALL
           GOBACK.
       END PROGRAM T3686.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. U3686.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM X3686B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO X3686B.
       LINKAGE SECTION.
       01 R TYPE TB.
       PROCEDURE DIVISION RETURNING R.
           DISPLAY "in sub"
           GOBACK.
       END PROGRAM U3686.
