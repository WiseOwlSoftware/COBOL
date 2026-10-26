*>--- conformance-case
*> id: pb1014-content-boolean-into-numeric-formal
*> rules: [GR-14.8.2.3.3-L2.2]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB1014 -- ISO 14.8.2.3.3 2) a): a numeric formal's
      *> conformance rules are those of a COMPUTE with the argument as
      *> the sending operand, and a boolean value is not a numeric
      *> sending operand. The BY CONTENT boolean literal into the PIC 9(4)
      *> method formal is COBOL0828, so the boolean argument record
      *> (CallEmitter.BooleanArgumentRecord) never has a numeric formal to
      *> fill: the MOVE receiving store it routes through has no numeric
      *> arm for a boolean sender.
       IDENTIFICATION DIVISION.
       CLASS-ID. PB1014N INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS BASE CLASS PB1014N.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       IDENTIFICATION DIVISION.
       METHOD-ID. MN.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "IN-MN"
           GOBACK.
       END METHOD MN.
       END OBJECT.
       END CLASS PB1014N.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1014M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY. CLASS PB1014N.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE PB1014N.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1014N "NEW" RETURNING O
           INVOKE O "MN" USING BY CONTENT B"10"
           STOP RUN.
       END PROGRAM PB1014M.
