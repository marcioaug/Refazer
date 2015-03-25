﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    internal partial class IdentifierCollection
    {
        private abstract class CollectionBase : ICollection<string>
        {
            protected readonly IdentifierCollection IdentifierCollection;
            private int count = -1;

            protected CollectionBase(IdentifierCollection identifierCollection)
            {
                this.IdentifierCollection = identifierCollection;
            }

            public abstract bool Contains(string item);

            public void CopyTo(string[] array, int arrayIndex)
            {
                using (var enumerator = this.GetEnumerator())
                {
                    while (arrayIndex < array.Length && enumerator.MoveNext())
                    {
                        array[arrayIndex] = enumerator.Current;
                        arrayIndex++;
                    }
                }
            }

            public int Count
            {
                get
                {
                    if (this.count == -1)
                    {
                        this.count = this.IdentifierCollection.map.Values.Sum(o => o is string ? 1 : ((ISet<string>)o).Count);
                    }

                    return this.count;
                }
            }

            public bool IsReadOnly
            {
                get { return true; }
            }

            public IEnumerator<string> GetEnumerator()
            {
                foreach (var obj in this.IdentifierCollection.map.Values)
                {
                    var strs = obj as HashSet<string>;
                    if (strs != null)
                    {
                        foreach (var s in strs)
                        {
                            yield return s;
                        }
                    }
                    else
                    {
                        yield return (string)obj;
                    }
                }
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return this.GetEnumerator();
            }

            #region Unsupported  
            public void Add(string item)
            {
                throw new NotSupportedException();
            }

            public void Clear()
            {
                throw new NotSupportedException();
            }

            public bool Remove(string item)
            {
                throw new NotSupportedException();
            }
            #endregion
        }

        private sealed class CaseSensitiveCollection : CollectionBase
        {
            public CaseSensitiveCollection(IdentifierCollection identifierCollection) : base(identifierCollection)
            {
            }

            public override bool Contains(string item)
            {
                return IdentifierCollection.CaseSensitiveContains(item);
            }
        }

        private sealed class CaseInsensitiveCollection : CollectionBase
        {
            public CaseInsensitiveCollection(IdentifierCollection identifierCollection) : base(identifierCollection)
            {
            }

            public override bool Contains(string item)
            {
                return IdentifierCollection.CaseInsensitiveContains(item);
            }
        }
    }
}
