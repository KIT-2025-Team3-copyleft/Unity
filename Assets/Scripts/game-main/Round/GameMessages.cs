using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class MessageWrapper
{
    public string @event;
    public string action;
}

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
    public string role;
    public string slot;
    public string selectedCard;
    public string voteTarget;
}


[Serializable]
public class RoundStartMessage
{
    public int currentRound;
    public int timeLimit;
    public string mission;
    public string myRole;
    public string mySlot;
    public List<string> cards;
    public bool chatEnabled;
    public string godPersonality;
    public List<Player> players;
    public string oracle;
    public string message;
    // public Dictionary<string, string> slotColors; // 👈 로컬에서 생성하므로 제거
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
    public string sentence;
    public int score;
    public VisualCue visualCue;
    public TrialProposalPhase trialProposalPhase;

    public string reason;

    public List<string> finalWords;

    // public Dictionary<string, string> slotColors; // 👈 로컬에서 생성하므로 제거
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

[System.Serializable]
public class RoundResultResponse
{
    public string @event;
    public string message;
    public RoundResult data;
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

// RECEIVE_CARDS 전용 구조체
[System.Serializable]
public class ReceiveCardsData
{
    public string slotType;
    public List<string> cards;
}

[System.Serializable]
public class ReceiveCardsMessage
{
    public string @event;
    public string message;
    public ReceiveCardsData data;
}

// 새로운 구조체 추가 (다른 플레이어 슬롯 할당을 위함)
[Serializable]
public class SlotAssignment
{
    public string sessionId;
    public string slot; // "SUBJECT", "TARGET", "HOW", "ACTION" 중 하나
}

[Serializable]
public class PlayerSlotAssignmentData
{
    public List<SlotAssignment> assignments;
}

[Serializable]
public class PlayerSlotAssignmentMessage
{
    public string @event; // "PLAYER_SLOT_ASSIGNMENT"
    public PlayerSlotAssignmentData data;
}