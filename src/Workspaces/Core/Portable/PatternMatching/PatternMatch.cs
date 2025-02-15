﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.PatternMatching;

[DataContract]
internal readonly struct PatternMatch : IComparable<PatternMatch>
{
    /// <summary>
    /// The type of match that occurred.
    /// </summary>
    [DataMember(Order = 0)]
    public PatternMatchKind Kind { get; }

    [DataMember(Order = 1)]
    private readonly bool _punctuationStripped;

    /// <summary>
    /// True if this was a case sensitive match.
    /// </summary>
    [DataMember(Order = 2)]
    public bool IsCaseSensitive { get; }

    /// <summary>
    /// The spans in the original text that were matched.  Only returned if the 
    /// pattern matcher is asked to collect these spans.
    /// </summary>
    [DataMember(Order = 3)]
    public ImmutableArray<TextSpan> MatchedSpans { get; }

    internal PatternMatch(
        PatternMatchKind resultType,
        bool punctuationStripped,
        bool isCaseSensitive,
        TextSpan? matchedSpan)
        : this(resultType, punctuationStripped, isCaseSensitive,
               matchedSpan == null ? [] : [matchedSpan.Value])
    {
    }

    internal PatternMatch(
        PatternMatchKind kind,
        bool punctuationStripped,
        bool isCaseSensitive,
        ImmutableArray<TextSpan> matchedSpans)
    {
        this.Kind = kind;
        this.IsCaseSensitive = isCaseSensitive;
        this.MatchedSpans = matchedSpans;
        _punctuationStripped = punctuationStripped;
    }

    public PatternMatch WithMatchedSpans(ImmutableArray<TextSpan> matchedSpans)
        => new(Kind, _punctuationStripped, IsCaseSensitive, matchedSpans);

    public int CompareTo(PatternMatch other)
        => CompareTo(other, ignoreCase: false);

    public int CompareTo(PatternMatch? other, bool ignoreCase)
        => other.HasValue ? CompareTo(other.Value, ignoreCase) : -1;

    public int CompareTo(PatternMatch other, bool ignoreCase)
    {
        // 1. In all scenarios, An case sensitive camel match (like CR against CreateRange) should beat a case
        //    insensitive match (like 'CR' against 'Create').
        //
        // Other cases can be added here as necessary.

        switch (this.IsCaseSensitive, this.Kind.IsCamelCaseKind(), other.IsCaseSensitive, other.Kind.IsCamelCaseKind())
        {
            case (true, true, false, false):
                return -1;
            case (false, false, true, true):
                return 1;
        }

        // Compare types
        {
            var comparison = this.Kind - other.Kind;
            if (comparison != 0)
                return comparison;
        }

        if (!ignoreCase)
        {
            switch (this.IsCaseSensitive, other.IsCaseSensitive)
            {
                case (true, false):
                    return -1;
                case (false, true):
                    return 1;
            }
        }

        // Consider a match to be better if it was successful without stripping punctuation
        // versus a match that had to strip punctuation to succeed.
        return this._punctuationStripped.CompareTo(other._punctuationStripped);
    }
}
