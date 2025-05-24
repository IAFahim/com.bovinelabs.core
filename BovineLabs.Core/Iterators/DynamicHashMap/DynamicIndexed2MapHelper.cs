// <copyright file="DynamicIndexed2MapHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe ref struct DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex1 : unmanaged, IEquatable<TIndex1>
        where TIndex2 : unmanaged, IEquatable<TIndex2>
        where TValue : unmanaged
    {
        internal int ValuesOffset;

        internal HashHelper<TKey> KeyHash;
        internal HashHelper<TIndex1> IndexHash1;
        internal HashHelper<TIndex2> IndexHash2;

        internal int Count;
        internal int Capacity;
        internal int BucketCapacityMask; // = bucket capacity - 1
        internal int Log2MinGrowth;
        internal int AllocatedIndex;
        internal int FirstFreeIdx;

        internal int BucketCapacity => this.BucketCapacityMask + 1;

        internal TValue* Values
        {
            get
            {
                fixed (DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data = &this)
                {
                    return (TValue*)((byte*)data + data->ValuesOffset);
                }
            }
        }

        internal readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this.Count == 0;
        }

        internal static void Init(DynamicBuffer<byte> buffer, int capacity, int minGrowth)
        {
            Check.Assume(buffer.Length == 0, "Buffer already assigned");

            var log2MinGrowth = (byte)(32 - math.lzcnt(math.max(1, minGrowth) - 1));
            capacity = CalcCapacityCeilPow2(0, capacity, log2MinGrowth);

            var bucketCapacity = GetBucketSize(capacity);
            var totalSize = CalculateDataSize(capacity, bucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var index1Offset, out var index1NextOffset, out var index1BucketOffset,
                out var index2Offset, out var index2NextOffset, out var index2BucketOffset);

            var hashMapDataSize = sizeof(DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>);
            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            var data = buffer.AsIndexed2Helper<TKey, TIndex1, TIndex2, TValue>();

            data->Log2MinGrowth = log2MinGrowth;
            data->Capacity = capacity;
            data->BucketCapacityMask = bucketCapacity - 1;

            data->ValuesOffset = hashMapDataSize;
            data->KeyHash = new HashHelper<TKey>((byte*)data, &data->KeyHash, hashMapDataSize, keyOffset, nextOffset, bucketOffset);
            data->IndexHash1 = new HashHelper<TIndex1>((byte*)data, &data->IndexHash1, hashMapDataSize, index1Offset, index1NextOffset, index1BucketOffset);
            data->IndexHash2 = new HashHelper<TIndex2>((byte*)data, &data->IndexHash2, hashMapDataSize, index2Offset, index2NextOffset, index2BucketOffset);

            data->Clear(); // also sets FirstFreeIdx, Count, AllocatedIndex
        }

        internal static void Resize(DynamicBuffer<byte> buffer, ref DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data, int newCapacity)
        {
            newCapacity = math.max(newCapacity, data->Count);
            var newBucketCapacity = math.ceilpow2(GetBucketSize(newCapacity));

            if (data->Capacity == newCapacity && data->BucketCapacity == newBucketCapacity)
            {
                return;
            }

            ResizeExact(buffer, ref data, newCapacity, newBucketCapacity);
        }

        internal static void ResizeExact(
            DynamicBuffer<byte> buffer, ref DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data, int newCapacity, int newBucketCapacity)
        {
            var totalSize = CalculateDataSize(newCapacity, newBucketCapacity, out var keyOffset, out var nextOffset, out var bucketOffset,
                out var index1Offset, out var index1NextOffset, out var index1BucketOffset,
                out var index2Offset, out var index2NextOffset, out var index2BucketOffset);

            var oldValue = (TValue*)UnsafeUtility.Malloc(data->Capacity * sizeof(TValue), UnsafeUtility.AlignOf<TValue>(), Allocator.Temp);
            UnsafeUtility.MemCpy(oldValue, data->Values, data->Capacity * sizeof(TValue));

            var kHelper = new HashHelper<TKey>.Resize(ref data->KeyHash, data->Capacity, data->BucketCapacity);
            var iHelper1 = new HashHelper<TIndex1>.Resize(ref data->IndexHash1, data->Capacity, data->BucketCapacity);
            var iHelper2 = new HashHelper<TIndex2>.Resize(ref data->IndexHash2, data->Capacity, data->BucketCapacity);

            var oldCapacity = data->Capacity;
            var oldBucketCapacity = data->BucketCapacity;
            var oldCount = data->Count;
            var oldFirstFreeIdx = data->FirstFreeIdx;
            var oldAllocatedIndex = data->AllocatedIndex;

            var oldLog2MinGrowth = data->Log2MinGrowth;
            var hashMapDataSize = sizeof(DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>);

            buffer.ResizeUninitialized(hashMapDataSize + totalSize);

            data = buffer.AsIndexed2Helper<TKey, TIndex1, TIndex2, TValue>();
            data->Capacity = newCapacity;
            data->BucketCapacityMask = newBucketCapacity - 1;
            data->Log2MinGrowth = oldLog2MinGrowth;

            data->ValuesOffset = hashMapDataSize;
            data->KeyHash = new HashHelper<TKey>((byte*)data, &data->KeyHash, hashMapDataSize, keyOffset, nextOffset, bucketOffset);
            data->IndexHash1 = new HashHelper<TIndex1>((byte*)data, &data->IndexHash1, hashMapDataSize, index1Offset, index1NextOffset, index1BucketOffset);
            data->IndexHash2 = new HashHelper<TIndex2>((byte*)data, &data->IndexHash2, hashMapDataSize, index2Offset, index2NextOffset, index2BucketOffset);

            if (newCapacity > oldCapacity)
            {
                data->Count = oldCount;
                data->FirstFreeIdx = oldFirstFreeIdx;
                data->AllocatedIndex = oldAllocatedIndex;

                UnsafeUtility.MemCpy(data->Values, oldValue, oldCapacity * sizeof(TValue));

                kHelper.Increase(ref data->KeyHash, newCapacity, newBucketCapacity);
                iHelper1.Increase(ref data->IndexHash1, newCapacity, newBucketCapacity);
                iHelper2.Increase(ref data->IndexHash2, newCapacity, newBucketCapacity);

                if (data->AllocatedIndex > data->Capacity)
                {
                    data->AllocatedIndex = data->Capacity;
                }
            }
            else
            {
                data->Clear();

                for (var i = 0; i < oldBucketCapacity; ++i)
                {
                    for (var idx = kHelper.OldBuckets[i]; idx != -1; idx = kHelper.OldNext[idx])
                    {
                        AddNoCollideNoAlloc(data, kHelper.OldKeys[idx], iHelper1.OldKeys[idx], iHelper2.OldKeys[idx], oldValue[idx]);
                    }
                }
            }
        }

        internal static int TryAdd(
            DynamicBuffer<byte> buffer, ref DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data, in TKey key, in TIndex1 index1, in TIndex2 index2, in TValue value)
        {
            if (data->Find(key) == -1)
            {
                return AddInternal(buffer, ref data, key, index1, index2, value);
            }

            return -1;
        }

        internal static int AddUnique(
            DynamicBuffer<byte> buffer, ref DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data, in TKey key, in TIndex1 index1, in TIndex2 index2, in TValue value)
        {
            data->CheckDoesNotExist(key);
            return AddInternal(buffer, ref data, key, index1, index2, value);
        }

        internal static void Flatten(DynamicBuffer<byte> buffer, ref DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data)
        {
            var capacity = CalcCapacityCeilPow2(data->Count, data->Count, data->Log2MinGrowth);
            ResizeExact(buffer, ref data, capacity, GetBucketSize(capacity));
        }

        private static int AddInternal(
            DynamicBuffer<byte> buffer, ref DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data, TKey key, TIndex1 index1, TIndex2 index2, TValue value)
        {
            // Allocate an entry from the free list
            if (data->AllocatedIndex >= data->Capacity && data->FirstFreeIdx < 0)
            {
                var newCap = CalcCapacityCeilPow2(data->Count, data->Capacity + (1 << data->Log2MinGrowth), data->Log2MinGrowth);
                Resize(buffer, ref data, newCap);
            }

            return AddNoCollideNoAlloc(data, key, index1, index2, value);
        }

        private static int AddNoCollideNoAlloc(
            DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data, in TKey key, in TIndex1 index1, in TIndex2 index2, in TValue value)
        {
            Check.Assume(data->AllocatedIndex < data->Capacity || data->FirstFreeIdx >= 0);

            var idx = data->FirstFreeIdx;

            if (idx >= 0)
            {
                data->FirstFreeIdx = data->KeyHash.Next[idx];
            }
            else
            {
                idx = data->AllocatedIndex++;
            }

            data->CheckIndexOutOfBounds(idx);

            UnsafeUtility.WriteArrayElement(data->KeyHash.Keys, idx, key);
            UnsafeUtility.WriteArrayElement(data->IndexHash1.Keys, idx, index1);
            UnsafeUtility.WriteArrayElement(data->IndexHash2.Keys, idx, index2);
            UnsafeUtility.WriteArrayElement(data->Values, idx, value);

            // Add the key to the hash-map
            var keyBucket = HashHelper<TKey>.GetBucket(key, data->BucketCapacityMask);
            var keyNext = data->KeyHash.Next;
            keyNext[idx] = data->KeyHash.Buckets[keyBucket];
            data->KeyHash.Buckets[keyBucket] = idx;

            // Add the index to the hash-map
            var index1Bucket = HashHelper<TIndex1>.GetBucket(index1, data->BucketCapacityMask);
            var index2Bucket = HashHelper<TIndex2>.GetBucket(index2, data->BucketCapacityMask);

            var index1Next = data->IndexHash1.Next;
            index1Next[idx] = data->IndexHash1.Buckets[index1Bucket];
            data->IndexHash1.Buckets[index1Bucket] = idx;

            var index2Next = data->IndexHash2.Next;
            index2Next[idx] = data->IndexHash2.Buckets[index2Bucket];
            data->IndexHash2.Buckets[index2Bucket] = idx;

            data->Count++;
            return idx;
        }

        internal void Clear()
        {
            this.KeyHash.Clear(this.Capacity, this.BucketCapacity);
            this.IndexHash1.Clear(this.Capacity, this.BucketCapacity);
            this.IndexHash2.Clear(this.Capacity, this.BucketCapacity);

            this.Count = 0;
            this.FirstFreeIdx = -1;
            this.AllocatedIndex = 0;
        }

        internal int Find(TKey key)
        {
            if (this.AllocatedIndex > 0)
            {
                return this.KeyHash.Find(key, this.Capacity, this.BucketCapacityMask);
            }

            return -1;
        }

        internal bool Remove(TKey key)
        {
            if (this.Capacity == 0)
            {
                return false;
            }

            // First find the slot based on the hash
            var bucket = HashHelper<TKey>.GetBucket(key, this.BucketCapacityMask);

            var prevEntry = -1;
            var entryIdx = this.KeyHash.Buckets[bucket];

            while (entryIdx >= 0 && entryIdx < this.Capacity)
            {
                if (UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, entryIdx).Equals(key))
                {
                    // Found matching element, remove it
                    if (prevEntry < 0)
                    {
                        this.KeyHash.Buckets[bucket] = this.KeyHash.Next[entryIdx];
                    }
                    else
                    {
                        this.KeyHash.Next[prevEntry] = this.KeyHash.Next[entryIdx];
                    }

                    // And free the index
                    this.KeyHash.Next[entryIdx] = this.FirstFreeIdx;
                    this.FirstFreeIdx = entryIdx;

                    this.IndexHash1.RemoveIndex(entryIdx, this.BucketCapacityMask);
                    this.IndexHash2.RemoveIndex(entryIdx, this.BucketCapacityMask);

                    this.Count--;
                    return true;
                }

                prevEntry = entryIdx;
                entryIdx = this.KeyHash.Next[entryIdx];
            }

            return false;
        }

        internal bool TryGetValue(TKey key, out TIndex1 index1, out TIndex2 index2, out TValue item)
        {
            var idx = this.Find(key);

            if (idx != -1)
            {
                index1 = UnsafeUtility.ReadArrayElement<TIndex1>(this.IndexHash1.Keys, idx);
                index2 = UnsafeUtility.ReadArrayElement<TIndex2>(this.IndexHash2.Keys, idx);
                item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, idx);
                return true;
            }

            index1 = default;
            index2 = default;
            item = default;
            return false;
        }

        internal bool TryGetFirstValue(TIndex1 index1, out TKey key, out TIndex2 index2, out TValue item, out HashMapIterator<TIndex1> it)
        {
            it.Key = index1;

            if (this.AllocatedIndex <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                key = default;
                index2 = default;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = HashHelper<TIndex1>.GetBucket(it.Key, this.BucketCapacityMask);
            it.EntryIndex = it.NextEntryIndex = this.IndexHash1.Buckets[bucket];

            return this.TryGetNextValue(out key, out index2, out item, ref it);
        }

        internal bool TryGetNextValue(out TKey key, out TIndex2 index2, out TValue item, ref HashMapIterator<TIndex1> it)
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= this.Capacity)
            {
                key = default;
                index2 = default;
                item = default;
                return false;
            }

            var next = this.IndexHash1.Next;
            var keys = this.IndexHash1.Keys;

            while (!UnsafeUtility.ReadArrayElement<TIndex1>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)this.Capacity)
                {
                    key = default;
                    index2 = default;
                    item = default;
                    return false;
                }
            }

            it.NextEntryIndex = next[entryIdx];
            it.EntryIndex = entryIdx;
            key = UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, entryIdx);
            index2 = UnsafeUtility.ReadArrayElement<TIndex2>(this.IndexHash2.Keys, entryIdx);
            item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, entryIdx);
            return true;
        }

        internal bool TryGetFirstValue(TIndex2 index2, out TKey key, out TIndex1 index1, out TValue item, out HashMapIterator<TIndex2> it)
        {
            it.Key = index2;

            if (this.AllocatedIndex <= 0)
            {
                it.EntryIndex = it.NextEntryIndex = -1;
                key = default;
                index1 = default;
                item = default;
                return false;
            }

            // First find the slot based on the hash
            var bucket = HashHelper<TIndex2>.GetBucket(it.Key, this.BucketCapacityMask);
            it.EntryIndex = it.NextEntryIndex = this.IndexHash1.Buckets[bucket];

            return this.TryGetNextValue(out key, out index1, out item, ref it);
        }

        internal bool TryGetNextValue(out TKey key, out TIndex1 index1, out TValue item, ref HashMapIterator<TIndex2> it)
        {
            var entryIdx = it.NextEntryIndex;
            it.NextEntryIndex = -1;
            it.EntryIndex = -1;

            if (entryIdx < 0 || entryIdx >= this.Capacity)
            {
                key = default;
                index1 = default;
                item = default;
                return false;
            }

            var next = this.IndexHash2.Next;
            var keys = this.IndexHash2.Keys;

            while (!UnsafeUtility.ReadArrayElement<TIndex2>(keys, entryIdx).Equals(it.Key))
            {
                entryIdx = next[entryIdx];
                if ((uint)entryIdx >= (uint)this.Capacity)
                {
                    key = default;
                    index1 = default;
                    item = default;
                    return false;
                }
            }

            it.NextEntryIndex = next[entryIdx];
            it.EntryIndex = entryIdx;
            key = UnsafeUtility.ReadArrayElement<TKey>(this.KeyHash.Keys, entryIdx);
            index1 = UnsafeUtility.ReadArrayElement<TIndex1>(this.IndexHash1.Keys, entryIdx);
            item = UnsafeUtility.ReadArrayElement<TValue>(this.Values, entryIdx);
            return true;
        }

        internal void RemoveRangeShiftDown(int start, int length)
        {
            if (length == 0)
            {
                return;
            }

            Check.Assume(this.FirstFreeIdx == -1, "Trying to RemoveRangeShiftDown on map with holes. Call Flatten() first.");
            Check.Assume(start >= 0 && start < this.Count);
            Check.Assume(length >= 0 && start + length <= this.Count);

            var keys = this.KeyHash.Keys;
            var indices1 = this.IndexHash1.Keys;
            var indices2 = this.IndexHash2.Keys;
            var values = this.Values;

            var shift = this.Count - length - start;

            // var shift = count - le
            UnsafeUtility.MemMove(keys + start, keys + start + length, sizeof(TKey) * shift);
            UnsafeUtility.MemMove(indices1 + start, indices1 + start + length, sizeof(TIndex1) * shift);
            UnsafeUtility.MemMove(indices2 + start, indices2 + start + length, sizeof(TIndex2) * shift);
            UnsafeUtility.MemMove(values + start, values + (start + length), shift * sizeof(TValue));

            UnsafeUtility.MemSet(this.KeyHash.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((this.KeyHash.Next + this.Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            UnsafeUtility.MemSet(this.IndexHash1.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((this.IndexHash1.Next + this.Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            UnsafeUtility.MemSet(this.IndexHash2.Buckets, 0xff, this.BucketCapacity * sizeof(int));
            UnsafeUtility.MemSet((this.IndexHash2.Next + this.Count) - length, 0xff, length * sizeof(int)); // only need to clear replaced elements

            this.AllocatedIndex -= length;
            this.Count -= length;

            var keyBuckets = this.KeyHash.Buckets;
            var keyNext = this.KeyHash.Next;

            for (var idx = 0; idx < this.Count; idx++)
            {
                var bucket = keys[idx].GetHashCode() & this.BucketCapacityMask;
                keyNext[idx] = keyBuckets[bucket];
                keyBuckets[bucket] = idx;
            }

            var index1Buckets = this.IndexHash1.Buckets;
            var index1Next = this.IndexHash1.Next;

            for (var idx = 0; idx < this.Count; idx++)
            {
                var bucket = indices1[idx].GetHashCode() & this.BucketCapacityMask;
                index1Next[idx] = index1Buckets[bucket];
                index1Buckets[bucket] = idx;
            }

            var index2Buckets = this.IndexHash2.Buckets;
            var index2Next = this.IndexHash2.Next;

            for (var idx = 0; idx < this.Count; idx++)
            {
                var bucket = indices2[idx].GetHashCode() & this.BucketCapacityMask;
                index2Next[idx] = index2Buckets[bucket];
                index2Buckets[bucket] = idx;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int CalcCapacityCeilPow2(int count, int capacity, int log2MinGrowth)
        {
            capacity = math.max(math.max(1, count), capacity);
            var newCapacity = math.max(capacity, 1 << log2MinGrowth);
            var result = math.ceilpow2(newCapacity);

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetBucketSize(int capacity)
        {
            return capacity * 2;
        }

        private static int CalculateDataSize(
            int capacity, int bucketCapacity, out int outKeyOffset, out int outNextOffset, out int outBucketOffset,
            out int outIndex1Offset, out int outIndex1NextOffset, out int outIndex1BucketOffset,
            out int outIndex2Offset, out int outIndex2NextOffset, out int outIndex2BucketOffset)
        {
            var sizeOfTKey = sizeof(TKey);
            var sizeOfTIndex1 = sizeof(TIndex1);
            var sizeOfTIndex2 = sizeof(TIndex2);
            var sizeOfTValue = sizeof(TValue);
            const int sizeOfInt = sizeof(int);

            var valuesSize = sizeOfTValue * capacity;

            var keysSize = sizeOfTKey * capacity;
            var nextSize = sizeOfInt * capacity;
            var bucketSize = sizeOfInt * bucketCapacity;

            var index1Size = sizeOfTIndex1 * capacity;
            var index1NextSize = sizeOfInt * capacity;
            var index1BucketSize = sizeOfInt * bucketCapacity;

            var index2Size = sizeOfTIndex2 * capacity;
            var index2NextSize = sizeOfInt * capacity;
            var index2BucketSize = sizeOfInt * bucketCapacity;

            var totalSize = valuesSize + keysSize + nextSize + bucketSize +
                index1Size + index1NextSize + index1BucketSize +
                index2Size + index2NextSize + index2BucketSize;

            outKeyOffset = valuesSize;
            outNextOffset = outKeyOffset + keysSize;
            outBucketOffset = outNextOffset + nextSize;
            outIndex1Offset = outBucketOffset + bucketSize;
            outIndex1NextOffset = outIndex1Offset + index1Size;
            outIndex1BucketOffset = outIndex1NextOffset + index1NextSize;

            outIndex2Offset = outIndex1BucketOffset + index1BucketSize;
            outIndex2NextOffset = outIndex2Offset + index2Size;
            outIndex2BucketOffset = outIndex2NextOffset + index2NextSize;

            return totalSize;
        }

        internal (NativeArray<TKey> Keys, NativeArray<TIndex1> Indices1, NativeArray<TIndex2> Indices2, NativeArray<TValue> Values) GetArrays(AllocatorManager.AllocatorHandle allocator)
        {
            var keyOutput = CollectionHelper.CreateNativeArray<TKey>(this.Count, allocator);
            var indexOutput1 = CollectionHelper.CreateNativeArray<TIndex1>(this.Count, allocator);
            var indexOutput2 = CollectionHelper.CreateNativeArray<TIndex2>(this.Count, allocator);
            var valueOutput = CollectionHelper.CreateNativeArray<TValue>(this.Count, allocator);

            var values = this.Values;
            var keys = this.KeyHash.Keys;
            var buckets = this.KeyHash.Buckets;
            var next = this.KeyHash.Next;

            var indices1 = this.IndexHash1.Keys;
            var indices2 = this.IndexHash2.Keys;

            for (int i = 0, count = 0, max = this.Count, capacity = this.BucketCapacity; i < capacity && count < max; ++i)
            {
                var bucket = buckets[i];

                while (bucket != -1)
                {
                    keyOutput[count] = UnsafeUtility.ReadArrayElement<TKey>(keys, bucket);
                    indexOutput1[count] = UnsafeUtility.ReadArrayElement<TIndex1>(indices1, bucket);
                    indexOutput2[count] = UnsafeUtility.ReadArrayElement<TIndex2>(indices2, bucket);
                    valueOutput[count] = UnsafeUtility.ReadArrayElement<TValue>(values, bucket);
                    count++;
                    bucket = next[bucket];
                }
            }

            return (keyOutput, indexOutput1, indexOutput2, valueOutput);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckDoesNotExist(TKey key)
        {
            if (this.Find(key) != -1)
            {
                throw new ArgumentException($"An item with the same key has already been added: {key}");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckIndexOutOfBounds(int idx)
        {
            if ((uint)idx >= (uint)this.Capacity)
            {
                throw new InvalidOperationException($"Internal HashMap error. idx {idx}");
            }
        }

        internal struct Enumerator
        {
            [NativeDisableUnsafePtrRestriction]
            internal DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* Data;
            internal int Index;
            internal int BucketIndex;
            internal int NextIndex;

            internal Enumerator(DynamicIndexed2MapHelper<TKey, TIndex1, TIndex2, TValue>* data)
            {
                this.Data = data;
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal bool MoveNext()
            {
                var next = this.Data->KeyHash.Next;

                if (this.NextIndex != -1)
                {
                    this.Index = this.NextIndex;
                    this.NextIndex = next[this.NextIndex];
                    return true;
                }

                var buckets = this.Data->KeyHash.Buckets;

                for (int i = this.BucketIndex, num = this.Data->BucketCapacity; i < num; ++i)
                {
                    var idx = buckets[i];

                    if (idx != -1)
                    {
                        this.Index = idx;
                        this.BucketIndex = i + 1;
                        this.NextIndex = next[idx];

                        return true;
                    }
                }

                this.Index = -1;
                this.BucketIndex = this.Data->BucketCapacity;
                this.NextIndex = -1;
                return false;
            }

            internal void Reset()
            {
                this.Index = -1;
                this.BucketIndex = 0;
                this.NextIndex = -1;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal KIV2<TKey, TIndex1, TIndex2, TValue> GetCurrent()
            {
                return new KIV2<TKey, TIndex1, TIndex2, TValue>
                {
                    Data = this.Data,
                    Index = this.Index,
                };
            }
        }
    }
}
