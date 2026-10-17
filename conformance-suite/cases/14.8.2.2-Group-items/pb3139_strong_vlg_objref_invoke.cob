      *>--- conformance-case
      *> id: pb3139_strong_vlg_objref_invoke
      *> rules: [SR-14.8.2.2-2, GR-14.9.25.4-9]
      *> accept: [2014, 2023]
      *> source-format: fixed
      *>---
      *> kb/Work PB3139 - a STRONGLY-TYPED VARIABLE-LENGTH group whose
      *> members include OBJECT REFERENCE leaves (one in a
      *> dynamic-capacity table's element) crosses an INVOKE BY
      *> REFERENCE, BY CONTENT and as a RETURNING item.
      *> cite.py --check 14.8.2.2 "If either the formal parameter or the
      *>   corresponding argument is a strongly-typed group item, both
      *>   shall be of the same type" -> OK 14.8.2.2 2)
      *> cite.py --check 13.18.60.3 "subordinate to a type declaration
      *>   that includes the STRONG phrase" -> OK 13.18.60.3 14)
      *> cite.py --check 14.2.3 "allocated by the activating runtime
      *>   element" -> OK 14.2.3 9)
      *> cite.py --check 14.6.9.2 "The operation recreates or overwrites
      *>   the receiving table with a copy of the sending table" -> OK
      *> G, H and LG/RG are all TYPE T, and the program's T and the
      *> class's T are equivalent type declarations (8.5.3.1), so every
      *> pairing conforms; before the fix the shape had no crossing
      *> form.
      *> DERIVATION (G: TX "ABC", TD "abc", TN (2) 42, TA -> O, two
      *> occurrences of TT: "aa", and "bb" whose TT-O -> O):
      *>   (1) BY REFERENCE: TAKEG sees LX=ABC N2=042 D=[abc] C=2 X2=bb
      *>       and a set TA and TT-O (2); its MOVEs (TX XYZ, TN (1) 7,
      *>       TD "longer") and its reference to TT-X (3) "cc" (a third
      *>       occurrence, 8.5.1.9.3) are G's:
      *>       REF:XYZ N1=007 D=[longer] C=3 X3=cc.
      *>   (2) BY CONTENT: TAKEG sees LX=DEF N2=042 D=[longer] C=3
      *>       X2=bb; its stores reach only its copy (14.2.3 GR9), so G
      *>       keeps TX DEF and TD "longer": CON:DEF D=[longer] C=3.
      *>   (3) RETURNING (14.8.3.2, same type): MAKEG returns TX RET, TD
      *>       "rd", one occurrence "rr" whose TT-O is SELF, the very
      *>       object O references: RET:RET D=[rd] C=1 X1=rr, SAMEOBJ.
      *> OO, strong types: COBOL 2002; dynamic-length items and
      *> dynamic-capacity tables: COBOL 2014.
       IDENTIFICATION DIVISION.
       CLASS-ID. P3139K INHERITS FROM BASE.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS BASE.
       IDENTIFICATION DIVISION.
       OBJECT.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
          05 TD PIC X DYNAMIC LENGTH LIMIT IS 8.
          05 TN PIC 9(3) OCCURS 2.
          05 TT OCCURS DYNAMIC CAPACITY IN TC FROM 1 TO 4.
             10 TT-O USAGE OBJECT REFERENCE.
             10 TT-X PIC X(2).
       PROCEDURE DIVISION.
       METHOD-ID. TAKEG.
       DATA DIVISION.
       LOCAL-STORAGE SECTION.
       01 K PIC Z.
       01 D PIC X(3).
       LINKAGE SECTION.
       01 LG TYPE T.
       PROCEDURE DIVISION USING LG.
           MOVE TC OF LG TO K
           MOVE TN OF LG (2) TO D
           DISPLAY "LX=" TX OF LG " N2=" D
               " D=[" TD OF LG "] C=" K " X2=" TT-X OF LG (2)
           IF TA OF LG = NULL OR TT-O OF LG (2) = NULL
               DISPLAY "TA:NULL"
           ELSE
               DISPLAY "TA:SET"
           END-IF
           MOVE "XYZ" TO TX OF LG
           MOVE 7 TO TN OF LG (1)
           MOVE "longer" TO TD OF LG
           MOVE "cc" TO TT-X OF LG (3)
           GOBACK.
       END METHOD TAKEG.
       METHOD-ID. MAKEG.
       DATA DIVISION.
       LINKAGE SECTION.
       01 RG TYPE T.
       PROCEDURE DIVISION RETURNING RG.
           MOVE "RET" TO TX OF RG
           MOVE "rd" TO TD OF RG
           MOVE "rr" TO TT-X OF RG (1)
           SET TT-O OF RG (1) TO SELF
           GOBACK.
       END METHOD MAKEG.
       END OBJECT.
       END CLASS P3139K.
       IDENTIFICATION DIVISION.
       PROGRAM-ID. P3139M.
       ENVIRONMENT DIVISION.
       CONFIGURATION SECTION.
       REPOSITORY.
           CLASS P3139K.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 T TYPEDEF STRONG.
          05 TA USAGE OBJECT REFERENCE.
          05 TX PIC X(3).
          05 TD PIC X DYNAMIC LENGTH LIMIT IS 8.
          05 TN PIC 9(3) OCCURS 2.
          05 TT OCCURS DYNAMIC CAPACITY IN TC FROM 1 TO 4.
             10 TT-O USAGE OBJECT REFERENCE.
             10 TT-X PIC X(2).
       01 G TYPE T.
       01 H TYPE T.
       01 O USAGE OBJECT REFERENCE P3139K.
       01 K PIC Z.
       01 D PIC X(3).
       PROCEDURE DIVISION.
           INVOKE P3139K "NEW" RETURNING O
           MOVE "ABC" TO TX OF G
           MOVE "abc" TO TD OF G
           MOVE 42 TO TN OF G (2)
           SET TA OF G TO O
           MOVE "aa" TO TT-X OF G (1)
           MOVE "bb" TO TT-X OF G (2)
           SET TT-O OF G (2) TO O
           INVOKE O "TAKEG" USING G
           MOVE TC OF G TO K
           MOVE TN OF G (1) TO D
           DISPLAY "REF:" TX OF G " N1=" D
               " D=[" TD OF G "] C=" K " X3=" TT-X OF G (3)
           MOVE "DEF" TO TX OF G
           INVOKE O "TAKEG" USING BY CONTENT G
           MOVE TC OF G TO K
           DISPLAY "CON:" TX OF G " D=[" TD OF G "] C=" K
           INVOKE O "MAKEG" RETURNING H
           MOVE TC OF H TO K
           DISPLAY "RET:" TX OF H " D=[" TD OF H "] C=" K
               " X1=" TT-X OF H (1)
           IF TT-O OF H (1) = O
               DISPLAY "SAMEOBJ"
           ELSE
               DISPLAY "OTHER"
           END-IF
           STOP RUN.
       END PROGRAM P3139M.
