      *> kb/Work PB4650: the library of pb4650_unused_actual_expansion_is_one_class,
      *> compiled apart (its build step). A class BOX and the PARAMETERIZED class
      *> COUNTER USING ELEM, in an assembly of their own. COUNTER never names its
      *> formal ELEM in any member, so the code of an expansion of it holds no
      *> reference to the assembly of its actual. Its FACTORY counts the calls of
      *> BUMP, so how many classes exist in a run unit shows in the count.
       IDENTIFICATION DIVISION.
       CLASS-ID. P4650BOX AS "P4650-Box" INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       END CLASS P4650BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. P4650COUNTER AS "P4650-Counter"
           INHERITS FROM BASE USING ELEM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE
           CLASS ELEM.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9(3) VALUE 0.
       PROCEDURE DIVISION.
       METHOD-ID. BUMP.
       PROCEDURE DIVISION.
           ADD 1 TO N
           DISPLAY "N=" N.
       END METHOD BUMP.
       END FACTORY.
       END CLASS P4650COUNTER.
