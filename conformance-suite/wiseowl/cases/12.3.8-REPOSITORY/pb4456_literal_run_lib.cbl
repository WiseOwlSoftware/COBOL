      *> kb/Work PB4456: the library of pb4456_expansion_literals_at_every_column,
      *> compiled apart (its build step). A class BOX and the PARAMETERIZED class HOLD
      *> USING ELEM whose method SHOW-IT displays a long run of short literals before it
      *> invokes the element: the text of its expansion is one stream of words wrapped at
      *> 200 positions, so a literal opens at every column of a line over the run.
       IDENTIFICATION DIVISION.
       CLASS-ID. P4456BOX AS "P4456-Box" INHERITS FROM BASE.
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
       END CLASS P4456BOX.

       IDENTIFICATION DIVISION.
       CLASS-ID. P4456HOLD AS "P4456-Hold"
           INHERITS FROM BASE USING ELEM.
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
           DISPLAY "a"
           DISPLAY 'xy' "a b"
           DISPLAY '''' """""" "ab"""
           DISPLAY "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y' """" 'xy' "a b" 'x'
           DISPLAY """"""
           DISPLAY 'x' "abc"
           DISPLAY "ab""" X"4142" """a"
           DISPLAY X"41" "ab" '''x' "a"
           DISPLAY 'xy' "a b" 'x' "abc" "a""b"
           DISPLAY X"4142"
           DISPLAY "a""b" X"41"
           DISPLAY """a" 'x''y' """"
           DISPLAY '''x' "a" '''' """"""
           DISPLAY 'x' "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y'
           DISPLAY "ab" '''x'
           DISPLAY """" 'xy' "a b"
           DISPLAY '''' """""" "ab""" X"4142"
           DISPLAY "a""b" X"41" "ab" '''x' "a"
           DISPLAY 'xy'
           DISPLAY "a" ''''
           DISPLAY "a b" 'x' "abc"
           DISPLAY "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab" '''x' "a" '''' """"""
           DISPLAY 'x'
           DISPLAY """""" "ab"""
           DISPLAY "abc" "a""b" X"41"
           DISPLAY """a" 'x''y' """" 'xy'
           DISPLAY "a" '''' """""" "ab""" X"4142"
           DISPLAY "a""b"
           DISPLAY X"4142" """a"
           DISPLAY X"41" "ab" '''x'
           DISPLAY """" 'xy' "a b" 'x'
           DISPLAY """""" "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab"
           DISPLAY 'x''y' """"
           DISPLAY '''x' "a" ''''
           DISPLAY "a b" 'x' "abc" "a""b"
           DISPLAY X"4142" """a" 'x''y' """" 'xy'
           DISPLAY "a"
           DISPLAY 'xy' "a b"
           DISPLAY '''' """""" "ab"""
           DISPLAY "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y' """" 'xy' "a b" 'x'
           DISPLAY """"""
           DISPLAY 'x' "abc"
           DISPLAY "ab""" X"4142" """a"
           DISPLAY X"41" "ab" '''x' "a"
           DISPLAY 'xy' "a b" 'x' "abc" "a""b"
           DISPLAY X"4142"
           DISPLAY "a""b" X"41"
           DISPLAY """a" 'x''y' """"
           DISPLAY '''x' "a" '''' """"""
           DISPLAY 'x' "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y'
           DISPLAY "ab" '''x'
           DISPLAY """" 'xy' "a b"
           DISPLAY '''' """""" "ab""" X"4142"
           DISPLAY "a""b" X"41" "ab" '''x' "a"
           DISPLAY 'xy'
           DISPLAY "a" ''''
           DISPLAY "a b" 'x' "abc"
           DISPLAY "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab" '''x' "a" '''' """"""
           DISPLAY 'x'
           DISPLAY """""" "ab"""
           DISPLAY "abc" "a""b" X"41"
           DISPLAY """a" 'x''y' """" 'xy'
           DISPLAY "a" '''' """""" "ab""" X"4142"
           DISPLAY "a""b"
           DISPLAY X"4142" """a"
           DISPLAY X"41" "ab" '''x'
           DISPLAY """" 'xy' "a b" 'x'
           DISPLAY """""" "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab"
           DISPLAY 'x''y' """"
           DISPLAY '''x' "a" ''''
           DISPLAY "a b" 'x' "abc" "a""b"
           DISPLAY X"4142" """a" 'x''y' """" 'xy'
           DISPLAY "a"
           DISPLAY 'xy' "a b"
           DISPLAY '''' """""" "ab"""
           DISPLAY "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y' """" 'xy' "a b" 'x'
           DISPLAY """"""
           DISPLAY 'x' "abc"
           DISPLAY "ab""" X"4142" """a"
           DISPLAY X"41" "ab" '''x' "a"
           DISPLAY 'xy' "a b" 'x' "abc" "a""b"
           DISPLAY X"4142"
           DISPLAY "a""b" X"41"
           DISPLAY """a" 'x''y' """"
           DISPLAY '''x' "a" '''' """"""
           DISPLAY 'x' "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y'
           DISPLAY "ab" '''x'
           DISPLAY """" 'xy' "a b"
           DISPLAY '''' """""" "ab""" X"4142"
           DISPLAY "a""b" X"41" "ab" '''x' "a"
           DISPLAY 'xy'
           DISPLAY "a" ''''
           DISPLAY "a b" 'x' "abc"
           DISPLAY "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab" '''x' "a" '''' """"""
           DISPLAY 'x'
           DISPLAY """""" "ab"""
           DISPLAY "abc" "a""b" X"41"
           DISPLAY """a" 'x''y' """" 'xy'
           DISPLAY "a" '''' """""" "ab""" X"4142"
           DISPLAY "a""b"
           DISPLAY X"4142" """a"
           DISPLAY X"41" "ab" '''x'
           DISPLAY """" 'xy' "a b" 'x'
           DISPLAY """""" "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab"
           DISPLAY 'x''y' """"
           DISPLAY '''x' "a" ''''
           DISPLAY "a b" 'x' "abc" "a""b"
           DISPLAY X"4142" """a" 'x''y' """" 'xy'
           DISPLAY "a"
           DISPLAY 'xy' "a b"
           DISPLAY '''' """""" "ab"""
           DISPLAY "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y' """" 'xy' "a b" 'x'
           DISPLAY """"""
           DISPLAY 'x' "abc"
           DISPLAY "ab""" X"4142" """a"
           DISPLAY X"41" "ab" '''x' "a"
           DISPLAY 'xy' "a b" 'x' "abc" "a""b"
           DISPLAY X"4142"
           DISPLAY "a""b" X"41"
           DISPLAY """a" 'x''y' """"
           DISPLAY '''x' "a" '''' """"""
           DISPLAY 'x' "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y'
           DISPLAY "ab" '''x'
           DISPLAY """" 'xy' "a b"
           DISPLAY '''' """""" "ab""" X"4142"
           DISPLAY "a""b" X"41" "ab" '''x' "a"
           DISPLAY 'xy'
           DISPLAY "a" ''''
           DISPLAY "a b" 'x' "abc"
           DISPLAY "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab" '''x' "a" '''' """"""
           DISPLAY 'x'
           DISPLAY """""" "ab"""
           DISPLAY "abc" "a""b" X"41"
           DISPLAY """a" 'x''y' """" 'xy'
           DISPLAY "a" '''' """""" "ab""" X"4142"
           DISPLAY "a""b"
           DISPLAY X"4142" """a"
           DISPLAY X"41" "ab" '''x'
           DISPLAY """" 'xy' "a b" 'x'
           DISPLAY """""" "ab""" X"4142" """a" 'x''y'
           DISPLAY "ab"
           DISPLAY 'x''y' """"
           DISPLAY '''x' "a" ''''
           DISPLAY "a b" 'x' "abc" "a""b"
           DISPLAY X"4142" """a" 'x''y' """" 'xy'
           DISPLAY "a"
           DISPLAY 'xy' "a b"
           DISPLAY '''' """""" "ab"""
           DISPLAY "abc" "a""b" X"41" "ab"
           DISPLAY 'x''y' """" 'xy' "a b" 'x'
           DISPLAY """"""
           DISPLAY 'x' "abc"
           DISPLAY "ab""" X"4142" """a"
           DISPLAY X"41" "ab" '''x' "a"
           DISPLAY 'xy' "a b" 'x' "abc" "a""b"
           INVOKE ITEM "SHOW".
       END METHOD SHOW-IT.
       END OBJECT.
       END CLASS P4456HOLD.
