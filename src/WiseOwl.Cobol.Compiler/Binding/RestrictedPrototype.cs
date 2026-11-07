// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using WiseOwl.Cobol.Binding.Model;

namespace WiseOwl.Cobol.Binding;

/// <summary>The prototype a restricted program- or function-pointer's <c>TO prototype-name-1</c> phrase resolves to
/// through its unit's prototype tables (<c>BinderDriver.ProgramPrototypesOf</c> / <c>UserFunctionsOf</c>, ISO §2023:8.4.6.8 /
/// §2023:8.4.6.6 and §2023:12.3.8.4 GR10 / GR11): its namespace, its externalized name, and its signature — the external repository
/// entry's when GR10 c) / GR11 c) take the details from there (DESIGN-external-repository §8.2). Attached to the pointer as
/// <see cref="DataItem.RestrictedPrototype"/> (kb/Work PB2495), it gives the pointer its two restrictions: the TO phrase's
/// identity, the externalized name (<see cref="PrototypeSignatures.NamedRestriction(DataItem)"/>), and what it may contain,
/// the identity of the signature (<see cref="PrototypeSignatures.Identity(RestrictedPrototype)"/>, ISO §2023:13.18.60.4 GR25 /
/// GR26; kb/Work PB4491). <paramref name="Origin"/> is null for a prototype the compilation group declares, and the record
/// it was decoded from otherwise.</summary>
internal sealed record RestrictedPrototype(bool Function, string ExternalizedName, CalleeSignature Signature, PrototypeOrigin? Origin = null)
{
    public static RestrictedPrototype Of(ProgramPrototype program) =>
        new(Function: false, program.ExternalizedName, program.Signature);

    public static RestrictedPrototype Of(UserFunctionSignature function) =>
        new(Function: true, function.Externalized, new CalleeSignature(function.Formals, function.Returning));
}

/// <summary>Where a decoded prototype comes from (DESIGN-external-repository §6.4 part 1): the repository record that
/// describes it, so the encoder writes one <c>ReferencedPrototypes</c> row per prototype it meets and never folds two
/// records' prototypes of one name — or a decoded prototype and the group's — into one row (<c>RepositoryCodec</c>).
/// An identity only, compared by reference — the decoder makes one per referenced prototype of the record it decodes.</summary>
internal sealed class PrototypeOrigin;
