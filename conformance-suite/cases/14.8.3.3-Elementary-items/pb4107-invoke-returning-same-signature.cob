*>--- conformance-case
*> id: pb4107-invoke-returning-same-signature
*> rules: [GR-14.8.3.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4107 - a typed INVOKE returning a pointer TO P1 into a receiving item TO P2, where P1 and P2 have one
      *> signature. 9.3.6 rule 6 sends the MATCH to SET (14.9.39.3 SR22: the same signature), so RP1 is bound; its
      *> returning item then conforms by 14.8.3.3, "the same ... USAGE clauses", and the TO phrase is compared by the
      *> prototype it names: TO P1 is not TO P2, refused at compile time (14.9.23.3 SR5 c); COBOL0828).
      *> 14.8.3.3: "If the sending operand is not an object reference, the receiving operand shall have the same ALIGN,
      *>   BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses"   cite.py: OK  14.8.3.3 2)
      *> 9.3.6 rule 6: "may be a sending item in a SET statement with the returning item as the receiving item"
      *>   cite.py: OK  9.3.6 6)
      *> 14.9.23.3 SR5 c): "The rules for conformance as specified in 14.8.2, Parameters and 14.8.3, Returning items
      *>   apply."   cite.py: OK  14.9.23.3 5) c)
      *> The universal INVOKE of the same pair sets EC-OO-UNIVERSAL instead (14.9.23.4 GR7 c)): the positive golden
      *> _unassigned/pb2464_prototype_pointer_signature_type.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4107R1 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       PROCEDURE DIVISION USING L-X.
       END PROGRAM P4107R1.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P4107R2 IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       PROCEDURE DIVISION USING L-Y.
       END PROGRAM P4107R2.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T4107R.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107R2
           CLASS C4107R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT2 IS TYPEDEF USAGE PROGRAM-POINTER TO P4107R2.
       01 R2 TYPE PT2.
       01 OC USAGE OBJECT REFERENCE C4107R.
       PROCEDURE DIVISION.
           INVOKE C4107R "NEW" RETURNING OC
           INVOKE OC "RP1" RETURNING R2
           GOBACK.
       END PROGRAM T4107R.

       IDENTIFICATION DIVISION.
       CLASS-ID. C4107R INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P4107R1
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PT1 IS TYPEDEF USAGE PROGRAM-POINTER TO P4107R1.
       PROCEDURE DIVISION.
       METHOD-ID. RP1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LP TYPE PT1.
       PROCEDURE DIVISION RETURNING LP.
           SET LP TO NULL.
       END METHOD RP1.
       END OBJECT.
       END CLASS C4107R.
