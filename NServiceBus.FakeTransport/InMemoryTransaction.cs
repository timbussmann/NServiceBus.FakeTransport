using System;
using System.Collections.Generic;

namespace NServiceBus.FakeTransport
{
    public class InMemoryTransaction
    {
        private readonly List<Action> actions = new List<Action>();

        public void Complete()
        {
            foreach (var action in actions)
            {
                action();
            }
        }

        public void Enlist(Action action)
        {
            actions.Add(action);
        }
    }
}