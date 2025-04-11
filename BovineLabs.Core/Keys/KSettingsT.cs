// <copyright file="KSettingsT.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Mathematics;
    using UnityEngine;

    /// <summary> Generic implementation of <see cref="KSettings" /> to allow calling the generic <see cref="K{T}" />. </summary>
    /// <typeparam name="T"> Itself. </typeparam>
    public abstract class KSettings<T> : KSettings
        where T : KSettings<T>
    {
        [SerializeField]
        private NameValue[] keys = Array.Empty<NameValue>();

        public override IReadOnlyList<NameValue> Keys => this.keys;

        /// <inheritdoc />
        protected sealed override void Initialize()
        {
            K<T>.Initialize(this.keys);
        }

        protected virtual IEnumerable<NameValue> SetReset()
        {
            return Enumerable.Empty<NameValue>();
        }

#if UNITY_EDITOR
        protected void OnValidate()
        {
            this.Validate(ref this.keys);
        }

        protected virtual void Validate(ref NameValue[] keyRef)
        {
            ValidateLength(ref keyRef);

            for (var i = 0; i < keyRef.Length; i++)
            {
                var k = keyRef[i];
                k.Name = k.Name.ToLower();
                k.Value = math.clamp(k.Value, 0, KMap.MaxCapacity - 1);
                keyRef[i] = k;
            }
        }

        private void Reset()
        {
            this.keys = this.SetReset().ToArray();
        }
#endif
    }
}
