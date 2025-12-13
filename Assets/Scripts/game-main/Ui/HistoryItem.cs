using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class HistoryItem : MonoBehaviour
{
    private readonly string[] SlotVisualOrder = { "SUBJECT", "TARGET", "HOW", "ACTION" };

    // 라운드 번호와 신탁
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI OracleText;

    // 완성 문장 (4개 단어)
    public List<TextMeshProUGUI> wordTexts;

    // 신의 반응 (이모지)
    public TextMeshProUGUI reactionText;

    // 신의 평가
    public TextMeshProUGUI evaluationText;

    public void SetData(RoundResult result, Dictionary<string, string> slotColors, int roundNumber, string mission)
    {
        if (roundText != null) roundText.text = $"라운드 {roundNumber}의 기록";
        if (OracleText != null) OracleText.text = $"신탁: {mission}";

        if (evaluationText != null) evaluationText.text = $"신의 평가: {result.reason}";

        string reaction = GetReactionText(result.score);
        if (reactionText != null)
        {
            reactionText.text = $"신의 반응: {reaction}";
        }

        DisplayFinalSentence(slotColors, result.sentenceParts);
    }

    // 이후 이모지로 변?경 
    private string GetReactionText(int score)
    {
        if (score < 0)
        {
            return "번개"; 
        }
        else
        {
            return "꽃잎"; 
        }
    }

    private void DisplayFinalSentence(Dictionary<string, string> slotColors, List<SentencePart> sentenceParts)
    {
        for (int i = 0; i < sentenceParts.Count && i < wordTexts.Count; i++)
        {
            SentencePart part = sentenceParts[i];

            string colorName = part.playerColor;


            Debug.Log($"[HistoryItem Color Debug] Slot: {part.slotType}, Word: {part.word}, Color (from part): {colorName}");

            if (wordTexts[i] != null)
            {
                wordTexts[i].text = part.word;
                wordTexts[i].color = GetUnityColor(colorName);
            }
            else
            {
                Debug.LogError($"❌ HistoryItem: wordTexts[{i}] 참조가 null입니다.");
            }
        }
    }

    private Color GetUnityColor(string colorName)
    {
        if (colorName == null) colorName = "unknown";

        switch (colorName.ToLower())
        {
            case "red":
                return Color.red;
            case "blue":
                return Color.blue;
            case "green":
                return Color.green;
            case "yellow":
                return Color.yellow;
            case "pink":
                return new Color(1f, 0.41f, 0.71f);
            default:
                Debug.LogWarning($"Unknown color name: {colorName}. Defaulting to white.");
                return Color.white;
        }
    }
}