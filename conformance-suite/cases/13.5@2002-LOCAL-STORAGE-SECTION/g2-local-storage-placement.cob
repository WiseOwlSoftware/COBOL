      *>--- conformance-case
      *> id: g2-local-storage-placement
      *> rules: [2002-SR-13.5.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.5.2 SR1 - "The local-storage section may be specified
      *> in a program definition or function definition or in a method
      *> definition contained in a class definition." Each of the three
      *> permitted source elements owns a LOCAL-STORAGE SECTION and
      *> uses it. Its items "are initialized as indicated in 13.16.61,
      *> VALUE clause" (§2002:13.5.3 GR2) and are automatic: "Their
      *> storage is allocated and set to initial state each time the
      *> runtime element containing them is activated" (§2002:8.6.3).
      *>   P=[PROG]  the program definition's item L0 VALUE "PROG"
      *>   F1=6 F2=6 function G2LS01F: LF VALUE 5, ADD 1, returned; the
      *>             second activation starts again from 5
      *>   M1=4 M2=4 method BUMP of class G2LS01C: LM VALUE 3, ADD 1,
      *>             returned; the second invocation starts from 3
      *> The refused placements (a factory or instance definition) are
      *> the twin's pb1251-factory-local-storage and
      *> pb1251-object-local-storage.
      *> Why each leg can fail: the section is refused in one of the
      *> three elements, its VALUE is not applied, or the item is kept
      *> static across activations (F2=7 or M2=5).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2LS01F.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LF PIC 9 VALUE 5.
       LINKAGE SECTION.
       01 R PIC 9.
       PROCEDURE DIVISION RETURNING R.
           ADD 1 TO LF.
           MOVE LF TO R.
           GOBACK.
       END FUNCTION G2LS01F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2LS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2LS01C
           FUNCTION G2LS01F.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2LS01C.
       01 W1 PIC 9.
       01 W2 PIC 9.
       LOCAL-STORAGE SECTION.
       01 L0 PIC X(4) VALUE "PROG".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "P=[" L0 "]".
           MOVE FUNCTION G2LS01F TO W1.
           MOVE FUNCTION G2LS01F TO W2.
           DISPLAY "F1=" W1 " F2=" W2.
           INVOKE G2LS01C "NEW" RETURNING O.
           INVOKE O "BUMP" RETURNING W1.
           INVOKE O "BUMP" RETURNING W2.
           DISPLAY "M1=" W1 " M2=" W2.
           STOP RUN.
       END PROGRAM G2LS01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2LS01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 LM PIC 9 VALUE 3.
       LINKAGE SECTION.
       01 RV PIC 9.
       PROCEDURE DIVISION RETURNING RV.
           ADD 1 TO LM.
           MOVE LM TO RV.
       END METHOD BUMP.
       END OBJECT.
       END CLASS G2LS01C.
