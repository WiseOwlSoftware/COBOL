*>--- conformance-case
*> id: pb5481_literal_subject_literal_object_2002
*> rules: [SR-14.9.13.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB5481. §2002:14.8.12.2 SR9 and §2023:14.9.13.3 SR10: "The
      *> permissible combinations of selection subject and selection object
      *> operands are indicated in Table 15", and the [NOT] literal row is blank
      *> under the literal subject column, so a literal subject against a literal
      *> object is refused (COBOL1634) from 2002. At --std 85 the pairing is
      *> legal (cases/VI.6.13@1985-EVALUATE-STATEMENT/
      *> pb5481_literal_subject_literal_object_85).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5481B.
       PROCEDURE DIVISION.
       MAIN-PARA.
           EVALUATE 5
               WHEN 5 DISPLAY "SELECTED"
               WHEN OTHER DISPLAY "OTHER"
           END-EVALUATE.
           STOP RUN.
