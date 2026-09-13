*> reject-at: 85 2002 2014 2023
*> ISO 1989:2023 13.18.60.3 syntax rule 1 - "The USAGE clause shall not be specified in a data description
*> entry that has a level-number of 66 or 88."
*>
*> The LEVEL-66 half. The grammar makes `renamesClause` a whole alternative of `dataDescriptionBody`, so a
*> 66 entry cannot carry a clause list at all and the USAGE clause has nowhere to attach. The rejection is
*> therefore a parse-level one; this golden pins that the shape is refused at every edition, which is what
*> SR1 requires of the compiler.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB488-USAGE-ON-66.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 REC.
          05 A PIC X(2) VALUE "AB".
          05 B PIC X(2) VALUE "CD".
       66 R1 RENAMES A THRU B USAGE DISPLAY.
       PROCEDURE DIVISION.
           DISPLAY R1
           STOP RUN.
