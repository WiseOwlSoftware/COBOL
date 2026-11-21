---
heading: "### 9.3 The character set: no substitute graphic, no double character substitution (§1985:I.1.5.2.5.4, §1985:IV.4.1)"
---
§1985:IV.4.1 lets a character set lack a graphic for a COBOL character ("In these instances, a substitute graphic may be
specified to replace the character(s) not represented"; §1985:I.1.5.2.5.4), and requires double characters where the set is
small: "When a character set contains fewer than 52 characters (all characters of the COBOL character set except the
lowercase letters), double characters must be substituted for the single characters". Neither case arises in WiseOwl
COBOL. A source program is read as UTF-8, UTF-16, UTF-32 or ISO/IEC 8859-1 text (DOC-A.1-25), and each of those holds every
character of the COBOL character set, the lowercase letters included, with its own graphic and its own code point. So the
character set has more than 52 characters: no substitute graphic is specified, and no double character is substituted for a
single one. Double character substitution is item 1 of the obsolete list (§1985:C.1); it is
"dependent upon the character set available with the computer" (§1985:C.3 item 1), and its support is the case it names,
which never arises here. The same holds at every edition (DOC-A.1-25).
