      *>--- conformance-case
      *> id: pb3766-contained-clauses-name-global-data-item
      *> rules: [GR-13.18.27.4-1, GR-13.18.27.4-2, SR-12.4.5.8.3-2]
      *> accept: [2002]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> kb/Work PB3766, review finding R1-1 (the data-item arm of the report case). The data-names of a
      *> container's GLOBAL items are visible to the programs it contains, in the clauses of their own DATA
      *> DIVISION as well as in their PROCEDURE DIVISION.
      *>
      *> THE RULE. ISO/IEC 1989:2023 section 13.18.27.4 GR1: "A constant-name, data-name, file-name, report-name,
      *> or screen-name described using a GLOBAL clause is a global name. All data-names subordinate to a global
      *> name are global names." GR2: "A statement in a program contained directly or indirectly within a program
      *> that describes a global name may reference that name without describing it again." The FILE STATUS
      *> clause of the contained program's own file DAT names GS, a two-character alphanumeric item defined in
      *> the container's working-storage section (12.4.5.8.3 SR2), and the OCCURS ... DEPENDING ON clause of its
      *> own table T names GN, a GLOBAL item of the container. The compiler resolved both before it inherited the
      *> container's GLOBAL items and refused the program: COBOL1639 "the clause's data-name references no data
      *> item" and COBOL0851 "data-name-1 is not defined".
      *>
      *> DERIVATION OF THE EXPECTED OUTPUT. GN holds 2, so 13.18.38.4 GR7 ("The value of the data item referenced
      *> by data-name-1 represents the current number of occurrences of the subject of the entry") gives T two
      *> occurrences of E, and DISPLAY of the group T shows its two current characters: AA. The OPEN INPUT of DAT
      *> is attempted on a file that is not OPTIONAL and whose physical file is not present, so the I-O status
      *> is 35 (9.1.13.6 item 5), and GS, the FILE STATUS item, receives it (12.4.5.8.4 GR1): the container
      *> displays GS=35 after the contained program displayed T=AA.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766G.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GS PIC XX GLOBAL.
       01 GN PIC 9 GLOBAL VALUE 2.
       PROCEDURE DIVISION.
           CALL "PB3766GI"
           DISPLAY "GS=" GS
           STOP RUN.

       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB3766GI.
       ENVIRONMENT DIVISION.
       INPUT-OUTPUT SECTION.
       FILE-CONTROL.
           SELECT DAT ASSIGN TO "pb3766-absent.dat"
               FILE STATUS IS GS.
       DATA DIVISION.
       FILE SECTION.
       FD DAT.
       01 DAT-REC PIC X.
       WORKING-STORAGE SECTION.
       01 T.
          05 E PIC X OCCURS 1 TO 5 DEPENDING ON GN.
       PROCEDURE DIVISION.
           OPEN INPUT DAT
           MOVE ALL "A" TO T
           DISPLAY "T=" T
           EXIT PROGRAM.
       END PROGRAM PB3766GI.
       END PROGRAM PB3766G.
