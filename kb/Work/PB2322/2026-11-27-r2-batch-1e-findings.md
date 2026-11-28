## R2 batch 1e (pin 9e65164eb3a9): 1 finding on this note

### `procedure-binding-1--architecture--f1#1` (finding, extract, votes upheld/upheld/upheld)

**Finding:** ExpressionBinder decomposition (DESIGN-architecture-review 8.3, PB2322) has no target for its RECEIVING family, a third of the type

**Rule:** single responsibility per type (DESIGN-architecture-review 5.1) and the 8.3 rule that every god class decomposition names a target for each responsibility; the type summary itself names three families (OPERAND, EXPRESSION, RECEIVING) while the 8.3 row and PB2322 list only OperandContextRules, ArithmeticExpressionBinder and OperandBinder

**Scenario:** ExpressionBinder grew from 1189 lines / 52 members at the census PB2322 was filed from (79a4b6c58) to 1475 lines / 73 members at the pin (R0-0037). Lines 800-1178 are the receiving-side chokepoint the summary calls THE one receiving-side chokepoint: ResolveSending, ResolveReceiving x3, ResolveReceivingAtStore x2 with IdentifiedAtStore, ReceivingReferences, RefuseNonReceivable x2, PermitsReceiving, ReceivingFormBar/ReceivingPlaceBar, ScreenResultant, Receivers x3, ResolvedReceiver, ReceiverOf and RoundingOf, called from 25 verb binders and pinned by ReceivingResolutionDriftTests. A PB2322 wave executed as written must either leave these ~380 lines on a residual ExpressionBinder (the god class survives under its old name) or push them into OperandBinder, whose seam is Bind(ctx, operand context) -> BoundOperand, a SENDING funnel; the receiving bars (8.4.3.x SR prohibitions, 13.18.15.3 SR2, 13.7.3 SR5, the arithmetic resultant categories) would then sit beside the sending class screens they deliberately do not share (ScreenResultant summary: collapsing the two would lose the numeric-edited axis).

**Target:** A fourth target type in the 8.3 row and in PB2322: ReceivingOperandBinder (Binding/Procedure), owning the receiving chokepoint and its table ReceivingOperandForms, exposing Resolve(receivingIdentifier|dataReference) -> Place?, ResolveAtStore(...) -> Place?, Permits(dref, place) and Receivers(...) -> List<Receiver>; RoundingOf moves with Receivers (its only bind-side reader of the ROUNDED phrase besides the verb binders). ReceivingResolutionDriftTests then pins the new type as the one entry. The 8.3 row is amended to name the four targets.

**Finding sites (the pin's):** src/WiseOwl.Cobol.Compiler/Binding/Procedure/ExpressionBinder.cs:184-197 (ExpressionBinder summary: three families); src/WiseOwl.Cobol.Compiler/Binding/Procedure/ExpressionBinder.cs:806-1178 (ExpressionBinder.RoundingOf .. ResolvedReceiver)

The finding is about this note's own target: the extraction has no target for the RECEIVING family, a third of the type. It is recorded here and not filed apart. The sites are the pin's.
