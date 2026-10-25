*>--- conformance-case
*> id: pb3162_external_prototype_pointer_set_and_pass
*> rules: [SR-14.9.39.3-22, GR-14.8.2.3.3-2, GR-14.8.2.3.2-4]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3162 - a SET of one restricted program-pointer into another and the passing of the same pointer to a
      *> formal of the receiver's type answer the same question, so they give the same answer.
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
      *> X3162A and X3162B are REPOSITORY prototypes with no definition in the compilation group, so neither has a
      *> compile-time signature; the run unit resolves them. Nothing shows the two types differ, so the SET is admitted
      *> (it always was) and so are both CALLs, BY REFERENCE and BY CONTENT, to U3162's formal of type TB (before the
      *> fix both raised EC-PROGRAM-ARG-MISMATCH under checking). The control: an UNRESTRICTED program-pointer passed BY
      *> CONTENT to the same formal is still refused at run time (SR22's "shall reference a program-pointer and the
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
