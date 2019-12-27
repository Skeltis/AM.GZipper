using System;

namespace AM.GZipperLib.DecisionTree
{
    public interface IDecisionNode
    {
        string Name { get; }
        IDecisionNode GetDecision();
        Decision FindDecision();
        Action GetResolution();
    }
}
