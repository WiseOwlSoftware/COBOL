*> reject-at: 2002 2014 2023
*> ISO 13.18.63.2 FORMAT 5 (content-validation-entry) - Annex A.4.14 item 7, the "IS VALID" tail that turns
*> an ordinary level-88 VALUE list into a content-validation entry. Only the TAIL is declined: the ordinary
*> condition-name VALUE (format 3) is fully supported, which the positive control
*> tests/conformance/85/declined_validate_words_are_user_words.cob and the whole existing 88 corpus pin.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. DCLVAL5.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 WS-A PIC 9(4).
          88 GOOD-VALS VALUES 1 THRU 5 IS VALID.
       PROCEDURE DIVISION.
           DISPLAY "UNREACHABLE".
           STOP RUN.
