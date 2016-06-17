using System;

namespace Seq.Forwarder.Importer
{
    class Line<T>
    {
        public int Number { get; }
        public T Value { get; }
        
        public Line(int number, T value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            Number = number;
            Value = value;
        }

        public Line<U> MappedTo<U>(U value)
        {
            return new Line<U>(Number, value);
        }
    }
}