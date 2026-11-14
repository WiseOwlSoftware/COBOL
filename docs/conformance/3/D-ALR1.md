- **D-ALR1 — the length of an ANY LENGTH RETURNING item when the activation has no receiving item** (kb/Work PB1167;
  ISO §2023:13.18.2.3 SR3 b) and §2023:13.18.2.4 GR1 b)). §2023:13.18.2.3 SR3 admits the clause's subject as the RETURNING item of a
  contained program or a method (cite-checked), and §2023:13.18.2.4 GR1 b) fixes its length n as "the length of the
  corresponding argument or returning item of the activating runtime element"; GR1 a) makes it a zero-length item when
  that returning item is zero-length. With a receiver, n is the receiver's length and the standard decides it. The
  standard is silent on the one activation that has NO receiver: a CALL or INVOKE whose RETURNING phrase is absent
  (§2023:14.8.3.1 requires the phrase exactly when the activated element declares a RETURNING item, so only an activation
  the compiler cannot check statically, a dynamic CALL, can reach it). **WiseOwl COBOL gives the item the length its
  PICTURE spells (one position for the one-symbol PICTURE §2023:13.18.2.3 SR1 requires).** Rejected readings: zero length
  (a MOVE into it would silently store nothing, and the value is discarded by GR4 anyway, so the choice cannot change
  the activator's data) and a diagnostic (the activation is legal source). GnuCOBOL has no ANY LENGTH RETURNING item, so
  no vendor behavior applies (rule 1 precedence: ISO, then GnuCOBOL, then IBM / Micro Focus). Written once on each ABI:
  the program ABI seeds the item at activation entry from the receiver's `CobolArg` (`CobolArgAdapt.ReturningSeed`) and
  the method ABI receives n as the trailing `__retLen` parameter that `OoEmitter.OoSignatureOf` appends for exactly the
  methods whose RETURNING item is ANY LENGTH. A universal-receiver INVOKE of such a method sets EC-OO-UNIVERSAL
  (§2023:14.9.23.4 GR7 c)), an inline invocation of it is refused (§2023:8.4.3.4.3 SR4, COBOL2140), and a PROPERTY method may
  not carry it (§2023:13.18.2.3 SR2). Witnesses: `conformance:cases/13.18.2-ANY-LENGTH/pb1167_any_length_returning`,
  `unit:ReturningSeedTests.No_Receiver_Gives_The_Declared_Picture_Length`,
  `unit:ReturningSeedTests.Zero_Length_Receiver_Gives_A_Zero_Length_Item`,
  `conformance:cases/_unassigned/pb1167-inline-invocation-returning-any-length`,
  `conformance:cases/_unassigned/pb1167-any-length-returning-property-method`,
  `conformance:cases/14.8.3.3-Elementary-items/pb1167-returning-fixed-sender-any-length-receiver`.
