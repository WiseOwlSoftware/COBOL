      *>--- conformance-case
      *> id: pb4265_csharp_keyword_data_names
      *> rules: [GR-13.18.44.4-2, GR-13.18.27.4-2, GR-8.1.3.2-3]
      *> accept: [2002, 2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB4265. Data-names written in lowercase that spell
      *> words some host language reserves (int, double, void, ushort,
      *> sbyte, uint) are ordinary user-defined words: none is a COBOL
      *> reserved word, and case does not distinguish them.
      *> ISO §8.1.3.2 3) a): "COBOL basic letters appearing elsewhere
      *> within the compilation group are treated in a case-insensitive
      *> manner."
      *>   cite.py --check 8.1.3.2 -> OK §8.1.3.2 3) a) (General rules)
      *> ISO §13.18.44.4 GR2: "When the same storage area is defined by
      *> more than one data description entry, the data-name associated
      *> with any of those data description entries may be used to
      *> reference that storage area."
      *>   cite.py --check 13.18.44.4 -> OK §13.18.44.4 2)
      *> ISO §13.18.27.4 GR2: "A statement in a program contained
      *> directly or indirectly within a program that describes a
      *> global name may reference that name without describing it
      *> again."
      *>   cite.py --check 13.18.27.4 -> OK §13.18.27.4 2)
      *> Each REDEFINES changes the category, so both descriptions read
      *> one storage area: double (PIC 9(4)) reads int's "1234" as
      *> 1234 and ushort reads void's "56" as 56 (VIEWS-OK); ADD 1 to
      *> each numeric view leaves "1235" in int and "57" in void. uint
      *> is the GLOBAL numeric view of sbyte's "789": the contained
      *> program reads 789 (GLOBAL-OK) and adds 1, and the container
      *> then sees "790" in sbyte. Only alphanumeric items are
      *> DISPLAYed, whose device form the standard fixes.
      *> Why each leg can fail: the area a redefinition shares is a
      *> member the generated program declares under a name derived
      *> from the data-name; a derivation that is not a valid
      *> identifier for the implementation's own target refuses this
      *> legal program (before PB4265, '_redef_@int'), and a bridge to
      *> the container's GLOBAL storage that names it wrong loses the
      *> contained program's update.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4265K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 int PIC X(4) VALUE "1234".
       01 double REDEFINES int PIC 9(4).
       01 grp.
          05 void PIC X(2) VALUE "56".
          05 ushort REDEFINES void PIC 99.
       01 sbyte PIC X(3) VALUE "789" GLOBAL.
       01 uint REDEFINES sbyte PIC 999 GLOBAL.
       PROCEDURE DIVISION.
       MAIN-P.
           DISPLAY int " " void.
           IF double = 1234 AND ushort = 56
               DISPLAY "VIEWS-OK"
           END-IF.
           ADD 1 TO double.
           ADD 1 TO ushort.
           DISPLAY int " " void.
           CALL "PB4265KC".
           DISPLAY sbyte.
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB4265KC.
       PROCEDURE DIVISION.
       INNER-P.
           IF uint = 789
               DISPLAY "GLOBAL-OK"
           END-IF.
           ADD 1 TO uint.
           EXIT PROGRAM.
       END PROGRAM PB4265KC.
       END PROGRAM PB4265K.
