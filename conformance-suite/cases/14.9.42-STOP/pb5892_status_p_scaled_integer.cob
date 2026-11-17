      *>--- conformance-case
      *> id: pb5892_status_p_scaled_integer
      *> rules: [SR-14.9.42.3-2]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5892's sibling - the STOP status operand is an
      *> integer data item by its VALUE, not by the spelling of its
      *> PICTURE.
      *>
      *> §2023:14.9.42.3 SR2 (cite.py --check): "Identifier-1 shall
      *> reference an integer data item or a data item with usage
      *> display or usage national." U PIC 99P COMP holds an integer
      *> (the P is an assumed zero to the right: every value of U is
      *> a multiple of 10), so it is admitted; before the fix it was
      *> refused COBOL1704 as neither alternative (it is not usage
      *> display, and its scale is not 0). U is 0, so the run
      *> passes 0 to the operating system (§2023:14.9.42.4 GR5) and
      *> ends normally after the DISPLAY.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB5892S.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01  U   PIC 99P COMP VALUE 0.
       PROCEDURE DIVISION.
       MAIN-PARA.
           DISPLAY "ADMITTED".
           STOP RUN WITH NORMAL STATUS U.
