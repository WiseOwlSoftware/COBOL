*>--- conformance-case
*> id: pb1522_replace_before_parameterized_expansion
*> rules: [DOC-A.1-201]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1522 -- the ORDER of the REPLACE statement and the expansion of a parameterized class.
      *> ISO 7.1: "The manner and time of expansion of parameterized classes and parameterized interfaces is
      *> defined by the implementor, except that it occurs after the text manipulation stage of processing."
      *> ISO 7.2.1 Step 3: "The conditionally-processed compilation group is read and the replacing actions of
      *> REPLACE statements are applied in order."  So a REPLACE rewrites the text of a parameterized
      *> definition BEFORE it is expanded, and it never sees the text an expansion produces.  WiseOwl COBOL
      *> documents the same in docs/CONFORMANCE.md section 7 (DOC-A.1-201).
      *>
      *> The program makes one REPLACE active over the parameterized class PB1522HOLD and cancels it (REPLACE OFF,
      *> 7.2.4.4 GR7 d)) before the program that expands the class:
      *>     REPLACE ==USAGE OBJECT REFERENCE PB1522BOX== BY ==USAGE OBJECT REFERENCE PB1522TXT==.
      *> That pseudo-text-1 occurs in the definition's SOURCE text exactly once (the item FIXED), and a second
      *> time ONLY in the text the expansion produces (the items ITEM and X are written USAGE OBJECT REFERENCE
      *> ELEM; the actual parameter PB1522BOX replaces ELEM at expansion, 12.3.8.4 GR5).
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES -- NOT FROM A RUN:
      *>
      *> 1. HB "PUT" USING B, then "SHOW-IT".  X and ITEM are typed ELEM in the source, so the REPLACE leaves
      *>    them alone (it finds no PB1522BOX there) and the expansion makes them PB1522BOX.  B holds a
      *>    PB1522BOX object whose V is 42, so SHOW-IT invokes PB1522BOX's SHOW:                  BOX 042
      *>    DISCRIMINATOR: a REPLACE applied AFTER expansion would find USAGE OBJECT REFERENCE PB1522BOX in
      *>    ITEM and X and retype them PB1522TXT, and INVOKE HB "PUT" USING B would then pass a PB1522BOX
      *>    object to a PB1522TXT formal and be refused (9.3.6 match rules) -- the program would not compile.
      *>
      *> 2. HB "SETFIX" USING T, then "SHOW-FIX".  FIXED is written USAGE OBJECT REFERENCE PB1522BOX in the
      *>    source, the REPLACE is active there, so the stage-3 text of the definition is USAGE OBJECT
      *>    REFERENCE PB1522TXT, and the expansion compiles that text.  SET FIXED TO Y (Y is a PB1522TXT
      *>    reference) is then a conforming SET (14.9.39.3 SR12 a) 2.), and FIXED holds T, a PB1522TXT object,
      *>    so SHOW-FIX invokes PB1522TXT's SHOW:                                                  TXT
      *>    DISCRIMINATOR: an expansion that took the definition's text from BEFORE the REPLACE would leave
      *>    FIXED typed PB1522BOX and SET FIXED TO Y would be refused (a PB1522TXT sender into a PB1522BOX
      *>    receiver names an unrelated class).
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1522BOX INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 V PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. SETV.
       DATA DIVISION.
       LINKAGE SECTION.
       01 NUM PIC 9(3).
       PROCEDURE DIVISION USING NUM.
           MOVE NUM TO V.
       END METHOD SETV.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "BOX " V.
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1522BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1522TXT INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. SHOW.
       PROCEDURE DIVISION.
           DISPLAY "TXT".
       END METHOD SHOW.
       END OBJECT.
       END CLASS PB1522TXT.

       REPLACE ==USAGE OBJECT REFERENCE PB1522BOX== BY ==USAGE OBJECT REFERENCE PB1522TXT==.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1522HOLD INHERITS FROM BASE USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELEM
           CLASS PB1522BOX
           CLASS PB1522TXT.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE ELEM.
       01 FIXED USAGE OBJECT REFERENCE PB1522BOX.
       PROCEDURE DIVISION.
       METHOD-ID. PUT.
       DATA DIVISION.
       LINKAGE SECTION.
       01 X USAGE OBJECT REFERENCE ELEM.
       PROCEDURE DIVISION USING X.
           SET ITEM TO X.
       END METHOD PUT.
       METHOD-ID. SHOW-IT.
       PROCEDURE DIVISION.
           INVOKE ITEM "SHOW".
       END METHOD SHOW-IT.
       METHOD-ID. SETFIX.
       DATA DIVISION.
       LINKAGE SECTION.
       01 Y USAGE OBJECT REFERENCE PB1522TXT.
       PROCEDURE DIVISION USING Y.
           SET FIXED TO Y.
       END METHOD SETFIX.
       METHOD-ID. SHOW-FIX.
       PROCEDURE DIVISION.
           INVOKE FIXED "SHOW".
       END METHOD SHOW-FIX.
       END OBJECT.
       END CLASS PB1522HOLD.
       REPLACE OFF.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1522M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1522BOX
           CLASS PB1522TXT
           CLASS PB1522HOLD
           CLASS PB1522HOLD-BOX EXPANDS PB1522HOLD USING PB1522BOX.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 B  USAGE OBJECT REFERENCE PB1522BOX.
       01 T  USAGE OBJECT REFERENCE PB1522TXT.
       01 HB USAGE OBJECT REFERENCE PB1522HOLD-BOX.
       PROCEDURE DIVISION.
           INVOKE PB1522BOX "NEW" RETURNING B.
           INVOKE B "SETV" USING 42.
           INVOKE PB1522TXT "NEW" RETURNING T.
           INVOKE PB1522HOLD-BOX "NEW" RETURNING HB.
           INVOKE HB "PUT" USING B.
           INVOKE HB "SHOW-IT".
           INVOKE HB "SETFIX" USING T.
           INVOKE HB "SHOW-FIX".
           STOP RUN.
       END PROGRAM PB1522M.
