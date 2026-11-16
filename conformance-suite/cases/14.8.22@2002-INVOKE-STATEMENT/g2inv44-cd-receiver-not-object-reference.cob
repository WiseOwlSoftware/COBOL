      *>--- conformance-case
      *> id: g2inv44-cd-receiver-not-object-reference
      *> rules: [2002-SR-14.8.22.2-1]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:14.8.22.2 SR1 - "Identifier-1 shall be an object
      *> reference." CX is an alphanumeric record item under a 2002
      *> output CD, so the INVOKE CX line is refused at 2002. The
      *> accepted form, a CD object reference as identifier-1, is
      *> g2inv42-cd-operand-reach. Why it can fail: the compiler accepts
      *> a non-object CD item as the receiver or reports another code.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2INV44.
       DATA DIVISION.
       COMMUNICATION SECTION.
       CD CDOUT FOR OUTPUT.
       01 CR.
          05 CX PIC X(5) VALUE "HELLO".
       PROCEDURE DIVISION.
       MAIN.
           INVOKE CX "PING".
           STOP RUN.
       END PROGRAM G2INV44.
