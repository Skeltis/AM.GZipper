using System;

namespace AM.GZipperLib.DecisionTree
{
    public class DecisionCondition : IDecisionNode
    {
        private object sync = new object();

        private IDecisionNode _yes;
        private IDecisionNode _no;
        private Func<bool> _condition;

        public string Name { get; }

        public DecisionCondition(string name, Func<bool> condition,IDecisionNode yesNode, IDecisionNode noNode)
        {
            Name = name;
            _condition = condition;
            _yes = yesNode;
            _no = noNode;
        }

        public Action GetResolution()
        {
            return FindDecision().GetResolution();
        }

        public IDecisionNode GetDecision()
        {
            return (_condition.Invoke()) ? _yes : _no;
        }

        public Decision FindDecision()
        {
            IDecisionNode decision = this;
            do
            {
                decision = decision.GetDecision();
            } while (!(decision is Decision) && decision != null);
            //System.Diagnostics.Debug.WriteLine($"decision: {decision.Name}");
            return (decision as Decision);
        }


    }
}
