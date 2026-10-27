*>--- conformance-case
*> id: pb3468_figurative_constant_receivers
*> rules: [GR-14.9.25.4-7, GR-14.9.25.4-6, GR-8.3.3.6.4-2, GR-8.3.3.6.4-4, SR-14.9.25.3-7]
*> accept: [2002]
*> source-format: free
*> portable: unverified
*>---
      *> kb/Work PB3468 - MOVE of a figurative constant to an alphanumeric, alphanumeric-edited, national,
      *> national-edited or boolean item. The expected lines are DERIVED from the rule text, never from a run.
      *>
      *> ISO 14.9.25.4 GR7 (Table 17): the figurative constant takes the category of the RECEIVER (an alphanumeric
      *>   receiver gets an alphanumeric ZERO, SPACE, QUOTE, HIGH-VALUE or LOW-VALUE; a national receiver gets a
      *>   national one; a boolean receiver gets a boolean ZERO); ALL literal keeps the category of the literal.
      *>   cite.py --check 14.9.25.4 "The category of figurative constants when used in the MOVE statement depends
      *>   on the category of the receiving operand" -> OK 14.9.25.4 7)
      *> ISO 8.3.3.6.4 GR2: the string is repeated character by character until it reaches the receiver's size,
      *>   then truncated; GR4: ZERO is the character '0', or the boolean character '0' for a boolean item.
      *>   cite.py --check 8.3.3.6.4 "the string of characters is repeated character by character until the size
      *>   of the resultant string is greater than or equal to the number of character positions" -> OK 8.3.3.6.4 2)
      *>   cite.py --check 8.3.3.6.4 "one or more of the boolean character '0'" -> OK 8.3.3.6.4 4)
      *> ISO 14.9.25.4 GR6: the move edits as the receiver's PICTURE says, and 13.18.40.5 rule 3 (simple insertion)
      *>   puts each insertion character in its own position, so the figurative constant's characters fill only the
      *>   DATA positions, left to right: the B of X(2)BX(2) and the slash of X(3)/X(3) are never overwritten.
      *>   cite.py --check 14.9.25.4 "along with any editing specified for, or de-editing implied by, the receiving
      *>   data item" -> OK 14.9.25.4 6)
      *>   cite.py --check 13.18.40.5 "Simple insertion editing results in the insertion character occupying the same
      *>   character position in the edited item as the associated symbol occupies in character-string-1"
      *>   -> OK 13.18.40.5 3)
      *> ISO 14.9.25.3 SR7: only a figurative constant whose characters are boolean characters moves to a boolean
      *>   item, so the boolean receivers B and BU take ZERO and ALL B"10" and nothing else here (the refused forms
      *>   are move-space-to-boolean).
      *>   cite.py --check 14.9.25.3 "Any figurative constant for which the associated character or characters are
      *>   not boolean characters shall not be moved to a boolean data item" -> OK 14.9.25.3 7)
      *>
      *> Why each line can fail: ALL "AB" into XE is the shape an implementation can get wrong by storing the repeated
      *>   string unedited (ABABA), which loses the inserted space; the HIGH-VALUE and LOW-VALUE lines fail when the
      *>   fill reaches an insertion position or stops short of a data position. The values of HIGH-VALUE and
      *>   LOW-VALUE are implementor-defined (8.3.3.6.4 GR6, GR7), so those lines print a verdict, not the character.
      *>
      *> EXPECTED OUTPUT (X is 5 positions, XE 5 with the insertion at 3, XS 7 with the insertion at 4, N 5, NE 5
      *>   with the insertion at 3, B and BU 5 boolean positions):
      *>   ZERO       X=00000  XE=00 00  XS=000/000  N=00000  NE=00 00  B=00000  BU=00000
      *>   SPACE      X=5 spaces  XE=5 spaces  XS=3 spaces, slash, 3 spaces  N=5 spaces  NE=5 spaces
      *>   QUOTE      X="""""  XE="" ""  XS="""/"""  N="""""  NE="" ""
      *>   ALL "AB"   X=ABABA  XE=AB AB  XS=ABA/BAB  N=ABABA  NE=AB AB
      *>   ALL B"10"  B=10101  BU=10101
IDENTIFICATION DIVISION.
PROGRAM-ID. PB3468A.
DATA DIVISION.
WORKING-STORAGE SECTION.
01 X  PIC X(5).
01 XE PIC X(2)BX(2).
01 XS PIC X(3)/X(3).
01 N  PIC N(5).
01 NE PIC N(2)BN(2).
01 B  PIC 1(5).
01 BU PIC 1(5) USAGE BIT.
PROCEDURE DIVISION.
MAIN-PARA.
    MOVE ZERO TO X XE XS N NE B BU.
    DISPLAY "ZERO X=[" X "] XE=[" XE "] XS=[" XS "] B=[" B "] BU=[" BU "]".
    DISPLAY "ZERO N=[" N "] NE=[" NE "]".
    MOVE SPACE TO X XE XS N NE.
    DISPLAY "SPACE X=[" X "] XE=[" XE "] XS=[" XS "]".
    DISPLAY "SPACE N=[" N "] NE=[" NE "]".
    MOVE QUOTE TO X XE XS N NE.
    DISPLAY "QUOTE X=[" X "] XE=[" XE "] XS=[" XS "]".
    DISPLAY "QUOTE N=[" N "] NE=[" NE "]".
    MOVE ALL "AB" TO X XE XS N NE.
    DISPLAY "ALL-AB X=[" X "] XE=[" XE "] XS=[" XS "]".
    DISPLAY "ALL-AB N=[" N "] NE=[" NE "]".
    MOVE ALL B"10" TO B BU.
    DISPLAY "ALL-B10 B=[" B "] BU=[" BU "]".
    MOVE HIGH-VALUE TO X XE XS N.
    IF X = HIGH-VALUE
        DISPLAY "HIGH-VALUE X=filled"
    ELSE
        DISPLAY "HIGH-VALUE X=short"
    END-IF.
    IF XE (1:2) = HIGH-VALUE AND XE (3:1) = SPACE AND XE (4:2) = HIGH-VALUE
        DISPLAY "HIGH-VALUE XE=filled around the insertion"
    ELSE
        DISPLAY "HIGH-VALUE XE=wrong"
    END-IF.
    IF XS (1:3) = HIGH-VALUE AND XS (4:1) = "/" AND XS (5:3) = HIGH-VALUE
        DISPLAY "HIGH-VALUE XS=filled around the insertion"
    ELSE
        DISPLAY "HIGH-VALUE XS=wrong"
    END-IF.
    IF N = HIGH-VALUE
        DISPLAY "HIGH-VALUE N=filled"
    ELSE
        DISPLAY "HIGH-VALUE N=short"
    END-IF.
    MOVE LOW-VALUE TO X XE XS N.
    IF X = LOW-VALUE
        DISPLAY "LOW-VALUE X=filled"
    ELSE
        DISPLAY "LOW-VALUE X=short"
    END-IF.
    IF XE (1:2) = LOW-VALUE AND XE (3:1) = SPACE AND XE (4:2) = LOW-VALUE
        DISPLAY "LOW-VALUE XE=filled around the insertion"
    ELSE
        DISPLAY "LOW-VALUE XE=wrong"
    END-IF.
    IF XS (1:3) = LOW-VALUE AND XS (4:1) = "/" AND XS (5:3) = LOW-VALUE
        DISPLAY "LOW-VALUE XS=filled around the insertion"
    ELSE
        DISPLAY "LOW-VALUE XS=wrong"
    END-IF.
    IF N = LOW-VALUE
        DISPLAY "LOW-VALUE N=filled"
    ELSE
        DISPLAY "LOW-VALUE N=short"
    END-IF.
    STOP RUN.
