*>--- conformance-case
*> id: pb3817-initialize-replacing-object-reference-class
*> rules: [SR-14.9.20.3-4, GR-14.9.20.4-4, SR-14.9.39.3-12]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3817 - the object-reference sibling of pb3817-initialize-replacing-restricted-program-pointer.
      *> 14.9.20.4 GR4 makes the REPLACING store of an object-reference "SET receiving-operand TO sending-operand",
      *> and 14.9.39.3 SR12 a) 2. holds that SET: "if the data item referenced by identifier-3 is described without
      *> an ONLY phrase, the object-class-name specified in the description of the data item referenced by
      *> identifier-4 shall reference the same class or a subclass" of the one identifier-3 names.
      *> cite.py: OK  14.9.39.3 12) a) 2.
      *> RCV is described with PB3817CA and SND with PB3817CB, two unrelated classes (each INHERITS FROM BASE only), so
      *> the implicit SET RCV TO SND is not valid. Before this fix the INITIALIZE passed the binder and died in the
      *> backend as a CS0030 type error on the emitted C#, naming no COBOL rule. The same pair in a written SET was
      *> already refused COBOL0867. Object references exist from COBOL-2002.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3817N4.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS PB3817CA
           CLASS PB3817CB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RCV USAGE OBJECT REFERENCE PB3817CA.
       01 SND USAGE OBJECT REFERENCE PB3817CB.
       PROCEDURE DIVISION.
       MAIN-P.
           INVOKE PB3817CB "NEW" RETURNING SND
           INITIALIZE RCV REPLACING OBJECT-REFERENCE DATA BY SND
           STOP RUN.
       END PROGRAM PB3817N4.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3817CA INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       M-MAIN.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS PB3817CA.

       IDENTIFICATION DIVISION.
       CLASS-ID. PB3817CB INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       M-MAIN.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS PB3817CB.
