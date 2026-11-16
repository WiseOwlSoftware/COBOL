      *>--- conformance-case
      *> id: g2002_entry_convention_default_names
      *> rules: [2002-GR-11.8.5.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:11.8.5.3 GR2 - with no ENTRY-CONVENTION clause the entry
      *>   convention is implementor-defined "except that the naming
      *>   convention and mapping of method-names and program-names are
      *>   as specified in 8.3.1.1.1, User-defined words".
      *> §2002:14.8.4.3 GR3 - the value of literal-1 "is the program-name
      *>   of the program being called, as described in 8.3.1.1.1".
      *> §2002:7.2.8.2 GR1 - the default is >>CALL-CONVENTION COBOL, so by
      *>   §2002:8.3.1.1.1 2a "the program-name is treated as a COBOL word
      *>   that maps to the externalized name of the program", by the same
      *>   mapping as the PROGRAM-ID word G2ECVSUB.
      *> §2002:8.1.2.1 GR3a - as COBOL words, upper and lower case basic
      *>   letters are equivalent, so g2ecvsub, G2EcvSub and G2ECVSUB are
      *>   one word and map to one externalized name.
      *> §2002:8.3.1.1.1 - source elements naming the same externalized
      *>   name "refer to the same instance": the counter in G2ECVSUB's
      *>   working storage (not INITIAL) climbs 1, 2, 3 across the calls.
      *> Fails if a lower- or mixed-case CALL literal misses the program
      *>   (EC-PROGRAM-NOT-FOUND, ON EXCEPTION leg prints MISSED) or
      *>   reaches a second instance (the count restarts).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ECV01.
       PROCEDURE DIVISION.
           CALL "g2ecvsub"
               ON EXCEPTION DISPLAY "MISSED g2ecvsub"
           END-CALL.
           CALL "G2EcvSub"
               ON EXCEPTION DISPLAY "MISSED G2EcvSub"
           END-CALL.
           CALL "G2ECVSUB"
               ON EXCEPTION DISPLAY "MISSED G2ECVSUB"
           END-CALL.
           STOP RUN.
       END PROGRAM G2ECV01.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2ECVSUB.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
           ADD 1 TO N.
           DISPLAY "G2ECVSUB CALL " N.
           GOBACK.
       END PROGRAM G2ECVSUB.
