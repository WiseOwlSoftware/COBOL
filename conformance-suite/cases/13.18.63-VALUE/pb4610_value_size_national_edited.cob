      *>--- conformance-case
      *> id: pb4610_value_size_national_edited
      *> rules: [SR-13.18.63.3-5, SR-13.18.63.3-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4610P2.
      *> ISO 13.18.63.3 SR5 sentence 2 - "National literals in the
      *> VALUE clause of an elementary item shall not exceed the size
      *> indicated by an explicit PICTURE clause."  The national twin
      *> of SR4's size sentence (pb4610_value_size_numeric_edited): a
      *> numeric-edited item of usage NATIONAL is of class national
      *> (8.5.2.1 Table 2), so SR7 admits a national literal for it
      *> and this sentence measures that literal against the EDITED
      *> picture, the editing characters counted (2002 SR8).  The
      *> oversize literal is the negative
      *> pb4610-value-oversize-national-edited (COBOL0898).
      *>
      *> The rule is "shall not EXCEED", so a literal of exactly the
      *> item's size is conforming and is stored as written (no
      *> editing of the initial value).
      *> N1  PIC ZZ9 is three national positions; N"  7" is three.
      *> N2  PIC ZZ9.99 is six - Z, Z, 9, the point, 9, 9 - four of
      *>     them digit positions; N"  4.25" is six characters.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 N1 PIC ZZ9 USAGE NATIONAL VALUE N"  7".
       01 N2 PIC ZZ9.99 USAGE NATIONAL VALUE N"  4.25".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "N1=[" N1 "]"
           DISPLAY "N2=[" N2 "]"
           STOP RUN.
