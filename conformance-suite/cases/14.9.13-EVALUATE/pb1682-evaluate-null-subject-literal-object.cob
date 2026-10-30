      *>--- conformance-case
      *> id: pb1682-evaluate-null-subject-literal-object
      *> rules: [SR-14.9.13.3-10, SR-8.8.4.2.3-5]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB1682 - EVALUATE NULL WHEN "A": the subject is an identifier (8.4.3.1.2 Format 8), the object a
      *> literal, a combination Table 15 of 14.9.13.3 SR10 allows. The pairing is refused for the REAL reason: the
      *> pointer relation 8.8.4.2.3 SR5 admits only operands of class message-tag, object or pointer, so "A" cannot
      *> be compared with NULL (COBOL0869). It used to draw COBOL1634, "the selection subject is a literal", which
      *> is untrue and hid the rule that applies.
      *>   cite.py --check 8.8.4.2.3 "pointer" -> OK
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB1682NL.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN.
           EVALUATE NULL
              WHEN "A" DISPLAY "A"
              WHEN OTHER DISPLAY "O"
           END-EVALUATE
           STOP RUN.
