// <copyright file="DynamicIndexed2Map.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicIndexed2MapDebuggerTypeProxy<,,,>))]
    public unsafe struct DynamicIndexed2Map<TKey, TIndex1, TIndex2, TValue> : IEnumerable<KIV2<TKey, TIndex1, TIndex2, TValue>>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex1 : unmanaged, IEquatable<TIndex1>
        where TIndex2 : unmanaged, IEquatable<TIndex2>
        where TValue : unmanaged
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* helper;

        internal DynamicIndexed2Map(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsIndexed2Helper<TKey, TIndex1, TIndex2, TValue>();
        }

        /// <summary> Gets a value indicating whether this hash map has been allocated (and not yet deallocated). </summary>
        /// <value> True if this hash map has been allocated (and not yet deallocated). </value>
        public readonly bool IsCreated => this.buffer.IsCreated;

        /// <summary> Gets a value indicating whether this hash map is empty. </summary>
        /// <value> True if this hash map is empty or if the map has not been constructed. </value>
        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->IsEmpty;
            }
        }

        /// <summary> Gets the current number of key-value pairs in this hash map. </summary>
        /// <returns> The current number of key-value pairs in this hash map. </returns>
        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Count;
            }
        }

        /// <summary> Gets or sets the number of key-value pairs that fit in the current allocation. </summary>
        /// <value> The number of key-value pairs that fit in the current allocation. </value>
        /// <param name="value"> A new capacity. Must be larger than the current capacity. </param>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Capacity;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                this.RefCheck();
                DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* Helper => this.helper;

        /// <summary> Removes all key-value pairs. </summary>
        /// <remarks> Does not change the capacity. </remarks>
        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks> If the key is already present, this method returns false without modifying the hash map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="index1"> The first index to add. </param>
        /// <param name="index2"> The second index to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <returns> True if the key-value pair was added. </returns>
        public bool TryAdd(TKey key, TIndex1 index1, TIndex2 index2, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>.TryAdd(this.buffer, ref this.helper, key, index1, index2, item);
            return idx != -1;
        }

        /// <summary>
        /// Adds a new key-value pair.
        /// </summary>
        /// <remarks> If the key is already present, this method throws without modifying the hash map. </remarks>
        /// <param name="key"> The key to add. </param>
        /// <param name="index1"> The first index to add. </param>
        /// <param name="index2"> The second index to add. </param>
        /// <param name="item"> The value to add. </param>
        /// <exception cref="ArgumentException"> Thrown if the key was already present. </exception>
        public void Add(TKey key, TIndex1 index1, TIndex2 index2, TValue item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>.AddUnique(this.buffer, ref this.helper, key, index1, index2, item);
        }

        /// <summary>
        /// Removes a key-value pair.
        /// </summary>
        /// <param name="key"> The key to remove. </param>
        /// <returns> The number of elements removed. </returns>
        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->Remove(key);
        }

        /// <summary>
        /// Returns true if a given key is present in this hash map.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(key) != -1;
        }

        /// <summary> Removes holes. </summary>
        public void Flatten()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>.Flatten(this.buffer, ref this.helper);
        }

        /// <summary>
        /// Removes a range from the hashmap. This should only be called on a hashmap with no holes and you should know whaty ou're doing.
        /// </summary>
        /// <param name="index"> The index to start. </param>
        /// <param name="range"> The range. </param>
        public void UnsafeRemoveRangeShiftDown(int index, int range)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->RemoveRangeShiftDown(index, range);
        }

        /// <summary>
        /// Returns the value associated with a key.
        /// </summary>
        /// <param name="key"> The key to look up. </param>
        /// <param name="index1"> Outputs the value index1 with the key. Outputs default if the key was not present. </param>v
        /// <param name="index2"> Outputs the value index2 with the key. Outputs default if the key was not present. </param>v
        /// <param name="item"> Outputs the value associated with the key. Outputs default if the key was not present. </param>
        /// <returns> True if the key was present. </returns>
        public readonly bool TryGetValue(TKey key, out TIndex1 index1, out TIndex2 index2, out TValue item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out index1, out index2, out item);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="index1">The index to look up.</param>
        /// <param name="key">>Outputs the unique key associated with the index. Outputs default if the index was not present.</param>
        /// <param name="index2"> Outputs the value index2 with the key. Outputs default if the key was not present. </param>v
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <param name="it"> The iterator to be used for <see cref="TryGetNextValue"/>. </param>
        /// <returns>True if the key was present.</returns>
        public readonly bool TryGetFirstValue(TIndex1 index1, out TKey key, out TIndex2 index2, out TValue item, out HashMapIterator<TIndex1> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetFirstValue(index1, out key, out index2, out item, out it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="key">>Outputs the next key.</param>
        /// <param name="index2">Outputs the index2 value.</param>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public readonly bool TryGetNextValue(out TKey key, out TIndex2 index2, out TValue item, ref HashMapIterator<TIndex1> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetNextValue(out key, out index2, out item, ref it);
        }

        /// <summary> Returns the value associated with a key. </summary>
        /// <param name="index2">The index2 to look up.</param>
        /// <param name="key">>Outputs the unique key associated with the index. Outputs default if the index was not present.</param>
        /// <param name="index1"> Outputs the value index1 with the key. Outputs default if the key was not present. </param>v
        /// <param name="item">Outputs the value associated with the key. Outputs default if the key was not present.</param>
        /// <param name="it"> The iterator to be used for <see cref="TryGetNextValue"/>. </param>
        /// <returns>True if the key was present.</returns>
        public readonly bool TryGetFirstValue(TIndex2 index2, out TKey key, out TIndex1 index1, out TValue item, out HashMapIterator<TIndex2> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetFirstValue(index2, out key, out index1, out item, out it);
        }

        /// <summary> Advances an iterator to the next value associated with its key. </summary>
        /// <param name="key">>Outputs the next key.</param>
        /// <param name="index1">Outputs the index1 value.</param>
        /// <param name="item">Outputs the next value.</param>
        /// <param name="it">A reference to the iterator to advance.</param>
        /// <returns>True if the key was present and had another value.</returns>
        public readonly bool TryGetNextValue(out TKey key, out TIndex1 index1, out TValue item, ref HashMapIterator<TIndex2> it)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetNextValue(out key, out index1, out item, ref it);
        }

        /// <summary>
        /// Returns an enumerator over the key-value pairs of this hash map.
        /// </summary>
        /// <returns> An enumerator over the key-value pairs of this hash map. </returns>
        public readonly DynamicIndexed2MapEnumerator<TKey, TIndex1, TIndex2, TValue> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicIndexed2MapEnumerator<TKey, TIndex1, TIndex2, TValue>(this.helper);
        }

        public TKey* GetUnsafeKeyPtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->KeyHash.Keys;
        }

        public TIndex1* GetUnsafeIndex1Ptr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->IndexHash1.Keys;
        }

        public TIndex2* GetUnsafeIndex2Ptr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->IndexHash2.Keys;
        }

        public TValue* GetUnsafeValuePtr()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Values;
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
        /// </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator<KIV2<TKey, TIndex1, TIndex2, TValue>> IEnumerable<KIV2<TKey, TIndex1, TIndex2, TValue>>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is not implemented. Use <see cref="GetEnumerator" /> instead.
        /// </summary>
        /// <returns> Throws NotImplementedException. </returns>
        /// <exception cref="NotImplementedException"> Method is not implemented. </exception>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            var ptr = this.buffer.GetPtr();
            if (this.helper != ptr)
            {
                throw new ArgumentException("DynamicHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < sizeof(DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>))
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void ThrowKeyNotPresent(TKey key)
        {
            throw new ArgumentException($"Key: {key} is not present.");
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckLengthsMatch(int keys, int values)
        {
            if (keys != values)
            {
                throw new ArgumentException("Key and value array don't match");
            }
        }
    }

    internal sealed unsafe class DynamicIndexed2MapDebuggerTypeProxy<TKey, TIndex1, TIndex2, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex1 : unmanaged, IEquatable<TIndex1>
        where TIndex2 : unmanaged, IEquatable<TIndex2>
        where TValue : unmanaged
    {
        private readonly DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* helper;

        public DynamicIndexed2MapDebuggerTypeProxy(DynamicIndexed2Map<TKey, TIndex1, TIndex2, TValue> target)
        {
            this.helper = target.Helper;
        }

        public List<Quadlet> Items
        {
            get
            {
                var result = new List<Quadlet>();

                if (this.helper == null)
                {
                    return result;
                }

                var kva = this.helper->GetArrays(Allocator.Temp);

                for (var i = 0; i < kva.Keys.Length; ++i)
                {
                    result.Add(new Quadlet(kva.Keys[i], kva.Indices1[i], kva.Indices2[i], kva.Values[i]));
                }

                return result;
            }
        }

        internal readonly struct Quadlet
        {
            public readonly TKey Key;
            public readonly TIndex1 Index1;
            public readonly TIndex2 Index2;
            public readonly TValue Value;

            public Quadlet(TKey k, TIndex1 i1, TIndex2 i2, TValue v)
            {
                this.Key = k;
                this.Index1 = i1;
                this.Index2 = i2;
                this.Value = v;
            }

            public override string ToString()
            {
                return $"{this.Key} : {this.Index1} : {this.Index2} : {this.Value}";
            }
        }
    }
}
