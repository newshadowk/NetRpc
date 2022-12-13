namespace NetRpc.Contract
{
    public struct Field<T>
    {
        private readonly bool hasValue; 

        private T value;

        public Field(T value)
        {
            this.value = value;
            hasValue = true;
        }

        public readonly bool HasValue => hasValue;

        public readonly T Value
        {
            get
            {
                if (!hasValue)
                {
                    //ThrowHelper.ThrowInvalidOperationException_InvalidOperation_NoValue();
                }
                return value;
            }
        }

        public readonly T GetValueOrDefault() => value;

        public readonly T GetValueOrDefault(T defaultValue) =>
            hasValue ? value : defaultValue;

        public override bool Equals(object? other)
        {
            if (!hasValue) return other == null;
            if (other == null) return false;
            if (value == null) return false;
            return value.Equals(other);
        }

        public override int GetHashCode() => hasValue && value != null ? value.GetHashCode() : 0;

        public override string? ToString() => hasValue && value != null ? value.ToString() : "";

        public static implicit operator Field<T>(T value) => new(value);

        public static explicit operator T(Field<T> value) => value.Value;
    }

    public partial struct Nullable2<T> where T : struct
    {
        private readonly bool hasValue; // Do not rename (binary serialization)
        internal T value; // Do not rename (binary serialization) or make readonly (can be mutated in ToString, etc.)

        public Nullable2(T value)
        {
            this.value = value;
            hasValue = true;
        }

        public readonly bool HasValue
        {
            get => hasValue;
        }

        public readonly T Value
        {
            get
            {
                if (!hasValue)
                {
                }
                return value;
            }
        }

        public readonly T GetValueOrDefault() => value;

        public readonly T GetValueOrDefault(T defaultValue) =>
            hasValue ? value : defaultValue;

        public override bool Equals(object? other)
        {
            if (!hasValue) return other == null;
            if (other == null) return false;
            return value.Equals(other);
        }

        public override int GetHashCode() => hasValue ? value.GetHashCode() : 0;

        public override string? ToString() => hasValue ? value.ToString() : "";

        public static implicit operator Nullable2<T>(T value) =>
            new Nullable2<T>(value);

        public static explicit operator T(Nullable2<T> value) => value!.Value;
    }
}
