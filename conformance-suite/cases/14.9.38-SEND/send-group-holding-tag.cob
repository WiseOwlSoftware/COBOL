      *>--- conformance-case
      *> id: send-group-holding-tag
      *> rules: [GR-14.9.38.4-4, GR-14.9.38.4-5, GR-14.9.38.4-6]
      *> accept: [2023]
      *> requires: [A.3-4]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> ISO 14.9.38.3 SR3 lists the clauses the data description entries of identifier-1 or of any item subordinate to it shall not
      *> contain (ANY LENGTH, BASED, DYNAMIC-LENGTH, OBJECT-REFERENCE, OCCURS DEPENDING ON outside identifier-1, POINTER,
      *> FUNCTION-POINTER, PROGRAM-POINTER); USAGE MESSAGE-TAG is not among them, so a strongly-typed group that holds message tags
      *> (directly, in a table and in a nested group) is a legal message area.  GR4: "The message contains the content of" it.
      *> GR5: no run unit serves the name, so EC-MCS-NO-SERVER is set to exist (checking is on) and the SEND is unsuccessful; GR3 a):
      *> ON EXCEPTION runs; GR6: data-name-1 is set only when the SEND is successful, so MT stays NULL.
      *>   cite.py: OK  14.9.38.3 SR3  OK  14.9.38.4 GR4  OK  14.9.38.4 GR5  OK  14.9.38.4 GR6
      *> The tags the group holds are null (nothing in a run unit that serves no one can make another), and the statement is
      *> accepted and executed: the group and every tag in it is in the message, which no server takes.
      *> EXPECTED VALUES, DERIVED: the SEND is unsuccessful; ON EXCEPTION runs and EXCEPTION-STATUS is EC-MCS-NO-SERVER; the
      *> NOT ON EXCEPTION phrase does not run; the tag MT stays NULL.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SNDGRPTG.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 MT        USAGE MESSAGE-TAG.
       01 TG IS TYPEDEF STRONG.
          05 TGA PIC X(3) VALUE "ABC".
          05 TGT USAGE MESSAGE-TAG.
          05 TGU USAGE MESSAGE-TAG OCCURS 2.
          05 TGN.
             10 TGM USAGE MESSAGE-TAG.
             10 TGZ PIC X(2) VALUE "ZZ".
       01 G1 TYPE TG.
       PROCEDURE DIVISION.
       MAIN-P.
           >>TURN EC-MCS-NO-SERVER CHECKING ON
           SEND TO "NOSUCHSERVER" FROM G1 RETURNING MT
               ON EXCEPTION DISPLAY "EXC " FUNCTION TRIM(
                   FUNCTION EXCEPTION-STATUS)
               NOT ON EXCEPTION DISPLAY "OK"
           END-SEND.
           IF MT = NULL DISPLAY "MT=NULL" END-IF.
           STOP RUN.
