using System;
using System.Collections.Generic;
using UnityEngine;

// ----------------------------------------------------
// 🔸 통신 공통 구조 (Client <-> Server)
// ----------------------------------------------------

[System.Serializable]
public class MessageWrapper
{
    public string @event;
    public string action;
}

// Client -> Server Payload Structures
[System.Serializable]
public class CardSelectionPayload
{
    public string card;
}
[System.Serializable]
public class ProposeVotePayload
{
    public bool agree;
}
[System.Serializable]
public class CastVotePayload
{
    public string targetSessionId;
}
[System.Serializable]
public class ChatMessagePayload
{
    public string message;
}



[Serializable]
public class Player
{
    public string sessionId;
    public string nickname;
    public bool isHost;
    public string color;
    public string connectionStatus;
}

// ----------------------------------------------------
// 🌟 게임 진행 메시지 (기존 구조)
// ----------------------------------------------------

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

    public string reason;

    public List<string> finalWords;

    public Dictionary<string, string> slotColors;
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

[Serializable]
public class ShowOracleMessageData
{
    public string oracle;
}
[Serializable]
public class ShowOracleMessage
{
    public string @event;
    public string message;
    public ShowOracleMessageData data;
}

[Serializable]
public class ShowRoleMessageData
{
    public string role;
    public string godPersonality;
}
[Serializable]
public class ShowRoleMessage
{
    public string @event;
    public string message;
    public ShowRoleMessageData data;
}

[Serializable]
public class TrialResultData
{
    public bool success;
    public string targetNickname;
    public string targetRole;
}

[Serializable]
public class TrialResultMessage
{
    public string @event;
    public string message;
    public string code;
    public TrialResultData data;
}

[Serializable]
public class ChatMessageData
{
    public string sender;
    public string color;
    public string content;
    public string formattedMessage;
}

[Serializable]
public class ChatMessage
{
    public string @event;
    public ChatMessageData data;
}

[Serializable]
public class ErrorMessage
{
    public string @event;
    public string code;
    public string message;
}