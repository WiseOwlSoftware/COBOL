      *> reject-at: 2002 2014 2023
      *> ISO 10.6.2 SR1: "Within a compilation group, function-prototypes
      *> and program-prototypes shall precede all other types of source
      *> units." The prototype here follows a program definition.
      *> kb/Work PB894.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNOM.
       PROCEDURE DIVISION.
           STOP RUN.
       END PROGRAM PGNOM.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PGNOP IS PROTOTYPE.
       PROCEDURE DIVISION.
       END PROGRAM PGNOP.
