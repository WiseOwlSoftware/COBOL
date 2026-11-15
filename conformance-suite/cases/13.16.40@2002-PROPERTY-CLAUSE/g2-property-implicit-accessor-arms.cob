      *>--- conformance-case
      *> id: g2-property-implicit-accessor-arms
      *> rules: [2002-SR-13.16.40.2-1, 2002-GR-13.16.40.3-1,
      *>     2002-GR-13.16.40.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.16.40.2 SR1 - "The PROPERTY clause may be specified
      *>   only in the working-storage section of a factory definition
      *>   or an instance definition": FP is in the FACTORY's
      *>   working-storage, the others in the OBJECT's; both accepted.
      *> §2002:13.16.40.3 GR1 / GR2 - with neither NO GET nor NO SET the
      *>   clause defines a GET and a SET method, one of three shapes:
      *>   class index, object or pointer: SET LS TO data-name / SET
      *>   data-name TO LS; alphanumeric-, national- or numeric-edited:
      *>   MOVE data-name TO LS (1:) / MOVE LS TO data-name(1:) ("reference
      *>   modification of the receiving item as the whole of itself
      *>   prevents the editing rules from being reapplied"); otherwise
      *>   a plain MOVE. LS has the subject's description without VALUE.
      *> §2002:8.4.2.9.3 GR1/GR2 - a sending property is temp-1 with the
      *>   GET's RETURNING description; a receiving one is temp-2 with
      *>   the SET's USING description, passed to the SET method.
      *> All citations: python scripts/spec/cite.py --check -> OK.
      *> Expected lines, derived:
      *>   FP=07     factory GET returns VALUE 7 (class-name-1 form).
      *>   FP=08     MOVE 8 TO FP OF G2PRKC: factory SET stores 8.
      *>   E=[ 12.50]  MOVE 12.5 to temp-2 (ZZ9.99) edits " 12.50"; the
      *>             SET moves it to E(1:), the GET moves E to LS(1:);
      *>             moved to X(6) unchanged.
      *>   AE=[AB CD ] MOVE "ABCD" to temp-2 (XXBXX) edits "AB CD". The
      *>             SET and GET both move through (1:), so no re-edit.
      *>             FAILS if either accessor used the plain MOVE: editing
      *>             "AB CD" into XXBXX again gives "AB  C" ([AB  C ]).
      *>   NE=SAME   the same for national-edited NNBNN: N"AB CD" back;
      *>             a re-edit would give N"AB  C" (NE=DIFF).
      *>   IX=004    SET IX OF A TO WX (occurrence 4) through the SET
      *>             shape; SET IDX TO IX OF A after SET IDX TO 1 through
      *>             the GET shape; SET WN TO IDX gives 4.
      *>   N=042     the otherwise arm: MOVE 42, read back as 9(3).
      *>   OB=SAME   class object: SET OB OF A TO A, then OB OF A = A.
      *>   PT=SAME   class pointer: SET PT OF A TO ADDRESS OF W, then
      *>             PT OF A = ADDRESS OF W.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRK01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2PRKC
           PROPERTY FP
           PROPERTY E
           PROPERTY AE
           PROPERTY NE
           PROPERTY IX
           PROPERTY N
           PROPERTY OB
           PROPERTY PT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A  USAGE OBJECT REFERENCE G2PRKC.
       01 WX USAGE INDEX.
       01 WN PIC 9(3).
       01 WF PIC 9(2).
       01 WD PIC X(6).
       01 W  PIC X(4) VALUE "WXYZ".
       01 TB.
          05 TE PIC X OCCURS 9 INDEXED BY IDX.
       PROCEDURE DIVISION.
       MAIN-PARA.
           MOVE FP OF G2PRKC TO WF
           DISPLAY "FP=" WF
           MOVE 8 TO FP OF G2PRKC
           MOVE FP OF G2PRKC TO WF
           DISPLAY "FP=" WF
           INVOKE G2PRKC "NEW" RETURNING A
           MOVE 12.5 TO E OF A
           MOVE E OF A TO WD
           DISPLAY "E=[" WD "]"
           MOVE "ABCD" TO AE OF A
           MOVE AE OF A TO WD
           DISPLAY "AE=[" WD "]"
           MOVE N"ABCD" TO NE OF A
           IF NE OF A = N"AB CD"
               DISPLAY "NE=SAME"
           ELSE
               DISPLAY "NE=DIFF"
           END-IF
           SET IDX TO 4
           SET WX TO IDX
           SET IX OF A TO WX
           SET IDX TO 1
           SET IDX TO IX OF A
           SET WN TO IDX
           DISPLAY "IX=" WN
           MOVE 42 TO N OF A
           MOVE N OF A TO WN
           DISPLAY "N=" WN
           SET OB OF A TO A
           IF OB OF A = A
               DISPLAY "OB=SAME"
           ELSE
               DISPLAY "OB=DIFF"
           END-IF
           SET PT OF A TO ADDRESS OF W
           IF PT OF A = ADDRESS OF W
               DISPLAY "PT=SAME"
           ELSE
               DISPLAY "PT=DIFF"
           END-IF
           STOP RUN.
       END PROGRAM G2PRK01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2PRKC INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 FP PIC 9(2) VALUE 7 PROPERTY.
       PROCEDURE DIVISION.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E  PIC ZZ9.99 PROPERTY.
       01 AE PIC XXBXX PROPERTY.
       01 NE PIC NNBNN PROPERTY.
       01 IX USAGE INDEX PROPERTY.
       01 N  PIC 9(3) PROPERTY.
       01 OB USAGE OBJECT REFERENCE PROPERTY.
       01 PT USAGE POINTER PROPERTY.
       PROCEDURE DIVISION.
       END OBJECT.
       END CLASS G2PRKC.
