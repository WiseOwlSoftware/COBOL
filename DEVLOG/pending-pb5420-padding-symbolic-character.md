## PB5420: PADDING CHARACTER accepts a symbolic-character, with or without ALL

At `--std 85`, `PADDING CHARACTER IS SYM1` (SYM1 a SYMBOLIC CHARACTERS name) was refused COBOL1639 because `CapturePadding` tried the word as data-name-1 first, and `IS ALL SYM1` was refused COBOL6848 because `OneCharacter` knew only keyword figuratives and `ALL literal`. A symbolic-character is a figurative constant (`§1985:IV.4.2.2.2.3 GR7`), and a figurative constant stands for one character where `§1985:VII.2.7.3 SR1` asks for literal-1. Re-probe on this tree: padsym COBOL1639, padallsym COBOL6848, control padquote ran.

Fix, all in `FileControlBinder.Padding.cs`: a bare word is read as a symbolic-character after the constant-name check and before data-name-1; `OneCharacter` reads Format 6 (`ALL literal-1`, which now also takes `ALL constant-name`) through `ConcatFolder.FoldAllLiteral`, the one reader, and Format 7 through a small `SymbolicCharacter` helper shared with the bare-word arm. After: all three probes run `READ ABCD`.

Golden: `pb5420-padding-symbolic-character` (85, two files, bare and ALL forms, read back). Rows closed: none (a 1985 rule id is not recordable in a row list, PB5094). Self-review: sibling `FigurativeChar` callers (DataBinder VALUE, STOP literal) belong to other mechanisms; none reads this clause.
