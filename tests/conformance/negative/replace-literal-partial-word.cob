*> reject-at: 2002 2014 2023
      *> kb/Work R39 - REPLACE's operands were never literals in ANY ISO edition: the 7.2.4.2
      *> general format admits ==pseudo-text== / ==partial-word== operands only, and 7.2.4.3 SR7
      *> bars alphanumeric/boolean/national literals as partial-words explicitly. The GCOS/ACU
      *> spelling below (GnuCOBOL gates it behind -fpartial-replace-when-literal-src) used to be
      *> silently HALF-PARSED - no diagnostic on the statement, nothing applied, and the failure
      *> surfaced downstream as an unrelated undefined-reference on the never-replaced name.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. R39NEG.
       REPLACE LEADING "PREFIX-" BY SPACES.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 PREFIX-VAR1 PIC X(2) VALUE "OK".
       PROCEDURE DIVISION.
           DISPLAY VAR1.
           STOP RUN.
