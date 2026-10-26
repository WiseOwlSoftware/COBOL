*>--- conformance-case
*> id: pb3755-call-content-high-value-numeric-edited-2023
*> rules: [GR-14.8.2.3.3-L2.2, SR-14.9.25.3-5]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3755 - 14.8.2.3.3 rule 2 d): a BY CONTENT argument into a formal that is neither numeric, index nor ANY
      *> LENGTH "conform[s] as for a MOVE statement with the argument as the sending operand and the corresponding formal
      *> parameter as the receiving operand". MOVE HIGH-VALUE TO <a numeric-edited item> is removed at 2023 (14.9.25.3
      *> SR5; Annex E.2 item 1 bullet 1: it was valid through 2014), so HIGH-VALUE BY CONTENT into the PIC ZZ9 formal of a
      *> contained program activated with the NESTED phrase is COBOLNET0902 at 2023. Before this the SR5 edition rows were
      *> asked only by MOVE and INITIALIZE, and this compiled clean. The positive twin is
      *> pb3755_content_figurative_numeric_edited_formal (2002, 2014).
      *>   cite.py --check 14.8.2.3.3 "the conformance rules are the same as for a MOVE statement" -> OK
      *>     14.8.2.3.3 2) d)
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3755CL.
       PROCEDURE DIVISION.
           CALL "N3755CA" AS NESTED USING BY CONTENT HIGH-VALUE
           STOP RUN.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. N3755CA.
       DATA DIVISION.
       LINKAGE SECTION.
       01 E PIC ZZ9.
       PROCEDURE DIVISION USING E.
           DISPLAY "ENTERED"
           GOBACK.
       END PROGRAM N3755CA.
       END PROGRAM N3755CL.
