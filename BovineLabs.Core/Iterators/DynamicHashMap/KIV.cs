// <copyright file="KIV.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;

    [DebuggerDisplay("Key = {Key}, Index = {Indexed}, Value = {Value}")]
    public unsafe struct KIV<TKey, TIndex, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        internal DynamicIndexedMapHelper<TKey, TIndex, TValue>* Data;
        internal int Index;

        /// <summary> Gets an invalid KeyValue. </summary>
        public static KIV<TKey, TIndex, TValue> Null => new() { Index = -1 };

        /// <summary> Gets the key. </summary>
        /// <value> The key. If this KeyValue is Null, returns the default of TKey. </value>
        public TKey Key
        {
            get
            {
                if (this.Index != -1)
                {
                    return this.Data->KeyHash.Keys[this.Index];
                }

                return default;
            }
        }

        /// <summary> Gets the index. </summary>
        public TIndex Indexed
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return this.Data->IndexHash.Keys[this.Index];
            }
        }

        /// <summary> Gets the value. </summary>
        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref UnsafeUtility.AsRef<TValue>(this.Data->Values + this.Index);
            }
        }
    }

    [DebuggerDisplay("Key = {Key}, Index = {Indexed1}, Index = {Indexed2}, Value = {Value}")]
    public unsafe struct KIV2<TKey, TIndex1, TIndex2, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex1 : unmanaged, IEquatable<TIndex1>
        where TIndex2 : unmanaged, IEquatable<TIndex2>
        where TValue : unmanaged
    {
        internal DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* Data;
        internal int Index;

        /// <summary> Gets an invalid KeyValue. </summary>
        public static KIV2<TKey, TIndex1, TIndex2, TValue> Null => new() { Index = -1 };

        /// <summary> Gets the key. </summary>
        /// <value> The key. If this KeyValue is Null, returns the default of TKey. </value>
        public TKey Key
        {
            get
            {
                if (this.Index != -1)
                {
                    return this.Data->KeyHash.Keys[this.Index];
                }

                return default;
            }
        }

        /// <summary> Gets the index. </summary>
        public TIndex1 Indexed1
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return this.Data->IndexHash1.Keys[this.Index];
            }
        }

        /// <summary> Gets the index2. </summary>
        public TIndex2 Indexed2
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return this.Data->IndexHash2.Keys[this.Index];
            }
        }

        /// <summary> Gets the value. </summary>
        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref UnsafeUtility.AsRef<TValue>(this.Data->Values + this.Index);
            }
        }
    }
}
