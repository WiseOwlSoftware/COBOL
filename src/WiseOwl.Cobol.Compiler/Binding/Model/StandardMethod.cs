// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
namespace WiseOwl.Cobol.Binding.Model;

/// <summary>Which method of the standard class BASE an <see cref="OoMethodSymbol"/> is (ISO §2023:16.2).</summary>
public enum StandardMethod
{
    /// <summary>A method written in the compilation group.</summary>
    None,

    /// <summary>BaseFactoryInterface's <c>New</c> — ISO §2023:16.2.1.2 GR1: "The New method allocates storage for an
    /// object, initializes its instance data in accordance with 14.6.2.4, Initial state of object data, and
    /// returns a reference to the created object."</summary>
    New,

    /// <summary>BaseInterface's <c>FactoryObject</c> — ISO §2023:16.2.2.2 GR1: "When invoked on an instance object,
    /// the FactoryObject method determines the class of the object and returns a reference to the factory object
    /// associated with that class."</summary>
    FactoryObject,
}
