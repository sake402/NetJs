using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

public static class TypeSymbolSorterExtensions
{
    public static List<T> SortTypesForJavaScript<T>(this IEnumerable<T> items, Func<T, INamedTypeSymbol> typeSelector)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));
        if (typeSelector == null) throw new ArgumentNullException(nameof(typeSelector));

        var sortedList = new List<T>();
        var visited = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        var visiting = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);

        var symbolToItemMap = new Dictionary<INamedTypeSymbol, T>(SymbolEqualityComparer.Default);
        foreach (var item in items)
        {
            var symbol = typeSelector(item);
            symbolToItemMap[symbol.OriginalDefinition] = item;
            var innerTypes = symbol.GetTypeMembers();
            foreach (var innerType in innerTypes)
            {
                symbolToItemMap[innerType.OriginalDefinition] = item;
            }
        }

        foreach (var item in items)
        {
            var symbol = typeSelector(item);
            Visit(symbol, symbolToItemMap, visiting, visited, sortedList);
        }

        //Debug.Assert(sortedList.Count == symbolToItemMap.Count);
        return sortedList;
    }

    private static bool Visit<T>(
        INamedTypeSymbol symbol,
        Dictionary<INamedTypeSymbol, T> symbolToItemMap,
        HashSet<INamedTypeSymbol> visiting,
        HashSet<INamedTypeSymbol> visited,
        List<T> sortedList,
        List<T>? isTryList = null)
    {
        if (symbol == null) return false;
        if (symbol.Name == "int" || symbol.Name == "Int32")
        {

        }
        var definition = symbol.OriginalDefinition;
        if (!visiting.Add(definition))
        {
            return false;
        }

        void TryWriteTypeArguments(INamedTypeSymbol symbol, Func<INamedTypeSymbol, bool>? filter = null)
        {
            foreach (var typeArg in symbol.TypeArguments)
            {
                if (typeArg is INamedTypeSymbol namedTypeArg && (filter?.Invoke(namedTypeArg) ?? true)/* && namedTypeArg.ContainingType == null */&& symbolToItemMap.ContainsKey(namedTypeArg.OriginalDefinition))
                {
                    if (!visited.Contains(namedTypeArg.OriginalDefinition))
                    {
                        var _visitedBefore = visited.ToHashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
                        //var _sortedListBefore = sortedList.ToList();
                        var tryList = new List<T>();
                        if (Visit(namedTypeArg, symbolToItemMap, visiting, visited, sortedList, isTryList: tryList))
                        {
                            sortedList.AddRange(tryList);
                        }
                        else
                        {
                            if (visited.Count != _visitedBefore.Count)
                            {
                                //restore visited back to before the visit, since we are not adding the type to the sorted list
                                visited.Clear();
                                foreach (var e in _visitedBefore)
                                {
                                    visited.Add(e);
                                }
                            }
                            //sortedList.Clear();
                            //foreach (var e in _sortedListBefore)
                            //{
                            //    sortedList.Add(e);
                            //}
                        }
                    }
                    TryWriteTypeArguments(namedTypeArg);
                }
            }

        }

        bool visitResult = true;
        try
        {
            if (!visited.Contains(definition))
            {
                if (symbol.ContainingType != null && symbolToItemMap.ContainsKey(symbol.ContainingType.OriginalDefinition))
                {
                    var result = Visit(symbol.ContainingType, symbolToItemMap, visiting, visited, sortedList, isTryList: isTryList);
                    if (isTryList != null && !result)
                        return false;
                }

                if (symbol.BaseType != null && symbolToItemMap.ContainsKey(symbol.BaseType.OriginalDefinition))
                {
                    var result = Visit(symbol.BaseType, symbolToItemMap, visiting, visited, sortedList, isTryList: isTryList);
                    if (isTryList != null && !result)
                        return false;
                }
                else if (symbol.BaseType != null && symbol.BaseType.Arity > 0) //since we cant check the base type, at cheast check the arg
                {
                    TryWriteTypeArguments(symbol.BaseType, e => e.ContainingType != null);
                }

                if (symbol.BaseType != null && symbol.BaseType.ContainingType != null && symbolToItemMap.ContainsKey(symbol.BaseType.ContainingType.OriginalDefinition))
                {
                    var result = Visit(symbol.BaseType.ContainingType, symbolToItemMap, visiting, visited, sortedList, isTryList: isTryList);
                    if (isTryList != null && !result)
                        return false;
                }

                foreach (var iface in symbol.Interfaces)
                {
                    if (symbolToItemMap.ContainsKey(iface.OriginalDefinition))
                    {
                        var result = Visit(iface, symbolToItemMap, visiting, visited, sortedList, isTryList: isTryList);
                        if (isTryList != null && !result)
                            return false;
                    }
                }

                var innerTypes = symbol.GetTypeMembers();
                foreach (var innerType in innerTypes)
                {
                    if (symbolToItemMap.ContainsKey(innerType.OriginalDefinition))
                    {
                        var result = Visit(innerType, symbolToItemMap, visiting, visited, sortedList, isTryList: isTryList);
                        if (isTryList != null && !result)
                            return false;
                    }
                }

                //prioritize inner generic type arguments
                TryWriteTypeArguments(symbol, e => e.ContainingType != null);

                //dont sort/add inner type, they are rendered within their original type
                var rootType = definition;
                while (rootType.ContainingType != null)
                {
                    rootType = rootType.ContainingType.OriginalDefinition;
                }
                if (definition.ContainingType != null)
                {
                    var result = Visit(rootType, symbolToItemMap, visiting, visited, sortedList, isTryList: isTryList);
                    if (isTryList != null && !result)
                        return false;
                }
                else if (visited.Add(rootType))
                {
                    (isTryList ?? sortedList).Add(symbolToItemMap[rootType]);
                }
            }
            else
            {
                //visitResult = false;
            }

            TryWriteTypeArguments(symbol);
            if (symbol.BaseType != null)
                TryWriteTypeArguments(symbol.BaseType);
            foreach (var iface in symbol.Interfaces)
                TryWriteTypeArguments(iface);
        }
        finally
        {
            visiting.Remove(definition);
        }
        return visitResult;
    }
}
