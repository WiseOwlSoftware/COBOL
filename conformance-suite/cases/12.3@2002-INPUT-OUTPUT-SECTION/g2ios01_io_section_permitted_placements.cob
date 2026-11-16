      *>--- conformance-case
      *> id: g2ios01_io_section_permitted_placements
      *> rules: [2002-SR-12.3.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:12.3.2 SR1 - the input-output section in each of the four
      *> source elements that may hold one: a function definition, a
      *> program definition, a factory definition, an instance definition.
      *>   cite.py --check "§2002:12.3.2 SR1" "Within a class definition,
      *>   the input-output section may be specified only in a factory
      *>   definition or instance definition, but not in a method
      *>   definition" -> OK
      *>   cite.py --check "§2002:12.3.2 SR1" "The input-output section
      *>   shall not be specified within an interface definition" -> OK
      *> The three prohibitions are the refusals
      *>   g2ios02-io-section-in-method, g2ios03-io-section-in-class-outer
      *>   and g2ios04-io-section-in-interface.
      *> Each owner declares its own sequential file, writes one record
      *> that names the owner, reads it back and reports it. The factory
      *> and object methods reach their definition's file because "a
      *> constant-name, data-name or file-name declared in a source element
      *> for an object definition, whether factory or instance, is a
      *> global name" (§2002:8.4.5.1.1, cite.py OK).
      *> DERIVATION OF THE EXPECTED OUTPUT: each file is OPEN OUTPUT,
      *> written once, closed, OPEN INPUT and read once; a sequential READ
      *> returns the records in the order written (§2002:12.3.4.4.3 GR2a,
      *> cite.py OK), so the record read is the one written:
      *>   FUNCTION=FUNC, PROGRAM=PROG, FACTORY=FACT, OBJECT=OBJT.
      *> A leg fails if its input-output section is refused at 2002, or if
      *> its file connector does not work (the record is not read back).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2IOSFN.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FN-F ASSIGN TO "G2IOSFN.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  FN-F.
       01  FN-R        PIC X(4).
       LINKAGE SECTION.
       01  RES         PIC X(4).
       PROCEDURE DIVISION RETURNING RES.
       MAIN-FN.
           OPEN OUTPUT FN-F.
           MOVE "FUNC" TO FN-R.
           WRITE FN-R.
           CLOSE FN-F.
           OPEN INPUT FN-F.
           READ FN-F AT END MOVE "EOF " TO FN-R END-READ.
           MOVE FN-R TO RES.
           CLOSE FN-F.
           GOBACK.
       END FUNCTION G2IOSFN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2IOS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2IOSK
           FUNCTION G2IOSFN.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT P-F ASSIGN TO "G2IOSP.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  P-F.
       01  P-R         PIC X(4).
       WORKING-STORAGE SECTION.
       01  W           PIC X(4).
       01  O           USAGE OBJECT REFERENCE G2IOSK.
       PROCEDURE DIVISION.
       MAIN-P.
           MOVE FUNCTION G2IOSFN TO W.
           DISPLAY "FUNCTION=" W.
           OPEN OUTPUT P-F.
           MOVE "PROG" TO P-R.
           WRITE P-R.
           CLOSE P-F.
           OPEN INPUT P-F.
           READ P-F AT END MOVE "EOF " TO P-R END-READ.
           DISPLAY "PROGRAM=" P-R.
           CLOSE P-F.
           INVOKE G2IOSK "RUNF" RETURNING W.
           DISPLAY "FACTORY=" W.
           INVOKE G2IOSK "NEW" RETURNING O.
           INVOKE O "RUNO" RETURNING W.
           DISPLAY "OBJECT=" W.
           STOP RUN.
       END PROGRAM G2IOS01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2IOSK INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT FA-F ASSIGN TO "G2IOSF.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  FA-F.
       01  FA-R        PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. RUNF.
       DATA DIVISION.
       LINKAGE SECTION.
       01  RES         PIC X(4).
       PROCEDURE DIVISION RETURNING RES.
       MAIN-F.
           OPEN OUTPUT FA-F.
           MOVE "FACT" TO FA-R.
           WRITE FA-R.
           CLOSE FA-F.
           OPEN INPUT FA-F.
           READ FA-F AT END MOVE "EOF " TO FA-R END-READ.
           MOVE FA-R TO RES.
           CLOSE FA-F.
       END METHOD RUNF.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT OB-F ASSIGN TO "G2IOSO.DAT".
       DATA DIVISION.
       FILE SECTION.
       FD  OB-F.
       01  OB-R        PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. RUNO.
       DATA DIVISION.
       LINKAGE SECTION.
       01  RES         PIC X(4).
       PROCEDURE DIVISION RETURNING RES.
       MAIN-O.
           OPEN OUTPUT OB-F.
           MOVE "OBJT" TO OB-R.
           WRITE OB-R.
           CLOSE OB-F.
           OPEN INPUT OB-F.
           READ OB-F AT END MOVE "EOF " TO OB-R END-READ.
           MOVE OB-R TO RES.
           CLOSE OB-F.
       END METHOD RUNO.
       END OBJECT.
       END CLASS G2IOSK.
