      *>--- conformance-case
      *> id: g85_x02
      *> rules: [1985-SR-IV.4.3.8.3.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §1985:IV.4.3.8.3.3 SR4: "A function-identifier which references
      *> an integer or numeric function may be used only in an
      *> arithmetic expression". ORD is an integer function (the
      *> permitted uses of 1989-GR-A.2.3-3 are pinned by g85_x43, not
      *> here), and MOVE's sending
      *> operand (line 9 of the program below) is identifier-1, not an
      *> arithmetic expression. WiseOwl refuses line 9 at 85 with
      *> COBOL0900; a compiler that accepts it fails. Line 8, the same
      *> function on COMPUTE's right side, is legal.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G85X02.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  N PIC 999 VALUE 0.
       PROCEDURE DIVISION.
       P1.
           COMPUTE N = FUNCTION ORD ("A").
           MOVE FUNCTION ORD ("A") TO N.
           STOP RUN.
