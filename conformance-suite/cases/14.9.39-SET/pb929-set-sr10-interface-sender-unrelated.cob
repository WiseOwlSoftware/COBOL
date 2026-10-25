*>--- conformance-case
*> id: pb929-set-sr10-interface-sender-unrelated
*> rules: [SR-14.9.39.3-10]
*> source-format: free
*> portable: unverified
*>---
      *> ISO §14.9.39.3 syntax rule 10 a): when the receiver (identifier-3) is described with an interface-name that
      *> identifies the interface int-1, the data item referenced by identifier-4 shall be "an object reference described
      *> with an interface-name that identifies int-1 or an interface inheriting from int-1".  RI is described with the
      *> interface P9A1I; RZ is described with the interface P9A1Z, which neither is P9A1I nor INHERITS FROM it, so
      *> SET RI TO RZ violates SR10 a) and shall be refused.
      *> DISCRIMINATOR: the conforming twin is conformance:cases/14.9.39-SET/pb929_set_sr10_interface_receiver_senders,
      *> whose sender interface INHERITS FROM the receiver's; the two differ only by that INHERITS FROM clause.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P9A1.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE P9A1I
           INTERFACE P9A1Z.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 RI USAGE OBJECT REFERENCE P9A1I.
       01 RZ USAGE OBJECT REFERENCE P9A1Z.
       PROCEDURE DIVISION.
       MAIN.
           SET RI TO RZ.
           STOP RUN.
       END PROGRAM P9A1.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. P9A1I.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
       END METHOD PING.
       END INTERFACE P9A1I.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. P9A1Z.
       PROCEDURE DIVISION.
       METHOD-ID. ZING.
       PROCEDURE DIVISION.
       END METHOD ZING.
       END INTERFACE P9A1Z.
