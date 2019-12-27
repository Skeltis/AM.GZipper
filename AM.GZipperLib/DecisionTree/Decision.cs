using System;

namespace AM.GZipperLib.DecisionTree
{
    public class Decision : IDecisionNode
    {
        private Action _decision;

        public string Name { get; }

        public Decision(string name, Action decision)
        {
            Name = name;
            _decision = decision;
        }

        public Action GetResolution() => _decision;

        public IDecisionNode GetDecision() => this;

        public Decision FindDecision() => this;
    }
}
