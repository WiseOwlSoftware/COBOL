      *>--- conformance-case
      *> id: pb5922-move-pointer-2002
      *> rules: [SR-14.9.25.3-1, SR-13.18.60.3-9]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5922, review round 1 (R1-1). A MOVE operand shall
      *> not be of class pointer (§2002:14.8.24.2 SR1), and a
      *> data-pointer data item may be referenced explicitly only in
      *> the contexts §2002:13.16.58.2 SR8 lists. At --std 2002 the
      *> message cites both 2002 rules.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5922MP.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       01 Q USAGE POINTER.
       PROCEDURE DIVISION.
           MOVE P TO Q.
           STOP RUN.
       END PROGRAM PB5922MP.
