      *>--- conformance-case
      *> id: pb2103_prototype_callee_compiled_elsewhere
      *> rules: [GR-14.9.4.4-3]
      *> accept: [2002, 2014, 2023]
      *> files: [pb2103_callees.cbl]
      *> build: ["pb2103_callees.cbl > P2103LIB", "{self}"]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB2103 (DESIGN-external-repository 4.2): a CALL through a
      *> program prototype whose definition another compilation group
      *> compiles is checked against the program ACTUALLY activated.
      *> 12.3.8.4 GR10 b): "the details are taken from that program
      *> prototype definition", so the bind checks each CALL below
      *> against the prototypes P2103S and P2103T, and both conform.
      *> The definitions were compiled apart (pb2103_callees.cbl):
      *> P2103S as the prototype says, P2103T with PIC X(4) where its
      *> prototype says PIC 9(4). 14.9.4.4 GR3 d): "If the resources
      *> are available and the program being called is a COBOL
      *> program, the rules for conformance specified in 14.8.2,
      *> Parameters and 14.8.3, Returning items apply. If a violation
      *> of these rules is detected, the EC-PROGRAM-ARG-MISMATCH
      *> exception condition is set to exist if checking for it is
      *> enabled in both the activated program and activating runtime
      *> element" (both units turn it on). The element has a
      *> program-specifier for each, so 14.8.2.3.2 rule 2 applies BY
      *> REFERENCE: "the definition of the formal parameter and the
      *> definition of the argument shall have the same ALIGN, BLANK
      *> WHEN ZERO, DYNAMIC LENGTH, JUSTIFIED, PICTURE, SIGN, and USAGE
      *> clauses". Case 01 conforms and runs; case 02 is not
      *> successful, so P2103T never runs and ON EXCEPTION executes;
      *> case 03 is the RETURNING twin (14.8.3.3: P2103T returns
      *> PIC X(4) where its prototype returns PIC 9(4)).
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103S IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
       END PROGRAM P2103S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103T IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
       END PROGRAM P2103T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103R IS PROTOTYPE.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC 9(4).
       PROCEDURE DIVISION RETURNING R.
       END PROGRAM P2103R.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           PROGRAM P2103S
           PROGRAM P2103T
           PROGRAM P2103R.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N4 PIC 9(4) VALUE 1234.
       01 R4 PIC 9(4) VALUE 0.
       PROCEDURE DIVISION.
           CALL P2103S USING BY REFERENCE N4
               ON EXCEPTION DISPLAY "01 94->94 REF EXC"
               NOT ON EXCEPTION DISPLAY "01 94->94 REF OK"
           END-CALL
           CALL P2103T USING BY REFERENCE N4
               ON EXCEPTION DISPLAY "02 94->X4 REF EXC"
               NOT ON EXCEPTION DISPLAY "02 94->X4 REF OK"
           END-CALL
           CALL P2103R RETURNING R4
               ON EXCEPTION DISPLAY "03 RET X4->94 EXC " R4
               NOT ON EXCEPTION DISPLAY "03 RET X4->94 OK " R4
           END-CALL
           STOP RUN.
       END PROGRAM P2103M.
