using System;
using Unity.Entities;

namespace DOTS.Dispatcher.Tests.Editor
{
    internal readonly struct ValueTestData : IComponentData, IEquatable<ValueTestData>
    {
        public readonly int Value;

        public ValueTestData(int value)
        {
            Value = value;
        }

        public bool Equals(ValueTestData other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is ValueTestData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value;
        }

        public static bool operator ==(ValueTestData left, ValueTestData right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ValueTestData left, ValueTestData right)
        {
            return !left.Equals(right);
        }
    }
}