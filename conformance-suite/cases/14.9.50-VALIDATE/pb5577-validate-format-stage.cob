      *>--- conformance-case
      *> id: pb5577-validate-format-stage
      *> rules: [GR-14.9.50.4-2, GR-14.9.50.4-3, GR-14.9.50.4-5, GR-14.9.50.4-6, GR-14.9.50.4-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO §2023:14.9.50 VALIDATE (ISO/IEC 1989:2002 §14.8.46, where the facility is a required element), the FORMAT
      *> VALIDATION stage and the error indication that follows it.
      *>   GR6 a) "Each elementary data item is checked for compatibility with the PICTURE, SIGN, and USAGE clauses, as
      *>   applicable, in the item's data description. ... If any data item fails these checks, the data item's internal
      *>   indicator is set to invalid on format."
      *>   GR6 e) "If any of these internal indicators is set to invalid on format, an EC-VALIDATE-FORMAT exception
      *>   condition is set to exist." EC-VALIDATE-FORMAT is NONFATAL (Table 13), so execution continues after the
      *>   declarative (GR6: "execution continues as if the exception condition had not been set to exist").
      *>   GR5 "the content of the invalid data item does not change".
      *>   GR3 "If more than one identifier-1 is specified ... the result ... is the same as if a separate VALIDATE
      *>   statement had been written for each identifier-1 in the same order as specified".
      *>   GR7 b) "each occurrence of the data item [is] processed independently of the other occurrences"; c) an
      *>   OCCURS DEPENDING ON item's value "establishes ... the number of occurrences of the data item to be processed".
      *>   cite.py: OK  §2023:14.9.50.4 6)  (General rules)
      *> The records are filled with a group MOVE, which stores the characters as written (the numeric items then hold
      *> their character image, §2023:14.6.9.1 group move), so a numeric item can hold a non-digit. WHY EACH LEG CAN FAIL:
      *> the expected lines name a declarative run (EC) only where a GR6 a) check fails; a validator that ignored the
      *> table, the DEPENDING count, the alphabetic item or the second identifier prints a different line count, and one
      *> that changed the content would change the DISPLAYed record.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5577A.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  CNT       PIC 9 VALUE 2.
       01  REC-A.
           05  A-TYPE    PIC 99.
           05  A-NAME    PIC A(3).
           05  A-WEEK    PIC 99 OCCURS 3.
           05  A-VAR     PIC 9 OCCURS 1 TO 3 DEPENDING ON CNT.
       01  REC-B.
           05  B-CODE    PIC 99.
       PROCEDURE DIVISION.
       DECLARATIVES.
       D-SEC SECTION.
           USE AFTER EXCEPTION CONDITION EC-VALIDATE-FORMAT.
       D-PARA.
           DISPLAY "  EC-VALIDATE-FORMAT".
       END DECLARATIVES.
       MAIN SECTION.
       M-PARA.
           >>TURN EC-VALIDATE CHECKING ON
           MOVE "12ABC010203123" TO REC-A
           DISPLAY "valid"
           VALIDATE REC-A
           DISPLAY "A-TYPE not numeric"
           MOVE "1xABC010203123" TO REC-A
           VALIDATE REC-A
           DISPLAY "A-NAME not alphabetic"
           MOVE "12AB4010203123" TO REC-A
           VALIDATE REC-A
           DISPLAY "A-WEEK(2) not numeric"
           MOVE "12ABC01x203123" TO REC-A
           VALIDATE REC-A
           DISPLAY "A-VAR(3) not numeric but past the count"
           MOVE "12ABC01020312x" TO REC-A
           VALIDATE REC-A
           DISPLAY "A-VAR(2) not numeric"
           MOVE "12ABC0102031x3" TO REC-A
           VALIDATE REC-A
           DISPLAY "one subscripted occurrence, valid"
           MOVE "12ABC01x203123" TO REC-A
           VALIDATE A-WEEK (1)
           DISPLAY "two identifiers, both invalid"
           MOVE "x1" TO REC-B
           MOVE "1xABC010203123" TO REC-A
           VALIDATE REC-A REC-B
           DISPLAY "content unchanged: " REC-A " " REC-B
           >>TURN EC-VALIDATE CHECKING OFF
           DISPLAY "checking off"
           VALIDATE REC-A
           DISPLAY "done"
           STOP RUN.
