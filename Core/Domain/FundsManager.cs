using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace NosAyudamos
{
    class FundsManager : DomainObject
    {
        /// <summary>
        /// Factory method allows us to distinguish the deserialization scenario (using the 
        /// parameterless contructor) from the actual operating manager when initially created.
        /// </summary>
        public static FundsManager Create() => new FundsManager(Enumerable.Empty<DomainEvent>());

        public FundsManager(IEnumerable<DomainEvent> history)
            : this() => Load(history);

        /// <summary>
        /// Constructs a readonly funds manager.
        /// </summary>
        public FundsManager()
        {
            Handles<FundsAdded>(OnFundsAdded);
            Handles<FundsAssigned>(OnFundsAssigned);
            Handles<FundsRequested>(OnFundsRequested);
            Handles<FundsRequestReplaced>(OnFundsRequestReplaced);
        }

        // NOTE: the [JsonProperty] attributes allow the deserialization from 
        // JSON to be able to set the properties when loading from the last  
        // saved known snapshot state.

        [JsonProperty]
        public double AssignedAmount { get; private set; }

        [JsonProperty]
        public double AvailableAmount { get; private set; }

        [JsonProperty]
        public double RequestedAmount { get; private set; }

        [JsonProperty]
        public IDictionary<string, double> Requests { get; private set; } = new Dictionary<string, double>();

        public void Add(double amount, string from)
        {
            if (amount < 0)
                throw new ArgumentException("Can only donate positive amounts.");

            Raise(new FundsAdded(amount, from));
        }

        public void Assign()
        {
            if (AvailableAmount >= RequestedAmount)
            {
                foreach (var request in Requests)
                {
                    Raise(new FundsAssigned(request.Value, request.Key));
                }
            }
            else
            {
                // TODO: algorithm
            }
        }

        public void Request(double amount, string by)
        {
            if (amount < 0)
                throw new ArgumentException("Can only donate positive amounts.");

            if (Requests.ContainsKey(by))
                Raise(new FundsRequestReplaced(amount, by));
            else
                Raise(new FundsRequested(amount, by));
        }

        void OnFundsAdded(FundsAdded e) => AvailableAmount += e.Amount;

        void OnFundsAssigned(FundsAssigned e)
        {
            AvailableAmount -= e.Amount;
            AssignedAmount += e.Amount;
            RequestedAmount -= e.Amount;
            Requests.Remove(e.Person);
        }

        void OnFundsRequested(FundsRequested e)
        {
            Requests[e.Person] = e.Amount;
            RequestedAmount += e.Amount;
        }

        void OnFundsRequestReplaced(FundsRequestReplaced e)
        {
            if (!Requests.TryGetValue(e.Person, out var amount))
                throw new InvalidOperationException("There should have been an existing requested amount to replace.");

            RequestedAmount -= amount;
            RequestedAmount += e.Amount;

            Requests[e.Person] = e.Amount;
        }
    }
}
