      *>--- conformance-case
      *> id: g2-copy-resultant-spacing
      *> rules: [2002-GR-7.1.2.3-12]
      *> accept: [2002]
      *> files: [G2CRSP.cpy]
      *> source-format: fixed
      *> portable: unverified
      *>---
      *> §2002:7.1.2.3 GR12 - "When copying text-words into the
      *> resultant text, additional spaces may be introduced only
      *> between text-words where there already exists a space or where
      *> a space is assumed." Library text G2CRSP has no space between
      *> "(" / ")" and G2N or G2M, nor inside PFX-NAME / CNT-SFX, so the
      *> resultant text may have none there. Parentheses are separators
      *> in text manipulation (§2002:7.1.1.4), so G2N is its own
      *> text-word and matches. Legs:
      *>   G2N BY 3 - the 2002 word-1 BY literal-4 form, inside two
      *>     pictures: PIC X(3) and PIC 9(3). A space introduced next to
      *>     the 3 would end the picture character-string at "X(" (a
      *>     space is a separator), not a valid picture: no compile.
      *>   ==G2M== BY ==2== - pseudo-text, PIC X(2), the same way.
      *>   LEADING ==PFX-== BY ==G2P-== and TRAILING ==SFX== BY ==G2Q==
      *>     (§2002:7.1.2.3 GR10f "the library text-word is placed into
      *>     the resultant text with the matched characters either
      *>     replaced by partial-word-2"): a space at the joint would
      *>     split G2P-NAME / CNT-G2Q, and the references below would
      *>     name nothing.
      *> EXPECTED: A=[ABC] ("ABCDEFG" into X(3) keeps the leftmost
      *> three), B=007 (VALUE 7 in 9(3)), P=WXYZ, S=42, C=[XY] ("XYZ"
      *> into X(2)).
       IDENTIFICATION DIVISION.
       PROGRAM-ID. G2CPY15.
       DATA DIVISION.
       WORKING-STORAGE SECTION.
       COPY G2CRSP REPLACING G2N BY 3 ==G2M== BY ==2==
                  LEADING ==PFX-== BY ==G2P-==
                  TRAILING ==SFX== BY ==G2Q==.
       PROCEDURE DIVISION.
       P1.
           MOVE "ABCDEFG" TO G2S-A
           DISPLAY "A=[" G2S-A "]"
           DISPLAY "B=" G2S-B
           DISPLAY "P=" G2P-NAME
           DISPLAY "S=" CNT-G2Q
           MOVE "XYZ" TO G2S-C
           DISPLAY "C=[" G2S-C "]"
           STOP RUN.
