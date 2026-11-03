      *>--- conformance-case
      *> id: pb2104_import_class_under_another_word
      *> rules: [SR-12.3.8.3-6, GR-12.3.8.4-6]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2104_ledger_lib.cbl]
      *> build: ["pb2104_ledger_lib.cbl > P2104LIB", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2104 (DESIGN-external-repository 8.4, 10.1; docs/
      *> CONFORMANCE.md DOC-A.1-161). 12.3.8.3 syntax rule 6: "b) If
      *> literal-1 is not specified, there shall be information in the
      *> external repository for the class object-class-name-1", and
      *> "a) If literal-1 is specified, there shall be information in
      *> the external repository for the class literal-1". 12.3.8.4
      *> general rule 6: "The compiler shall use the information
      *> specified for object-class-name-1 together with the external
      *> repository to determine the details of the class that is to
      *> be used." The class P2104-Ledger is defined by another
      *> compilation group (the build step) and this program names it
      *> by ANOTHER word, ACCT, through AS "P2104-Ledger": the
      *> repository holds the class under its externalized name, so
      *> the class is found, its methods are invoked and its objects
      *> are passed to the other group's program P2104SB, whose
      *> formal is an OBJECT REFERENCE of "the same object-class-name"
      *> (14.8.2.3.2 rule 3): the class is one class whatever word
      *> each group gives it, so the activation conforms and the
      *> EC-PROGRAM-ARG-MISMATCH check at the CALL (14.9.4.4 GR3 d))
      *> sets no exception. The word ACCT is this program's alone, so
      *> a run-time description that spelled it would differ from
      *> P2104SB's LEDGER spelling; the description carries the
      *> externalized name. Each line can fail: 01 if the import,
      *> the NEW factory method or an imported method's argument
      *> crossing is wrong, 02 if the descriptions differ.
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2104IM.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS ACCT AS "P2104-Ledger"
           PROGRAM P2104SB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 A OBJECT REFERENCE ACCT.
       01 AMT PIC 9(4) VALUE 25.
       01 B PIC 9(4).
       PROCEDURE DIVISION.
           INVOKE ACCT "New" RETURNING A
           INVOKE A "DEPOSIT" USING AMT
           INVOKE A "DEPOSIT" USING AMT
           INVOKE A "BALANCE" RETURNING B
           DISPLAY "01 BALANCE " B
           CALL P2104SB USING A
               ON EXCEPTION DISPLAY "02 ARG MISMATCH"
               NOT ON EXCEPTION DISPLAY "02 CONFORMS"
           END-CALL
           STOP RUN.
       END PROGRAM P2104IM.
