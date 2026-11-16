      *>--- conformance-case
      *> id: g2-linkage-section-placement
      *> rules: [2002-SR-13.6.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.6.2 SR1 - "The linkage section may be specified in a
      *> program definition, function definition, method definition,
      *> program prototype definition, or function prototype
      *> definition." All five source elements here own a LINKAGE
      *> SECTION, and each definition's formal parameters are used.
      *> The prototypes precede every other source unit
      *> (§2002:10.5.2 SR1) and match their definitions' signatures
      *> (§2002:10.5.2 SR2, SR3). By reference "the activated runtime
      *> element operates as if the formal parameter occupies the same
      *> storage area as the argument" (§2002:14.1.3 GR8):
      *>   P=21  program G2LK01S (prototype G2LK01S) adds 1 to its
      *>         formal PA; the caller's X (VALUE 20) is that storage
      *>   F=42  function G2LK01F (prototype G2LK01F) returns FA * 2
      *>         with FA = X = 21
      *>   M=63  method TRIPLE of class G2LK01C returns MA * 3, MA = 21
      *> The refused placements (a factory or instance definition) are
      *> the twin's pb1251-factory-linkage and pb1251-object-linkage.
      *> Why each leg can fail: a LINKAGE SECTION is refused in one of
      *> the five elements, a prototype is not matched to its
      *> definition, or a formal is not bound to its argument.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK01S IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PA PIC 99.
       PROCEDURE DIVISION USING PA.
       END PROGRAM G2LK01S.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LK01F IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FA PIC 99.
       01 FR PIC 99.
       PROCEDURE DIVISION USING FA RETURNING FR.
       END FUNCTION G2LK01F.

       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LK01F.
       DATA DIVISION.
       LINKAGE SECTION.
       01 FA PIC 99.
       01 FR PIC 99.
       PROCEDURE DIVISION USING FA RETURNING FR.
           COMPUTE FR = FA * 2.
           GOBACK.
       END FUNCTION G2LK01F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2LK01C
           PROGRAM G2LK01S
           FUNCTION G2LK01F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2LK01C.
       01 X PIC 99 VALUE 20.
       01 Y PIC 99.
       PROCEDURE DIVISION.
       MAIN.
           CALL G2LK01S USING X.
           DISPLAY "P=" X.
           COMPUTE Y = FUNCTION G2LK01F(X).
           DISPLAY "F=" Y.
           INVOKE G2LK01C "NEW" RETURNING O.
           INVOKE O "TRIPLE" USING X RETURNING Y.
           DISPLAY "M=" Y.
           STOP RUN.
       END PROGRAM G2LK01.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LK01S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 PA PIC 99.
       PROCEDURE DIVISION USING PA.
           ADD 1 TO PA.
           GOBACK.
       END PROGRAM G2LK01S.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2LK01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. TRIPLE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 MA PIC 99.
       01 MR PIC 99.
       PROCEDURE DIVISION USING MA RETURNING MR.
           COMPUTE MR = MA * 3.
       END METHOD TRIPLE.
       END OBJECT.
       END CLASS G2LK01C.
