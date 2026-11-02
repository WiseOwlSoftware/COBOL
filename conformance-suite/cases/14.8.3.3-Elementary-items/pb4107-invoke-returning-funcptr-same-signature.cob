*>--- conformance-case
*> id: pb4107-invoke-returning-funcptr-same-signature
*> rules: [GR-14.8.3.3-2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4107 - the function-pointer twin of pb4107-invoke-returning-same-signature: a typed INVOKE returning a
      *> FUNCTION-POINTER TO F4107A into a receiving item TO F4107B, the two of one signature. 9.3.6 rule 6 matches
      *> (14.9.39.3 SR20: the same signature) and 14.8.3.3's "the same ... USAGE clauses" refuses the pair at compile
      *> time (14.9.23.3 SR5 c); COBOL0828).
      *> 14.8.3.3: "If the sending operand is not an object reference, the receiving operand shall have the same ALIGN,
      *>   BLANK WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE clauses"   cite.py: OK  14.8.3.3 2)
      *> 14.9.39.3 SR20: "The function-prototypes associated with identifier-12 and identifier-13 shall have the same
      *>   signature."   cite.py: OK  14.9.39.3 20)
      *> 14.9.23.3 SR5 c): "The rules for conformance as specified in 14.8.2, Parameters and 14.8.3, Returning items
      *>   apply."   cite.py: OK  14.9.23.3 5) c)
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F4107A IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-X PIC 9(4).
       01 L-R PIC 9(6).
       PROCEDURE DIVISION USING L-X RETURNING L-R.
       END FUNCTION F4107A.
       IDENTIFICATION DIVISION.
       FUNCTION-ID. F4107B IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L-Y PIC 9(4).
       01 L-S PIC 9(6).
       PROCEDURE DIVISION USING L-Y RETURNING L-S.
       END FUNCTION F4107B.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T4107F.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F4107B
           CLASS C4107F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP2 USAGE FUNCTION-POINTER TO F4107B.
       01 OC USAGE OBJECT REFERENCE C4107F.
       PROCEDURE DIVISION.
           INVOKE C4107F "NEW" RETURNING OC
           INVOKE OC "RF1" RETURNING FP2
           GOBACK.
       END PROGRAM T4107F.

       IDENTIFICATION DIVISION.
       CLASS-ID. C4107F INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           FUNCTION F4107A
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. RF1.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LF USAGE FUNCTION-POINTER TO F4107A.
       PROCEDURE DIVISION RETURNING LF.
           SET LF TO NULL.
       END METHOD RF1.
       END OBJECT.
       END CLASS C4107F.
