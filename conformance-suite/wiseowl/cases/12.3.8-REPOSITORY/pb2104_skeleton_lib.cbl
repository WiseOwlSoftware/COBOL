      *> kb/Work PB2104: the library of pb2104_expand_skeleton_from_repository
      *> and pb2104_expansion_is_one_class_across_groups, compiled apart (their
      *> build step). A class BOX and the PARAMETERIZED class HOLD USING ELEM, in
      *> an assembly of their own: HOLD is published as a skeleton, so a group
      *> that expands it needs no copy of its source.
       IDENTIFICATION DIVISION.
       CLASS-ID. P2104BOX AS "P2104-Box" INHERITS FROM BASE.
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
       END CLASS P2104BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. P2104HOLD AS "P2104-Hold" INHERITS FROM BASE USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELEM.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 ITEM USAGE OBJECT REFERENCE ELEM.
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
       END OBJECT.
       END CLASS P2104HOLD.
