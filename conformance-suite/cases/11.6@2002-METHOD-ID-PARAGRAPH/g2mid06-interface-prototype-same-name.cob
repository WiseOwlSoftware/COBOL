      *>--- conformance-case
      *> id: g2mid06-interface-prototype-same-name
      *> rules: [2002-SR-11.6.2-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.6.2 SR4b - "no inherited method prototype shall have the
      *> same name as the method prototype declared by this method
      *> definition." G2MIDJ inherits G2MIDI's prototype M and declares a
      *> prototype M taking one parameter: the same NAME, whatever the
      *> parameters, is refused at 2002 (OVERRIDE cannot rescue it: SR2
      *> bars OVERRIDE in a prototype). Expected on G2MIDJ's METHOD-ID
      *> line.
       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2MIDI.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       PROCEDURE DIVISION.
       END METHOD M.
       END INTERFACE G2MIDI.

       IDENTIFICATION DIVISION.
       INTERFACE-ID. G2MIDJ INHERITS FROM G2MIDI.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           INTERFACE G2MIDI.
       PROCEDURE DIVISION.
       METHOD-ID. M.
       DATA DIVISION.
       LINKAGE SECTION.
       01  P PIC X.
       PROCEDURE DIVISION USING P.
       END METHOD M.
       END INTERFACE G2MIDJ.
