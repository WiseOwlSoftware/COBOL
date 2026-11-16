      *>--- conformance-case
      *> id: g2-file-section-placement
      *> rules: [2002-SR-13.3.2-1]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:13.3.2 SR1 - "The file section may be specified in a
      *> function definition or a program definition. Within a class
      *> definition, the file section may be specified only in a
      *> factory definition or an instance definition". Each of the
      *> four permitted source elements here owns a FILE SECTION with
      *> its own FD (the factory and instance definitions take an
      *> environment division of their own, §2002:10.5.1), and each
      *> element writes one record to its file, closes it, reopens it
      *> for input and reads the record back:
      *>   P=[PROG]  the program definition's file G2FSP
      *>   F=[FUNC]  the function definition G2FS01F's file G2FSF
      *>   C=[FACT]  the factory definition's file G2FSC (method FWR)
      *>   O=[INST]  the instance definition's file G2FSO (method OWR)
      *> The refused placements of the same rule (a method definition,
      *> an interface definition) are g2-file-section-in-method and
      *> g2-file-section-in-interface.
      *> Why each leg can fail: the FILE SECTION is refused in one of
      *> the four elements, or its FD is not bound to the element that
      *> declares it (the read returns another element's record or
      *> spaces).
       IDENTIFICATION DIVISION.
       FUNCTION-ID. G2FS01F.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G2FSF ASSIGN TO "g2fs01f.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD G2FSF.
       01 FREC PIC X(4).
       LINKAGE SECTION.
       01 R PIC X(4).
       PROCEDURE DIVISION RETURNING R.
           OPEN OUTPUT G2FSF.
           MOVE "FUNC" TO FREC.
           WRITE FREC.
           CLOSE G2FSF.
           MOVE SPACES TO FREC.
           OPEN INPUT G2FSF.
           READ G2FSF.
           MOVE FREC TO R.
           CLOSE G2FSF.
           GOBACK.
       END FUNCTION G2FS01F.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2FS01.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2FS01C
           FUNCTION G2FS01F.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G2FSP ASSIGN TO "g2fs01p.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD G2FSP.
       01 PREC PIC X(4).
       WORKING-STORAGE SECTION.
       01 O USAGE OBJECT REFERENCE G2FS01C.
       01 W PIC X(4).
       PROCEDURE DIVISION.
       MAIN.
           OPEN OUTPUT G2FSP.
           MOVE "PROG" TO PREC.
           WRITE PREC.
           CLOSE G2FSP.
           MOVE SPACES TO PREC.
           OPEN INPUT G2FSP.
           READ G2FSP.
           MOVE PREC TO W.
           CLOSE G2FSP.
           DISPLAY "P=[" W "]".
           MOVE FUNCTION G2FS01F TO W.
           DISPLAY "F=[" W "]".
           MOVE SPACES TO W.
           INVOKE G2FS01C "FWR" RETURNING W.
           DISPLAY "C=[" W "]".
           INVOKE G2FS01C "NEW" RETURNING O.
           MOVE SPACES TO W.
           INVOKE O "OWR" RETURNING W.
           DISPLAY "O=[" W "]".
           STOP RUN.
       END PROGRAM G2FS01.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2FS01C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       FACTORY.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G2FSC ASSIGN TO "g2fs01c.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD G2FSC.
       01 CREC PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. FWR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV PIC X(4).
       PROCEDURE DIVISION RETURNING RV.
           OPEN OUTPUT G2FSC.
           MOVE "FACT" TO CREC.
           WRITE CREC.
           CLOSE G2FSC.
           MOVE SPACES TO CREC.
           OPEN INPUT G2FSC.
           READ G2FSC.
           MOVE CREC TO RV.
           CLOSE G2FSC.
       END METHOD FWR.
       END FACTORY.
       IDENTIFICATION DIVISION.
       OBJECT.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT G2FSO ASSIGN TO "g2fs01o.dat"
               ORGANIZATION IS SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD G2FSO.
       01 OREC PIC X(4).
       PROCEDURE DIVISION.
       METHOD-ID. OWR.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RV PIC X(4).
       PROCEDURE DIVISION RETURNING RV.
           OPEN OUTPUT G2FSO.
           MOVE "INST" TO OREC.
           WRITE OREC.
           CLOSE G2FSO.
           MOVE SPACES TO OREC.
           OPEN INPUT G2FSO.
           READ G2FSO.
           MOVE OREC TO RV.
           CLOSE G2FSO.
       END METHOD OWR.
       END OBJECT.
       END CLASS G2FS01C.
