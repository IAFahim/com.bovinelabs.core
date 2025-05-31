// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// Wrapper for the ID of an object. This can be used to store weak references to entities
    /// that can be instantiated at runtime via <see cref="ObjectDefinitionRegistry" />.
    /// </summary>
    [Serializable]
    public readonly struct ObjectId : IComponentData, IEquatable<ObjectId>, IComparable<ObjectId>
    {
        private const int ModShift = 24;
        private const int IDMask = (1 << 24) - 1;

        [CreateProperty(ReadOnly = true)]
        private readonly int rawValue;

        public ObjectId(int id, byte mod = 0)
        {
#if UNITY_EDITOR
            if (id > IDMask)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id too large");
            }
#endif

            this.rawValue = mod << ModShift | id;
        }

        [CreateProperty]
        public byte Mod => (byte)(this.rawValue >> ModShift);

        [CreateProperty]
        public int ID => this.rawValue & IDMask;

        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.ID == right.ID;
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return left.ID != right.ID;
        }

        public int CompareTo(ObjectId other)
        {
            return this.rawValue.CompareTo(other.rawValue);
        }

        public override string ToString()
        {
            return $"ID:{this.ID}";
        }

        public FixedString32Bytes ToFixedString()
        {
            return $"ID:{this.ID}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            throw new SystemException("Not supported");
        }

        /// <inheritdoc />
        public bool Equals(ObjectId other)
        {
            return this.rawValue == other.rawValue;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.rawValue;
        }
    }
}
#endif
