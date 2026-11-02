*>--- conformance-case
*> id: pb3162_external_prototype_pointer_set_and_pass
*> rules: [SR-14.9.39.3-22, GR-14.8.2.3.3-2, GR-14.8.2.3.2-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3162 - a SET of one restricted program-pointer into another and the BY CONTENT passing of the same
      *> pointer to a formal of the receiver's type answer the same question (the SET's), so they give the same answer.
      *> The BY REFERENCE passing asks another one (kb/Work PB4107).
      *> 14.9.39.3 SR22: "If identifier-7 references a restricted program-pointer, identifier-8 shall be the predefined
      *>   address NULL or shall reference a program-pointer and the program-prototypes associated with identifier-7 and
      *>   identifier-8 shall have the same signature."   cite.py: OK  14.9.39.3 22)
      *> 14.8.2.3.3: "If the formal parameter is of class pointer or an object reference described without the
      *>   ACTIVE-CLASS phrase, the conformance rules shall be the same as if a SET statement were performed in the
      *>   activating runtime element with the argument as the sending operand and the corresponding formal parameter as
      *>   the receiving operand."   cite.py: OK  14.8.2.3.3
      *> 14.8.2.3.2: "If either is a restricted pointer, both shall be restricted and of the same type."
      *>   cite.py: OK  14.8.2.3.2
      *> 12.3.8.4 GR10 c): the details of a program prototype that is neither in the compilation group nor otherwise
      *>   known "are taken from the external repository"   cite.py: OK  12.3.8.4 10) c)
      *> 8.5.3.1: "Two typed items are of the same type when:" "The items are described with TYPE clauses that reference
      *>   equivalent type declarations"   cite.py: OK  8.5.3.1
      *> 14.9.4.4 GR3 d): "If a violation of these rules is detected, the EC-PROGRAM-ARG-MISMATCH exception condition is
      *>   set to exist"   cite.py: OK  14.9.4.4 3) d)
      *> X3162A and X3162B are REPOSITORY prototypes with no definition in the compilation group, so neither has a
      *> compile-time signature; the run unit resolves them. Nothing shows the two signatures differ, so the SET is
      *> admitted, and so is the BY CONTENT CALL to U3162's formal of type TB (before kb/Work PB3162 it raised
      *> EC-PROGRAM-ARG-MISMATCH under checking). The BY REFERENCE CALL is not: PA is of type TA and the formal of type
      *> TB, two type-names restricting to two prototypes, so they are not "of the same type" whatever the signatures
      *> (kb/Work PB4107). U3162 is specified later in the group, so the prototype is GR10 c)'s and the check is GR3 d)'s
      *> at the activation: EC-PROGRAM-ARG-MISMATCH. The control: an UNRESTRICTED program-pointer passed BY CONTENT to
      *> the same formal is refused at run time too (SR22's "shall reference a program-pointer and the
      *> program-prototypes associated with" both: an unrestricted pointer has none), EC-PROGRAM-ARG-MISMATCH.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. T3162.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM X3162A
           PROGRAM X3162B
           PROGRAM U3162.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TA TYPEDEF USAGE PROGRAM-POINTER TO X3162A.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO X3162B.
       01 PA TYPE TA.
       01 PB TYPE TB.
       01 PU USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
           SET PB TO PA
           DISPLAY "set ok"
           CALL U3162 USING BY REFERENCE PA
               ON EXCEPTION DISPLAY "ref exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "ref ok"
           END-CALL
           CALL "U3162" USING BY CONTENT PA
               ON EXCEPTION DISPLAY "content exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "content ok"
           END-CALL
           CALL "U3162" USING BY CONTENT PU
               ON EXCEPTION DISPLAY "unrestricted exc " FUNCTION EXCEPTION-STATUS
               NOT ON EXCEPTION DISPLAY "unrestricted ok"
           END-CALL
           GOBACK.
       END PROGRAM T3162.
       >>TURN EC-ALL CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. U3162.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM X3162B.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 TB TYPEDEF USAGE PROGRAM-POINTER TO X3162B.
       LINKAGE SECTION.
       01 F TYPE TB.
       PROCEDURE DIVISION USING F.
           DISPLAY "in sub"
           GOBACK.
       END PROGRAM U3162.
