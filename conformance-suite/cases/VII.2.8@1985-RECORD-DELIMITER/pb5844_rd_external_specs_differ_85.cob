      *>--- conformance-case
      *> id: pb5844_rd_external_specs_differ_85
      *> rules: [1985-GR-VII.2.8.4-6,
      *>    DOC-A.1-149]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:VII.2.8.4 GR6: "If the associated file connector is an
      *> external file connector, all RECORD DELIMITER clauses in the run
      *> unit which are associated with that file connector must have the
      *> same specifications." The two programs describe one external
      *> file connector, SEQ-FILE, and its RECORD DELIMITER clauses
      *> differ: STANDARD-1 in P5844XM and the implementor-name
      *> BINARY-SEQUENTIAL in P5844XS (kb/Work PB5844, review R1-1). The
      *> phrase STANDARD-1 is declined, but it is still the clause the
      *> entry specifies, so the two specifications are not the same and
      *> the program is refused (COBOL6847, the external connector's
      *> entries disagreeing). Both files are variable length (RECORD
      *> CONTAINS 1 TO 4, Annex A.1 item 148), so the clause is legal in
      *> each entry (§1985:VII.2.8.3 SR1). A compiler that accepts the
      *> pair fails the test.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5844XM.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "P5844XS"
               RECORD DELIMITER IS STANDARD-1.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE IS EXTERNAL
           RECORD CONTAINS 1 TO 4 CHARACTERS.
       01  SEQ-REC PIC X(4).
       PROCEDURE DIVISION.
       P-MAIN.
           OPEN OUTPUT SEQ-FILE
           MOVE "ABCD" TO SEQ-REC
           WRITE SEQ-REC
           CLOSE SEQ-FILE
           CALL "P5844XS"
           STOP RUN.
       END PROGRAM P5844XM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P5844XS.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT SEQ-FILE ASSIGN TO "P5844XS"
               RECORD DELIMITER IS BINARY-SEQUENTIAL.
       DATA DIVISION.
       FILE SECTION.
       FD  SEQ-FILE IS EXTERNAL
           RECORD CONTAINS 1 TO 4 CHARACTERS.
       01  SEQ-REC PIC X(4).
       PROCEDURE DIVISION.
       P-SUB.
           EXIT PROGRAM.
       END PROGRAM P5844XS.
