using System;
using System.Collections;
using System.Collections.Generic;
using DroneFlightModule.Core;

namespace DroneFlightModule.Telemetry
{
    /// <summary>
    /// A fixed-capacity buffer that overwrites its oldest item once full. Indexed oldest first;
    /// adding never allocates, so it can sit on the simulation's tick.
    /// </summary>
    public sealed class RingBuffer<T> : IReadOnlyList<T>
    {
        private readonly ArgumentGuard guard = new ArgumentGuard();
        private readonly T[] items;
        private int start;

        public RingBuffer(int capacity)
        {
            items = new T[guard.RequirePositive(capacity, nameof(capacity))];
        }

        public int Capacity => items.Length;
        public int Count { get; private set; }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, $"Must be below the count of {Count}.");
                }
                return items[(start + index) % items.Length];
            }
        }

        public void Add(T item)
        {
            if (Count < items.Length)
            {
                items[(start + Count) % items.Length] = item;
                Count++;
                return;
            }
            items[start] = item;
            start = (start + 1) % items.Length;
        }

        public IEnumerator<T> GetEnumerator()
        {
            for (int i = 0; i < Count; i++)
            {
                yield return this[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
