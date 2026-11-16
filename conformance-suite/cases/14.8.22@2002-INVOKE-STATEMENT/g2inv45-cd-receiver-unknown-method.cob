      *>--- conformance-case
      *> id: g2inv45-cd-receiver-unknown-method
      *> rules: [2002-SR-14.8.22.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR4 b) - "If identifier-1 references an object
      *> reference described with a class-name without the FACTORY
      *> phrase, literal-1 shall be the name of a method contained in
      *> the instance interface of that class-name." CO is a level-1
      *> object reference record under a 2002 output CD described with
      *> G2INV45C, whose instance interface has PING but no NOPE, so
      *> the INVOKE CO "NOPE" line is refused at 2002. The accepted
      *> form is g2inv42-cd-operand-reach. Why it can fail: the
      *> compiler accepts the unknown name for a CD receiver (leaving
      *> it to the runtime) or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV45.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS G2INV45C.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CO USAGE OBJECT REFERENCE G2INV45C.
       PROCEDURE DIVISION.
       MAIN.
           INVOKE G2INV45C "NEW" RETURNING CO.
           INVOKE CO "NOPE".
           STOP RUN.
       END PROGRAM G2INV45.

       IDENTIFICATION DIVISION.
       CLASS-ID. G2INV45C INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       PROCEDURE DIVISION.
       METHOD-ID. PING.
       PROCEDURE DIVISION.
           DISPLAY "PINGED".
       END METHOD PING.
       END OBJECT.
       END CLASS G2INV45C.
