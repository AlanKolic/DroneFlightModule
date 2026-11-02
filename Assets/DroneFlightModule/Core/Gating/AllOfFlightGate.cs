using System;
using System.Collections.Generic;

namespace DroneFlightModule.Core
{
    /// <summary>
    /// Allows flight only while every inner gate does.
    /// </summary>
    public sealed class AllOfFlightGate : IFlightGate
    {
        private readonly IFlightGate[] gates;

        public AllOfFlightGate(IReadOnlyList<IFlightGate> gates)
        {
            if (gates == null)
            {
                throw new ArgumentNullException(nameof(gates));
            }
            if (gates.Count == 0)
            {
                throw new ArgumentException("At least one gate is required.", nameof(gates));
            }

            this.gates = new IFlightGate[gates.Count];
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i] == null)
                {
                    throw new ArgumentException("Gates cannot be null.", nameof(gates));
                }
                this.gates[i] = gates[i];
            }
        }

        public bool IsFlightAllowed
        {
            get
            {
                foreach (var gate in gates)
                {
                    if (!gate.IsFlightAllowed)
                    {
                        return false;
                    }
                }
                return true;
            }
        }
    }
}
