      *>--- conformance-case
      *> id: pb4610_value_size_numeric_edited
      *> rules: [SR-13.18.63.3-4, SR-13.18.63.3-7]
      *> accept: [85, 2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4610P1.
      *> ISO 13.18.63.3 SR4 - "Alphanumeric literals in the VALUE
      *> clause of an elementary item shall not exceed the size
      *> indicated by an explicit PICTURE clause."  The sentence is
      *> the same in ISO/IEC 1989:2002, the 2014 FCD and 2023, so it
      *> is not a COBOL-2023 rule: kb/Work PB4610 found the compiler
      *> checking it for a NUMERIC-EDITED item only from 2023.  A
      *> numeric-edited item of usage DISPLAY is of class
      *> alphanumeric (8.5.2.1), so SR7 admits an alphanumeric
      *> literal for it and SR4's size sentence measures that literal.
      *>
      *> THE CONFORMING HALF.  The rule is "shall not EXCEED", so a
      *> literal of exactly the item's size is conforming, and the
      *> size is the EDITED picture's: the editing characters count
      *> (2002 SR8 / 2014 FCD SR8: "Editing characters in a picture
      *> character-string are included in determining the size of
      *> the data item, but do not cause editing of the initial value
      *> when the data item is initialized").  An off-by-one screen,
      *> or one that counted only the digit positions, would reject
      *> these.  The oversize literal is the negative
      *> pb4610-value-oversize-numeric-edited (COBOL1570, refused at
      *> every edition).
      *>
      *> E1  PIC ZZ9 is three character positions; "  7" is three.
      *>     No editing of the initial value, so DISPLAY shows the
      *>     literal's characters as written.
      *> E2  PIC $ZZ9.99 is seven character positions - $, Z, Z, 9,
      *>     the point, 9, 9 - four of them digit positions; "$  4.25"
      *>     is seven characters.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 E1 PIC ZZ9 VALUE "  7".
       01 E2 PIC $ZZ9.99 VALUE "$  4.25".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "E1=[" E1 "]"
           DISPLAY "E2=[" E2 "]"
           STOP RUN.
