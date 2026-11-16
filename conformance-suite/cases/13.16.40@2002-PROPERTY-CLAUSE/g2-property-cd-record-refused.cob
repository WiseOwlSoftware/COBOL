      *>--- conformance-case
      *> id: g2-property-cd-record-refused
      *> rules: [2002-SR-13.16.40.2-1]
      *> source-format: fixed
      *>---
      *> §2002:13.16.40.2 SR1 - "The PROPERTY clause may be specified
      *> only in the working-storage section of a factory definition or
      *> an instance definition."  CD-X1 is in a record description
      *> entry following a CD entry in a program's communication
      *> section (§2002:A.1.1.2: "the elements allowed in a record
      *> description entry are explained in 8.5.1.2, Levels, and in
      *> 13.14, Data description entry"), which is neither, so the
      *> program is refused at the PROPERTY entry.  The accepted
      *> sibling, factory and instance working-storage, is
      *> g2-property-implicit-accessor-arms.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2PRP02.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD  OUT-CD FOR OUTPUT
           DESTINATION COUNT IS OUT-COUNT
           TEXT LENGTH IS OUT-LEN
           STATUS KEY IS OUT-ST
           ERROR KEY IS OUT-EK
           SYMBOLIC DESTINATION IS OUT-DEST.
       01  CD-REC.
           05 CD-X1 PIC 9 PROPERTY.
       PROCEDURE DIVISION.
       P-PARA.
           DISPLAY "SHOULD NOT COMPILE".
           STOP RUN.
