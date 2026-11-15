*>--- conformance-case
*> id: pb5600-flag85-trailing-text
*> rules: [2002-FMT-7.2.11.1, 2002-SR-7.2.2-4]
*> source-format: free
*> portable: unverified
*>---
*> kb/Work PB5600. §2002:7.2.2 SR4: "When the reference format is free-form, a compiler directive may be followed only
*> by space characters and an optional inline comment", and §2002:7.2.11.1 prints the FLAG-85 format as an option list or
*> ALL, then ON or OFF. `>>FLAG-85 ALL ON JUNK` writes a word after the format is complete, so the directive is
*> malformed (COBOL1622); the compiler accepted it in silence before kb/Work PB5600.
IDENTIFICATION DIVISION.
PROGRAM-ID. PB5600N1.
PROCEDURE DIVISION.
    DISPLAY "A"
>>FLAG-85 ALL ON JUNK
    DISPLAY "B"
    STOP RUN.
