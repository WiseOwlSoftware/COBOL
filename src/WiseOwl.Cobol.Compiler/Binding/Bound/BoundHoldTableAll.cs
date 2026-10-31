// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding.Bound;

/// <summary>
/// ⛔ THE HELD FORM OF A table(ALL) FUNCTION ARGUMENT (ISO §8.4.3.2.4 GR2, §15.3; kb/Work PB4162): the pre-op that copies the
/// elements <paramref name="Source"/> enumerates, in the order §15.3 gives them, into the compiler-temporary table
/// <paramref name="Held"/> ranges over, at the argument's place in the argument list.
/// <para>GR2 evaluates a function's arguments "individually in the order specified in the list of arguments, from left to
/// right", and §15.3 makes <c>T (ALL)</c> "as if each table element associated with that subscript position were specified"
/// - a sequence of arguments, each evaluated where it stands. A later argument's user-defined function activation is a
/// statement-scoped pre-op that runs before the statement renders its call, so an ALL operand read where the call renders
/// read the table AFTER the activation, and a nested call that named it could not be held (kb/Work PB4124's residual). The
/// held form is a table of the element's own description, so everything downstream - the lane an element is read in, the
/// description <c>FUNCTION LENGTH</c> measures (DOC-A.1-92) - is what it was for the table itself, and the consumers keep
/// asking the one question they ask of any <see cref="TableAllPlace"/>.</para>
/// <para><paramref name="Held"/> ranges over ONE flat level: the elements in enumeration order, however many ALL levels
/// <paramref name="Source"/> has. Its count is <paramref name="Source"/>'s own when every range is fixed
/// (<see cref="TableAllPlace.StaticCount"/>), else the number of elements the copy counted, held in
/// <paramref name="Count"/> (an <see cref="AllCount.Odo"/> over it, bounded by the product of the levels' maxima).</para>
/// </summary>
/// <param name="Source">The enumeration being held, as bound.</param>
/// <param name="Held">The enumeration over the temporary table that replaces <paramref name="Source"/> in the call.</param>
/// <param name="Count">The numeric temporary the copy stores the number of elements into; null when the count is static.</param>
/// <param name="Copy">The store of ONE element: <paramref name="Source"/>'s element into <paramref name="Held"/>'s, the
/// identity copy every held operand gets (<c>SendingValueTemp.IdentityStore</c>); a child statement of this node.</param>
public sealed record BoundHoldTableAll(TableAllPlace Source, TableAllPlace Held, Place? Count, BoundStatement Copy) : BoundStatement;
