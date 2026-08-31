       IDENTIFICATION DIVISION.
       PROGRAM-ID. PB188TVW.
      *> kb/Work PB188 — THE WIDTH OF A QUOTED-LITERAL VALUE SEEDED INTO A CHARACTER-IMAGE NUMERIC LEAF.
      *> PB188 landed the fix reasoning that its arm was DEAD: DataBinder.ValidateValueCategory meets a quoted
      *> literal on a numeric subject first (COBOLNET1657) and either errors or rewrites the raw text to the
      *> number, so item.RawValue no longer starts with a quote by the time either seeder sees it.  That is
      *> true of the FORMAT 1 input only.  ValueInitializer.InitializerFor's effRaw is
      *> `rawOverride ?? item.RawValue`, and rawOverride is fed per occurrence by TableValueInit from
      *> DataBinder.BuildTableValueSpecs - RAW text, quotes intact, on a path that never calls
      *> ValidateValueCategory (its only call sites are the format-1 RawValue and the level-88 arms).  So the
      *> arm is LIVE through the format 2 (table) VALUE, and this program is the shape that measures it.
      *>
      *> MEASURED on 8ca74a3d:  L1=[AA012012CC]  — ten characters where the group area is twelve.  The old
      *> spelling stored `StrStore(chars, pic.Length)`, the PICTURE's DIGIT COUNT (3), into a window whose
      *> width is the item's ImageWidth (4), so every following member of the group image was displaced.
      *> 13.18.52.4 GR6a is why the two numbers differ here: with SEPARATE CHARACTER "the operational sign is
      *> presumed to be the leading (or, respectively, trailing) character position of the data item ...; this
      *> character position is not a digit position", so PIC S9(3) SIGN SEPARATE occupies four positions and
      *> three digits.  Routing through NumFormatImage takes the width from the item's own pinned byte form.
      *>
      *> EXPECTED VALUES ARE DERIVED FROM THE SPEC, not from the run:
      *>   13.18.63.4 GR12/GR13 - the FROM (1) TO (2) phrase initializes occurrence 1 to the literal and
      *>     reuses the (single) literal for occurrence 2.  GR11 carries GR5 and GR7 into format 2.
      *>   The CCVS leniency - an alphanumeric literal on a numeric item is read AS the numeric literal
      *>     13.18.63.3 SR2 asked for: the numbers 12 and 45.
      *>   13.18.52.4 GR6b - the operational signs for positive and negative are '+' and '-'; both values are
      *>     positive, so the sign character is '+'.
      *>   13.18.52.4 GR6a - LEADING SEPARATE puts it in the leading character position and TRAILING SEPARATE
      *>     in the trailing one, over three zero-filled digit positions: LB = "+012", TB = "045+".
      *>   13.18.63.4 GR5/GR7 - the group area is the members' positions in order:
      *>     GL = "AA" + "+012" + "+012" + "CC";  GT = "AA" + "045+" + "045+" + "CC".
      *> The TRAILING twin is here because the LEADING one alone cannot tell a four-wide window from a
      *> three-wide one followed by a stray '+': it puts the extra position at the OTHER end.
      *>
      *> ⚠ THIS PROGRAM IS NOT CONFORMING, and it is pinned deliberately as the CCVS leniency it is.
      *> 13.18.63.3 SR2 is an ALL FORMATS rule - "If the category of the subject of the entry is numeric, all
      *> literals in the VALUE clause shall be numeric" - so a quoted literal on a numeric item is a violation
      *> in format 2 exactly as in format 1.  The format-1 screen exists (COBOLNET1657) and the format-2 one
      *> does NOT: that is kb/Work PB208's first half, OPEN.  When PB208 lands, this fixture moves behind
      *> --permissive; it must not be read as a decision that format-2 literals escape SR2.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       01 GL.
          05 LA PIC X(2) VALUE "AA".
          05 LB PIC S9(3) SIGN IS LEADING SEPARATE OCCURS 2
             VALUE "012" FROM (1) TO (2).
          05 LC PIC X(2) VALUE "CC".
       01 GT.
          05 TA PIC X(2) VALUE "AA".
          05 TB PIC S9(3) SIGN IS TRAILING SEPARATE OCCURS 2
             VALUE "045" FROM (1) TO (2).
          05 TC PIC X(2) VALUE "CC".
       PROCEDURE DIVISION.
       MAIN.
           DISPLAY "L1=[" GL "]"
           DISPLAY "L2=[" LB(1) "][" LB(2) "][" LC "]"
           DISPLAY "T1=[" GT "]"
           DISPLAY "T2=[" TB(1) "][" TB(2) "][" TC "]"
           STOP RUN.
