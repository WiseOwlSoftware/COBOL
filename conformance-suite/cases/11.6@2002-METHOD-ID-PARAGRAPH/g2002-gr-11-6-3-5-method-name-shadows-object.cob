      *>--- conformance-case
      *> id: g2002-gr-11-6-3-5-method-name-shadows-object
      *> rules: [2002-GR-11.6.3-5]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.3 GR5 - "If a given user-defined word is defined in
      *> the data division of this method definition and in the data
      *> division of the containing object definition, the use of that
      *> word in this method refers to the declaration in this method.
      *> The declaration in the containing object definition is
      *> inaccessible to this method." A method's data division holds
      *> local-storage and linkage (§2002:13.5.2 SR1; §2002:13.4.2 SR1
      *> bars working-storage in a method). The object G2SHCL declares
      *> W = "OBJ-W" and P = "OBJ-P" in its working-storage. Expected:
      *>   "S1:MTH-W" - SHADOW's local-storage W (VALUE "MTH-W") is the
      *>                W SHADOW refers to, not the object's;
      *>   "S2:NEW-W" - SHADOW moves "NEW-W" to W: its own W;
      *>   "T:ARG-P"  - TAKE's linkage P (USING P, the caller passes
      *>                "ARG-P") is the P TAKE refers to;
      *>   "O:OBJ-W"  - OBJW declares no W, so W is the object's, and
      *>                the object's W is unchanged by SHADOW's MOVE
      *>                (the object's declaration was inaccessible to
      *>                SHADOW);
      *>   "O:OBJ-P"  - likewise P is the object's, untouched by TAKE.
      *> Each S/T line fails if the object's declaration answers inside
      *> a method that declares the word; each O line fails if a
      *> method's declaration leaks to the object or a sibling method.
       IDENTIFICATION DIVISION.
       CLASS-ID. G2SHCL INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W PIC X(5) VALUE "OBJ-W".
       01 P PIC X(5) VALUE "OBJ-P".
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. SHADOW.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 W PIC X(5) VALUE "MTH-W".
       PROCEDURE DIVISION.
           DISPLAY "S1:" W.
           MOVE "NEW-W" TO W.
           DISPLAY "S2:" W.
       END METHOD SHADOW.
       IDENTIFICATION DIVISION.
       METHOD-ID. TAKE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 P PIC X(5).
       PROCEDURE DIVISION USING P.
           DISPLAY "T:" P.
       END METHOD TAKE.
       IDENTIFICATION DIVISION.
       METHOD-ID. OBJW.
       PROCEDURE DIVISION.
           DISPLAY "O:" W.
           DISPLAY "O:" P.
       END METHOD OBJW.
       END OBJECT.
       END CLASS G2SHCL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2SH01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2SHCL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2SHCL.
       01 W-A PIC X(5) VALUE "ARG-P".
       PROCEDURE DIVISION.
           INVOKE G2SHCL "NEW" RETURNING O.
           INVOKE O "SHADOW".
           INVOKE O "TAKE" USING W-A.
           INVOKE O "OBJW".
           STOP RUN.
       END PROGRAM G2SH01.
