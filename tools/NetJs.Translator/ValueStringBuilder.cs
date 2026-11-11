namespace NetJs.Translator
{
    public ref struct ValueStringBuilder
    {
        private Span<char> _chars;
        private int _pos;

        public ValueStringBuilder(Span<char> initialBuffer)
        {
            _chars = initialBuffer;
            _pos = 0;
        }

        public int Length => _pos;

        public void Append(string? value)
        {
            if (value == null) return;

            int len = value.Length;
            // Simple grow logic: In production, you would rent from ArrayPool if it overflows stackalloc bounds
            if (_pos > _chars.Length - len)
            {
                throw new InvalidOperationException("Stack buffer capacity exceeded!");
            }

            value.AsSpan().CopyTo(_chars.Slice(_pos));
            _pos += len;
        }

        public void Append(string? value, int offsetStart, int count)
        {
            if (value == null || count <= 0) return;

            if (offsetStart < 0 || offsetStart + count > value.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(offsetStart), "Index and count must refer to a location within the string.");
            }

            // Verify the stack buffer has enough remaining space
            if (_pos > _chars.Length - count)
            {
                throw new InvalidOperationException("Stack buffer capacity exceeded!");
            }

            value.AsSpan(offsetStart, count).CopyTo(_chars.Slice(_pos));
            _pos += count;
        }

        public void Append(int value)
        {
            if (value.TryFormat(_chars.Slice(_pos), out int charsWritten))
            {
                _pos += charsWritten;
            }
            else
            {
                throw new InvalidOperationException("Stack buffer capacity exceeded!");
            }
        }

        public override string ToString() => _chars.Slice(0, _pos).ToString();
    }

}

