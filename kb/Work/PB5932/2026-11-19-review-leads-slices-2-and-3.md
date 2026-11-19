## Review leads of 6b slices 2 and 3, still open on main after slice 4 (filed by the controller, 2026-11-19)

Both were recorded as low, non-blocking leads by the branch reviews (findings files review-listing-step-6b-2.jsonl and
review-listing-step-6b-3.jsonl, lead R1-1 each). They landed unfixed. Checked on origin/main after slice 4 landed:

1. **Doc currency (slice 2, R1-1).** `docs/rearchitecture/DESIGN-listing.md` still names the deleted `ProcedureNameMap` /
   `ProcedureNameMap<T>` in section 6.7 (line 873), section 6.7.1 (a) (line 948), the slice-2 paragraph (line 1103) and the
   ownership table (line 1317). Slice 2 made it SymbolTable's private `ProcedureNames<T>`. Rule 6.
2. **A false design claim and string lookups left in the constant class (slice 3, R1-1).** Section 6.7.1's slice-3 paragraph
   (line 1142) says "No string reaches the table from a written name", but SymbolTable still takes the written name as a
   `string` in IsDeclaredConstant, IsConstantName, IsRefusedConstantName, IsFailedConstant, NamesConstant,
   InheritedConstantDepthOf, MethodDeclaresWord and DeclareMethodWord (six take `(string` on main). NamesConstant and
   IsFailedConstant bind an entry on demand through ConstantNamed, so they are resolutions by string, not yes/no questions.

**Owed by the next 6b slice, before PB5932 can land:** move those callers onto Identify / WrittenWord like the rest of the
constant class (or, for a caller with no written-word site, make 6.7.1 name it and say why), count them in the baseline if
they are rule-3 lookups, and correct the four ProcedureNameMap mentions. The last slice may not empty the baseline while
either is open.
