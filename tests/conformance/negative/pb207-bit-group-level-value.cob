      *> reject-at: 2023
      *> STAGED LOUD (COBOLNET0899), kb/Work PB207: the 13.18.63.4 GR5 area deposit for a BIT-PACKED group.
      *> GR5 initializes "the group area"; for a group with a USAGE BIT item subordinate to it that area is
      *> ceil(bits/8) PACKED characters laid out by the 8.5.1.6.3 bit walk, and the members do NOT tile it -
      *> BG below is 4 bits = 1 character while B1 and B2 occupy ceil(2/8) = 1 character each.  So the
      *> positional CHARACTER slice both initializer lanes implement has no meaning over this group.
      *> ⛔ WHAT THIS FIXTURE PINS IS THAT THE COMPILER NO LONGER CRASHES.  MEASURED on 8ca74a3d and again on
      *> the PB184 tree: this exact program raised an unhandled System.ArgumentOutOfRangeException out of
      *> GroupValueSlicer.SliceInit (Substring past the end of a 1-character area) - a compiler crash, not the
      *> silent mis-seed PB207 was filed as.  MEASURED on the single-member shape
      *> `01 BG GROUP-USAGE BIT VALUE B"1010". 05 B1 PIC 1(4).`: it did not crash and stored ONE boolean
      *> position where the literal has four, so that half was a silent wrong answer.  Neither is acceptable;
      *> a named refusal is, until PB207 lands the boolean-position area rule for both lanes.
      *> The predicate is DataItem.HasBitDescendant - the fact that switches ImageWidth to the bit walk - not
      *> GROUP-USAGE BIT, which is only the commonest way to acquire it (sweep note: the codec lane's guard
      *> keyed the narrower one).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB207N1.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 BG GROUP-USAGE BIT VALUE B"1010".
          05 B1 PIC 1(2).
          05 B2 PIC 1(2).
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "UNREACHABLE"
           STOP RUN.
