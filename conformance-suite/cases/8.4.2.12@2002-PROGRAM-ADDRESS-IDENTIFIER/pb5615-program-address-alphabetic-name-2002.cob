      *>--- conformance-case
      *> id: pb5615-program-address-alphabetic-name-2002
      *> rules: [2002-SR-8.4.2.12.2-1, 2002-GR-8.4.2.12.3-4]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5615 - §2002:8.5.2.1: "An alphabetic data item may be
      *>   specified wherever an elementary alphanumeric data item is
      *>   permitted as an operand." §2002:8.4.2.12.2 SR1: "Identifier-1
      *>   shall be of category alphanumeric or national." So NM, a
      *>   PIC A item, names the program in ADDRESS OF PROGRAM NM at
      *>   COBOL-2002. §2002:8.4.2.12.3 GR4: "If the runtime system
      *>   cannot locate the program, the EC-PROGRAM-NOT-FOUND
      *>   exception condition is set to exist and the value of the
      *>   address-identifier is the predefined address NULL." No program
      *>   NOSUCHPG exists, so PP is NULL: expected output NULL. It fails
      *>   if the PIC A operand is refused (COBOL2157) or PP is not NULL.
      *>   COBOL-2014 withdrew the sentence (§2014:E.2), so the same
      *>   source is refused at 2014 and 2023 (the diagnostics file
      *>   pb5615-program-address-alphabetic-name-refused).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5615AD.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  NM  PIC A(8) VALUE "NOSUCHPG".
       01  PP  USAGE PROGRAM-POINTER.
       PROCEDURE DIVISION.
       MAIN-P.
           SET PP TO ADDRESS OF PROGRAM NM
           IF PP = NULL
               DISPLAY "NULL"
           ELSE
               DISPLAY "NOT-NULL"
           END-IF
           STOP RUN.
