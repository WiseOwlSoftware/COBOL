      *>--- conformance-case
      *> id: pb3390-source-computer-comma-decimal-clause
      *> rules: [FMT-12.3.5.2]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3390 - the CLOSED ISO 12.3.5.2 general format prints SOURCE-COMPUTER.
      *> [ computer-name-1 ] . and nothing else. Under DECIMAL-POINT IS COMMA, ,5 is ONE numeric
      *> literal (a comma that no space follows is no separator, ISO 8.3.5 2)), and the error
      *> production did not open at it, so XYZ was refused as an unrecognized configuration
      *> paragraph. It opens at it now: XYZ is the computer-name and ,5 is refused BY NAME in the
      *> SOURCE-COMPUTER paragraph's format (COBOL1970), quoted whole, at every edition.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3390SC.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       SOURCE-COMPUTER. XYZ ,5.
       SPECIAL-NAMES.
           DECIMAL-POINT IS COMMA.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "N3390SC".
           STOP RUN.
