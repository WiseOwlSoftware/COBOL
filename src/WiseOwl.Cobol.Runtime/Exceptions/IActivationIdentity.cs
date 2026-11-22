// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Runtime.Exceptions;

/// <summary>The two activation identities the exception engine reads (kb/Work PB2743): the activation now executing and
/// the one that activated it. A staged RAISING condition names its ACTIVATING activation and a pickup takes it only in
/// that activation (kb/Work PB892), and a Format-3 PERFORM frame records the activation it runs in. The run unit's one
/// activation record (<c>ModuleStack</c>, in the control layer above) implements it, so the engine never names the
/// control layer.</summary>
public interface IActivationIdentity
{
    /// <summary>The identity of the activation now executing; 0 when none is.</summary>
    long CurrentActivation { get; }

    /// <summary>The identity of the activation that activated the one now executing; -1 when there is none.</summary>
    long ActivatingActivation { get; }
}
