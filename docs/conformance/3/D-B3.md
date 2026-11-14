- **D-B3 — the usage of a boolean literal is DISPLAY, so a MOVE of one to a group deposits its '0'/'1' characters**
  (kb/Work PB3562). A MOVE to an alphanumeric group is §2023:14.9.25.4 GR4's "alphanumeric to alphanumeric elementary
  move, except that there is no conversion of data from one form of internal representation to another", so the
  sender's representation, and therefore its usage, decides the bytes deposited. A boolean *function* has a stated
  usage — §2023:15.2 2) "Boolean functions have an implicit usage bit", §2023:15.13.4 1) "a boolean item of usage bit" — and so
  does a national literal (§2023:8.3.3.5.4 GR3 a), "usage NATIONAL"), so each deposits its own representation (packed bits,
  UTF-16BE pairs: `MOVE FUNCTION BOOLEAN-OF-INTEGER(102 8) TO G` deposits the one byte X"66"). A boolean *literal* has no
  stated usage: §2023:8.3.3.4.4 GR3 gives only its value, "the sequence of occurrences of boolean-character-1". WiseOwl
  COBOL reads it as what it is written with, the characters '0' and '1' "from the computer's coded character set"
  (§2023:8.3.3.4.3 SR2): usage DISPLAY, one byte a position, so `MOVE B"01100110" TO G` deposits the eight characters
  01100110. That is the answer the compiler's one static-usage reader (`IntrinsicArgumentRules.StaticUsageOf`) already
  gave every argument screen (a boolean literal passes CONCAT's display-usage screen), and the same usage a boolean
  item written without USAGE BIT has (§2023:13.18.60.4 GR7); the alternative, packing the literal as bits, would make one
  literal two usages. A zero-length boolean literal is not affected: GR3 makes it the figurative ZERO before the sender
  is asked. Written once, in `OperandText.NonElementaryMoveSender`, which every group-move sender channel (MOVE, an
  implicit MOVE such as WRITE FROM, a CALL / INVOKE argument into a group formal) reads - directly where the formal is
  known to the activating element, and through the argument's second alphabet where it is not (kb/Work PB3587): a
  Format-1 CALL, or a CALL located at run time, cannot see the formal, so it states a BY CONTENT argument's `Sending`
  with its characters (what an elementary formal reads) and, for a sender of usage national or bit,
  its representation (`MoveSending.StorageImage`, produced by `OperandText.StorageImageIfDiffers` / `StorageImageConverter`
  from the same codec), which a plain-group formal's record is filled from (`CobolArgAdapt.Text`); an INVOKE literal
  crosses as the bound literal, its category kept (`BoundInvokeArg.StringLiteral`), so a national literal is usage
  national there too. Pinned by `GroupMoveSenderChannelDriftTests`;
  witnessed by `conformance:wiseowl/cases/14.9.25-MOVE/pb3562_boolean_literal_group_move` and
  `conformance:cases/14.2-Procedure-division-structure/pb3587_content_group_formal_usage_values`.
