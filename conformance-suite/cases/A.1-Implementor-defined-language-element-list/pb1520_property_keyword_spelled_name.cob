*>--- conformance-case
*> id: pb1520_property_keyword_spelled_name
*> rules: [DOC-A.1-121]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> Annex A.1 item 121 (docs/CONFORMANCE.md DOC-A.1-121, ISO 11.7.4 GR1 a): "If the PROPERTY clause is
      *> specified, the name is implementor-defined"): WiseOwl COBOL names the accessors __GET_<P> and __SET_<P>
      *> for EVERY legal property-name, one spelled like a .NET keyword included -- GET PROPERTY long is __GET_LONG,
      *> never an altered or escaped spelling.  The earlier PARTIAL verdict rested on a probe in which the generated
      *> C# held `long __GET_@LONG()` and Roslyn refused it (CS1002); this golden is the committed witness that
      *> the name now composes once, from the whole word, and that the program runs.  kb/Work PB1520.
      *>
      *> EXPECTED OUTPUT, DERIVED FROM THE RULES -- NOT FROM A RUN:
      *> ISO 8.4.3.9.1: "A get property method is a method explicitly defined with the GET PROPERTY phrase or a
      *> method implicitly generated for a data item described with the PROPERTY clause; a set property method is
      *> a method explicitly defined with the SET PROPERTY phrase or ..." and a property reference gets through the
      *> get method and puts through the set method.
      *> 1. DISPLAY LONG OF A gets through the explicit GET PROPERTY LONG method, which returns W-LONG (7):  00007
      *> 2. MOVE 555 TO LONG OF A puts through the explicit SET PROPERTY LONG method (W-LONG := 555):          00555
      *> 3. DISPLAY INT OF A gets through the implicit accessor of the data item INT (VALUE 100):         00100
      *> 4. ADD 20 TO INT OF A reads then puts through the implicit accessors:                                00120
      *> DISCRIMINATOR: a compiler that spells the accessor with the keyword escaped (__GET_@LONG) or that fails to
      *> translate the generated class prints nothing at all.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1520P.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB1520C
           PROPERTY LONG
           PROPERTY INT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A USAGE OBJECT REFERENCE PB1520C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE PB1520C "NEW" RETURNING A.
           DISPLAY LONG OF A.
           MOVE 555 TO LONG OF A.
           DISPLAY LONG OF A.
           DISPLAY INT OF A.
           ADD 20 TO INT OF A.
           DISPLAY INT OF A.
           STOP RUN.
       END PROGRAM PB1520P.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB1520C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 W-LONG PIC 9(5) VALUE 7.
       01 INT PIC 9(5) VALUE 100 PROPERTY.
       PROCEDURE DIVISION.
       METHOD-ID. GET PROPERTY LONG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-R PIC 9(5).
       PROCEDURE DIVISION RETURNING LK-R.
       MAIN.
           MOVE W-LONG TO LK-R.
       END METHOD.
       METHOD-ID. SET PROPERTY LONG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 LK-V PIC 9(5).
       PROCEDURE DIVISION USING LK-V.
       MAIN.
           MOVE LK-V TO W-LONG.
       END METHOD.
       END OBJECT.
       END CLASS PB1520C.
