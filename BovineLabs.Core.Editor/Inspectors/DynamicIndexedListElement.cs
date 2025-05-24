// <copyright file="DynamicIndexedListElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Collections;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    public class DynamicIndexedListElement<TBuffer, TKey, TIndex, TValue> : DynamicListElement<TBuffer, DynamicIndexedListElement<TBuffer, TKey, TIndex, TValue>.KIV>
        where TBuffer : unmanaged, IDynamicIndexedMap<TKey, TIndex, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        private readonly IntegerField bufferSize;

        public DynamicIndexedListElement(object inspector, int refreshRate = 250)
            : base(inspector, refreshRate)
        {
            this.bufferSize = new IntegerField("Buffer Size") { isReadOnly = true };

            this.bufferSize.SetEnabled(false);

            this.Insert(0, this.bufferSize);
            StylingUtility.AlignInspectorLabelWidth(this.bufferSize);

            this.OnRefresh();
        }

        private DynamicIndexedMap<TKey, TIndex, TValue> GetMap => this.Context.EntityManager.GetBuffer<TBuffer>(this.Context.Entity, true).AsIndexedMap<TBuffer, TKey, TIndex, TValue>();

        public override bool IsValid()
        {
            return base.IsValid() && this.Context.EntityManager.HasBuffer<TBuffer>(this.Context.Entity);
        }

        protected override void PopulateList(List<KIV> list)
        {
            var map = this.GetMap;

            using var e = map.GetEnumerator();
            while (e.MoveNext())
            {
                list.Add(new KIV(e.Current));
            }
        }

        protected sealed override void OnRefresh()
        {
            this.bufferSize.value = this.Context.EntityManager.GetBuffer<TBuffer>(this.Context.Entity, true).Length;
        }

        protected override void OnValueChanged(NativeArray<KIV> newValues)
        {
        }

        public struct KIV
        {
            [UsedImplicitly]
            public TKey Key;

            [UsedImplicitly]
            public TIndex Index;

            [UsedImplicitly]
            public TValue Value;

            public KIV(KIV<TKey, TIndex, TValue> kvp)
            {
                this.Key = kvp.Key;
                this.Index = kvp.Indexed;
                this.Value = kvp.Value;
            }
        }
    }
}
