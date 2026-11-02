      *> kb/Work PB2103: the definitions of pb2103_prototype_callee_compiled_
      *> elsewhere's three prototypes, compiled apart into P2103LIB. P2103S
      *> is described as its prototype is; P2103T's formal and P2103R's
      *> returning item are PIC X(4) where their prototypes say PIC 9(4).
       >>TURN EC-PROGRAM-ARG-MISMATCH CHECKING ON
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103S.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC 9(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "   P2103S GOT " L
           GOBACK.
       END PROGRAM P2103S.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103T.
       DATA DIVISION.
       LINKAGE SECTION.
       01 L PIC X(4).
       PROCEDURE DIVISION USING L.
           DISPLAY "   P2103T GOT " L
           GOBACK.
       END PROGRAM P2103T.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P2103R.
       DATA DIVISION.
       LINKAGE SECTION.
       01 R PIC X(4).
       PROCEDURE DIVISION RETURNING R.
           MOVE "WXYZ" TO R
           GOBACK.
       END PROGRAM P2103R.
