﻿// <copyright file="EntityBlob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Blobs
{
    using BovineLabs.Core.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Properties;

    public struct EntityBlob : IComponentData
    {
        internal BlobAssetReference<BlobPerfectHashMap<int, int>> Value;

        [CreateProperty]
        public int MapCapacity => this.Value.IsCreated ? this.Value.Value.Capacity : 0;

        [CreateProperty]
        public unsafe int BlobSize => this.Value.IsCreated ? this.Value.m_data.Header->Length : 0;

        public unsafe bool TryGet<T>(int key, out BlobAssetReference<T> blobAssetReference)
            where T : unmanaged
        {
            if (!this.Value.Value.TryGetValue(key, out var offsetPtr))
            {
                blobAssetReference = default;
                return false;
            }

            ref var offset = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref offsetPtr.Ref);

            var header = (BlobAssetHeader*)offset.GetUnsafePtr();
            var blobPtr = (byte*)(header + 1);
            header->ValidationPtr = blobPtr;

            blobAssetReference = new BlobAssetReference<T> { m_data = new BlobAssetReferenceData { m_Ptr = blobPtr } };

            return true;
        }
    }
}
