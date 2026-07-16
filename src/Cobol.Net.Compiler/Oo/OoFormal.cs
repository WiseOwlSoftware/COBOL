// Copyright (c) 2026 Brent Rector. All rights reserved.
// Licensed under the Business Source License 1.1. See LICENSE file in the project root.
using CobolNet.Binding;
using CobolNet.Binding.Model;
using CobolNet.Frontend.Generated;

namespace CobolNet.Compiler.Oo;

/// <summary>One resolved USING formal: the LINKAGE item (its <see cref="DataItem.CsName"/> is the capturable
/// LOCAL the body addresses), the 0-based positional slot, and the emitted C# parameter name.</summary>
public sealed record OoFormal(DataItem Item, int Position, string ParamName);
