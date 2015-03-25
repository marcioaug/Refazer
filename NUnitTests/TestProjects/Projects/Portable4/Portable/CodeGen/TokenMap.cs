﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// Handles storage of items referenced via tokens in metadata (strings or Symbols).
    /// When items are stored they are uniquely "associated" with fake token, which is basically 
    /// a sequential number.
    /// IL gen will use these fake tokens during codegen and later, when actual token values are known
    /// the method bodies will be patched.
    /// To support thse two scenarios we need two maps - Item-->uint, and uint-->Item.  (the second is really just a list).
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class TokenMap<T> where T : class
    {
        private readonly ConcurrentDictionary<T, uint> itemIdentityToToken = new ConcurrentDictionary<T, uint>(ReferenceEqualityComparer.Instance);

        private readonly Dictionary<T, uint> itemToToken = new Dictionary<T, uint>();
        private readonly ArrayBuilder<T> items = new ArrayBuilder<T>();

        public uint GetOrAddTokenFor(T item, out bool referenceAdded)
        {
            uint tmp;
            if (itemIdentityToToken.TryGetValue(item, out tmp))
            {
                referenceAdded = false;
                return (uint)tmp;
            }

            return AddItem(item, out referenceAdded);
        }

        private uint AddItem(T item, out bool referenceAdded)
        {
            uint token;

            // NOTE: cannot use GetOrAdd here since items and itemToToken must be in sync
            // so if we do need to add we have to take a lock and modify both collections.
            lock (items)
            {
                if (!itemToToken.TryGetValue(item, out token))
                {
                    token = (uint)items.Count;
                    items.Add(item);
                    itemToToken.Add(item, token);
                }
            }

            referenceAdded = itemIdentityToToken.TryAdd(item, token);
            return token;
        }

        public T GetItem(uint token)
        {
            lock (items)
            {
                return items[(int)token];
            }
        }

        public IEnumerable<T> GetAllItems()
        {
            lock (items)
            {
                return items.ToArray();
            }
        }

        //TODO: why is this is called twice during emit?
        //      should probably return ROA instead of IE and cache that in Module. (and no need to return count)
        public IEnumerable<T> GetAllItemsAndCount(out int count)
        {
            lock (items)
            {
                count = items.Count;
                return items.ToArray();
            }
        }
    }
}
