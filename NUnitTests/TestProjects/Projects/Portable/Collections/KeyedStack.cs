﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Collections
{
    internal class KeyedStack<T, R>
    {
        private readonly Dictionary<T, Stack<R>> dict = new Dictionary<T, Stack<R>>();

        public void Push(T key, R value)
        {
            Stack<R> store;
            if (!dict.TryGetValue(key, out store))
            {
                store = new Stack<R>();
                dict.Add(key, store);
            }

            store.Push(value);
        }

        public bool TryPop(T key, out R value)
        {
            Stack<R> store;
            if (dict.TryGetValue(key, out store) && store.Count > 0)
            {
                value = store.Pop();
                return true;
            }

            value = default(R);
            return false;
        }
    }
}
