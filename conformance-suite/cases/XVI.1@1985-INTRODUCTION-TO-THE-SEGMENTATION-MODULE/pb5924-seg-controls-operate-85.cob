      *>--- conformance-case
      *> id: pb5924-seg-controls-operate-85
      *> rules: [1985-GR-XVI.1.6-p1]
      *> accept: [85]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB5924. §1985:XVI.1.6 (cite.py: OK): "The implementor must also provide all controls necessary for a segment to operate whenever the segment is used"
      *> Independent segments operate every time they are used: an iterated PERFORM THRU, a PERFORM
      *> and a GO TO of a paragraph that is not the first of its section.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. SEGOPR.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N PIC 9 VALUE 0.
       PROCEDURE DIVISION.
       MAIN SECTION.
       M1.
           PERFORM I1 THRU I2 3 TIMES.
           PERFORM J2.
           GO TO J3.
       M2.
           DISPLAY "END " N.
           STOP RUN.
       IND SECTION 55.
       I1.
           ADD 1 TO N.
           DISPLAY "I1 " N.
       I2.
           DISPLAY "I2 " N.
       IND2 SECTION 77.
       J1.
           DISPLAY "J1".
       J2.
           DISPLAY "J2".
       J3.
           DISPLAY "J3".
           GO TO M2.
