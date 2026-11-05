*>--- conformance-case
*> id: pointer-refused-relation-figurative
*> rules: [SR-8.8.4.2.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB4740 review R1-4 (the pointer sibling of message-tag-refused-relation-figurative) - a data pointer
      *> compared with a figurative constant is refused.
      *> ISO 8.8.4.2.3 SR5: "Identifier-3 and identifier-4 shall reference data items of class message-tag,
      *> object, or pointer, and shall be of the same category." (cite.py --check 8.8.4.2.3 OK, SR 5.) A figurative
      *> constant is no data item of those classes, and 8.4.3.10.3 SR1 admits NULL alone as the predefined operand
      *> (cite.py --check 8.4.3.10.3 OK, SR 1). The relation was accepted and the SPACE rendered as the NULL address.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PTRRELFIG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 P USAGE POINTER.
       PROCEDURE DIVISION.
       MAIN-PARA.
           IF P = SPACE DISPLAY "A" END-IF
           STOP RUN.
       END PROGRAM PTRRELFIG.
