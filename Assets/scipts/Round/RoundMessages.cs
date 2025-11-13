using System;
using System.Collections.Generic;

[Serializable]
public class RoundStartMessage
{
    public int roundNumber;
    public int timeLimit;
    public string mission;
    public string myRole;
    public string mySlot;
    public List<string> cards;
    public bool chatEnabled;
    public string godPersonality;
}

[Serializable]
public class PlayerActionUpdate
{
    public string playerId;
    public string actionStatus;
}

[Serializable]
public class InterpretationEnd
{
    public string message;
    public bool chatEnabled;
}

[Serializable]
public class RoundResult
{
    public string finalSentence;
    public int scoreChange;
    public VisualCue visualCue;
    public TrialProposalPhase trialProposalPhase;
}

[Serializable]
public class VisualCue
{
    public string subject;
    public string target;
    public string effect;
    public string action;
}

[Serializable]
public class TrialProposalPhase
{
    public bool active;
    public int timeLimit;
}