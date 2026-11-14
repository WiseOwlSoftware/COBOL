---
order: 25
---
> ⚖ **DETERMINATION — a sequential READ whose I-O status is '46' takes neither the AT END nor the NOT AT END
> phrase (§2023:14.9.30.4 GR21 → GR24)** (2026-09-22; kb/Work PB810). GR21 sets '46' when "the previous READ or START
> statement for the file connector was unsuccessful" and says execution "proceeds as indicated in General rule 24".
> Read as "all of GR24", the pointer would run the AT END imperative and also overwrite GR21's own '46' with GR24
> a)'s '10'. **WiseOwl COBOL does not read it that way.** GR24's actions are conditioned on its own first words — "If,
> during the execution of the READ statement, the at end condition exists" — and '46' is not the at end condition:
> §2023:9.1.13.7 6) makes it a LOGIC ERROR, which §2023:9.1.13.1 defines as an unsuccessful execution "as a result of an
> improper sequence of input-output operations", while the at end condition is §2023:9.1.13.4's '10'/'14'. So the rule
> that governs is GR13 — "If neither an at end nor an invalid key condition occurs during the execution of a READ
> statement, the AT END phrase or the INVALID KEY phrase is ignored" — and GR13 b) transfers control through
> §2023:9.1.12's input-output exception processing (a USE declarative, then the end of the READ). GnuCOBOL and IBM
> Enterprise COBOL take the AT END phrase for the at end condition only. The status→condition table is written
> once, in `CodeGen/Verbs/IoStatusClass.cs` (guarded by `IoStatusClassDriftTests`), and the determination is
> pinned on all three organizations — both emitter arms — by `85/pb810_read_46_takes_no_at_end`.
