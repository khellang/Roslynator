﻿// Copyright (c) Josef Pihrt. All rights reserved. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Roslynator
{
    internal sealed class NamedTypeSymbolDefinitionComparer : IComparer<INamedTypeSymbol>
    {
        public static NamedTypeSymbolDefinitionComparer Instance { get; } = new NamedTypeSymbolDefinitionComparer(systemNamespaceFirst: false);

        public static NamedTypeSymbolDefinitionComparer SystemNamespaceFirstInstance { get; } = new NamedTypeSymbolDefinitionComparer(systemNamespaceFirst: true);

        public bool SystemNamespaceFirst { get; }

        internal NamedTypeSymbolDefinitionComparer(bool systemNamespaceFirst = false)
        {
            SystemNamespaceFirst = systemNamespaceFirst;
        }

        public static NamedTypeSymbolDefinitionComparer GetInstance(bool systemNamespaceFirst)
        {
            return (systemNamespaceFirst) ? SystemNamespaceFirstInstance : Instance;
        }

        public int Compare(INamedTypeSymbol x, INamedTypeSymbol y)
        {
            Debug.Assert(x.IsDefinition, $"symbol is not definition: {x.ToDisplayString()}");
            Debug.Assert(y.IsDefinition, $"symbol is not definition: {y.ToDisplayString()}");

            if (object.ReferenceEquals(x, y))
                return 0;

            if (x == null)
                return -1;

            if (y == null)
                return 1;

            int diff = NamespaceSymbolDefinitionComparer.GetInstance(SystemNamespaceFirst).Compare(x.ContainingNamespace, y.ContainingNamespace);

            if (diff != 0)
                return diff;

            diff = GetRank(x).CompareTo(GetRank(y));

            if (diff != 0)
                return diff;

            int count1 = CountContainingTypes(x);
            int count2 = CountContainingTypes(y);

            while (true)
            {
                INamedTypeSymbol containingType1 = GetContainingType(x, count1);
                INamedTypeSymbol containingType2 = GetContainingType(y, count2);

                diff = SymbolDefinitionComparer.CompareName(containingType1, containingType2);

                if (diff != 0)
                    return diff;

                diff = containingType1.TypeParameters.Length.CompareTo(containingType2.TypeParameters.Length);

                if (diff != 0)
                    return diff;

                if (count1 == 0)
                    return (count2 == 0) ? 0 : -1;

                if (count2 == 0)
                    return 1;

                count1--;
                count2--;
            }

            int CountContainingTypes(INamedTypeSymbol namedType)
            {
                int count = 0;

                while (true)
                {
                    namedType = namedType.ContainingType;

                    if (namedType == null)
                        break;

                    count++;
                }

                return count;
            }

            INamedTypeSymbol GetContainingType(INamedTypeSymbol namedType, int count)
            {
                while (count > 0)
                {
                    namedType = namedType.ContainingType;
                    count--;
                }

                return namedType;
            }

            int GetRank(INamedTypeSymbol symbol)
            {
                switch (symbol.TypeKind)
                {
                    case TypeKind.Class:
                        return 1;
                    case TypeKind.Struct:
                        return 2;
                    case TypeKind.Interface:
                        return 3;
                    case TypeKind.Enum:
                        return 4;
                    case TypeKind.Delegate:
                        return 5;
                }

                Debug.Fail(symbol.ToDisplayString(SymbolDisplayFormats.Test));

                return 0;
            }
        }
    }
}