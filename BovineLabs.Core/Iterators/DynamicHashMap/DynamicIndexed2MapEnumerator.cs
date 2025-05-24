// <copyright file="DynamicIndexedMapEnumerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// An enumerator over the key-value pairs of a container.
    /// </summary>
    /// <remarks>
    /// In an enumerator's initial state, <see cref="Current" /> is not valid to read.
    /// From this state, the first <see cref="MoveNext" /> call advances the enumerator to the first key-value pair.
    /// </remarks>
    [NativeContainer]
    [NativeContainerIsReadOnly]
    public struct DynamicIndexed2MapEnumerator<TKey, TIndex1, TIndex2, TValue> : IEnumerator<KIV2<TKey, TIndex1, TIndex2, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex1 : unmanaged, IEquatable<TIndex1>
        where TIndex2 : unmanaged, IEquatable<TIndex2>
        where TValue : unmanaged
    {
        [NativeDisableUnsafePtrRestriction]
        private DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>.Enumerator enumerator;

        internal unsafe DynamicIndexed2MapEnumerator(DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data)
        {
            this.enumerator = new DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>.Enumerator(data);
        }

        /// <summary> The current key-value pair. </summary>
        public KIV2<TKey, TIndex1, TIndex2, TValue> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.enumerator.GetCurrent();
        }

        /// <summary> Gets the element at the current position of the enumerator in the container. </summary>
        object IEnumerator.Current => this.Current;

        /// <summary> Advances the enumerator to the next key-value pair. </summary>
        /// <returns> True if <see cref="Current" /> is valid to read after the call. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        /// <summary> Resets the enumerator to its initial state. </summary>
        public void Reset()
        {
            this.enumerator.Reset();
        }

        /// <summary> Does nothing. </summary>
        public void Dispose()
        {
        }
    }
}
