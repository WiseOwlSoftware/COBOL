      *> reject-at: 85 2002 2014 2023
      *> ISO 12.3.3 SR1: "The configuration section shall not be specified in a
      *> program that is contained within another program" - the containing
      *> program's configuration section applies to it (12.3.4 GR1), which is
      *> what DataBinder.InheritConfiguration realizes (PB60 / AR-15.67.3-5).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CFGNEGOUTER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       PROCEDURE DIVISION.
           CALL "CFGNEGINNER".
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. CFGNEGINNER.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       PROCEDURE DIVISION.
           EXIT PROGRAM.
       END PROGRAM CFGNEGINNER.
       END PROGRAM CFGNEGOUTER.
