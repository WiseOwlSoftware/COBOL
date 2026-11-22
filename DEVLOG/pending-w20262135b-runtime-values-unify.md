## Wave 20262135 group B: one unscaled-decimal reduction, one contiguous-record walk, one native comparison walker, one position read

Five R2 findings of the PB1754 review, landed as behavior-neutral restructurings (the run was interrupted by a Windows
reboot and resumed from its pushed branch).

- **PB2657.** The divide-by-ten reduction was spelled eight times in the runtime. `DecUnscaled.DigitCount` reads the
  digit count off the `Pow10` table and `DecUnscaled.StripTrailingZeros` keeps the loops' answers; both `CobolDec.DigitCount`
  overloads are deleted. `DecUnscaledTests` pins every 10^n boundary.
- **PB2654.** `CobolVarGroup`'s contiguous-record walk took `CobolContiguousLayout`'s fields as loose parameters; it now
  lives on `CobolContiguousLayout` (`Decompose`, `ComponentTake`, `ContiguousTake`).
- **PB2738.** CA1508 reported MatchWhen's live SR12 operand-3 category test as dead; the test carries a suppression naming
  the rule, so the code fix can no longer delete it.
- **PB2649.** `IF X = SPACES` allocated the figurative sized to X's width on every evaluation. A private `SizedOperand` view
  reads §2023:8.3.3.6.4 GR2's sized figurative in place (character i is `seed[i % seed.Length]`), and the native
  `Compare`, `CompareFig`, `ThruMember` and `ThruMemberFig` all walk one comparison loop and one THROUGH test.
  `FigToWidth` stays the one materializing form; `FigurativeReaderDriftTests` pins the walk to it.
- **PB2645.** `CobolTable.Occ`/`OccValue*` and `CobolString.RefModPosition`/`RefModValue*` were eight-overload twins
  that differed only in the Table 13 condition. `CobolPosition<TUse>` owns the read once; `TUse` is
  `PositionUse.Subscript` (§2023:8.4.2.3.4 GR1b) or `PositionUse.RefMod` (§2023:8.4.3.3.4 GR5), each a static-abstract
  `IPositionUse.NotAnInteger`. The emitted text changes (`CobolPosition<PositionUse.Subscript>.Read(J, _P_2)`); the
  characterization snapshot is re-baselined, the drift test compares one overload set, and the witnesses of nine inventory rows
  were re-sited by `record_verdicts` batches (GAP unchanged). The EC-BOUND-REF-MOD detail now cites
  §2023:8.4.3.3.4 GR5 instead of "item 5c", which shrank the citation debt baseline by three entries.

Runtime surface: one major bump (33) for the branch. Gate reds on the way: an unsorted PublicAPI.Unshipped, three stale
inventory witnesses and a test asserting the old citation text; each fixed at its root.

Left for the next agent of this group: PB2627, PB2636, PB2637, PB2737, PB2746 and PB2747, each a redesign of its own.
