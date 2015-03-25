﻿// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// An global option. An instance of this class can be used to access an option value from an OptionSet.
    /// </summary>
    public class Option<T> : IOption
    {
        /// <summary>
        /// Feature this option is associated with.
        /// </summary>
        public string Feature { get; private set; }

        /// <summary>
        /// The name of the option.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// The type of the option value.
        /// </summary>
        public Type Type
        {
            get { return typeof(T); }
        }

        /// <summary>
        /// The default value of the option.
        /// </summary>
        public T DefaultValue { get; private set; }

        public Option(string feature, string name, T defaultValue = default(T))
        {
            if (string.IsNullOrWhiteSpace(feature))
            {
                throw new ArgumentNullException("feature");
            }

            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("name");
            }

            this.Feature = feature;
            this.Name = name;
            this.DefaultValue = defaultValue;
        }

        Type IOption.Type
        {
            get { return typeof(T); }
        }

        object IOption.DefaultValue
        {
            get { return this.DefaultValue; }
        }

        bool IOption.IsPerLanguage
        {
            get { return false; }
        }

        public override string ToString()
        {
            return string.Format("{0} - {1}", this.Feature, this.Name);
        }

        public static implicit operator OptionKey(Option<T> option)
        {
            return new OptionKey(option);
        }
    }
}
