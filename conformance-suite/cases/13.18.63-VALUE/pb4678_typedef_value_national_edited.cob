      *>--- conformance-case
      *> id: pb4678_typedef_value_national_edited
      *> rules: [SR-13.18.63.3-5, SR-13.18.63.3-7]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *> portable: unverified
      *>---
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4678P2.
      *> ISO 13.18.63.3 SR5 sentence 2 and SR7 hold of a VALUE literal
      *> written in a TYPE DECLARATION (13.18.58.4 GR1): the entries
      *> subordinate to it and its condition-names are part of the
      *> declaration, and 13.18.58.4 GR3 makes every other clause, the
      *> VALUE among them, assumed by the data defined with the type-name.
      *> The negatives are pb4678-typedef-unused-value-oversize-national,
      *> pb4678-typedef-unused-value-class and
      *> pb4678-typedef-group-member-condition-class.
      *>
      *> The size rule is "shall not EXCEED", so a literal of exactly the
      *> picture's size conforms (ZZ9 is three national positions).
      *> NT  an elementary type declaration; U1 takes its VALUE (GR3).
      *> GT  a group declaration: USAGE NATIONAL reaches M and K by
      *>     13.18.60.4 GR1, so both are of class national.  K has a
      *>     table VALUE (Format 2) whose literal is national too.
      *> U2  takes the group, and so M's value and the condition-name.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 NT TYPEDEF PIC ZZ9 USAGE NATIONAL VALUE N"  7".
       01 GT TYPEDEF USAGE NATIONAL.
          05 M PIC ZZ9 VALUE N" 12".
             88 M-OK VALUE N" 12".
          05 K PIC ZZ9 OCCURS 2 VALUE N"  5" FROM (1).
       01 U1 TYPE NT.
       01 U2 TYPE GT.
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "U1=[" U1 "]"
           DISPLAY "M=[" M OF U2 "]"
           IF M-OK OF U2 DISPLAY "M-OK" END-IF
           DISPLAY "K1=[" K OF U2 (1) "]"
           STOP RUN.
