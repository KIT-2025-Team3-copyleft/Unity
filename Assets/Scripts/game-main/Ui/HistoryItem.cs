using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class HistoryItem : MonoBehaviour
{
    // UIManager의 SlotVisualOrder를 기반으로 매핑을 정의합니다.
    private readonly string[] SlotVisualOrder = { "SUBJECT", "TARGET", "HOW", "ACTION" };

    // 🌟 추가: 라운드 번호와 신탁
    public TextMeshProUGUI roundText;
    public TextMeshProUGUI OracleText;

    // 완성 문장 (4개 단어)
    public List<TextMeshProUGUI> wordTexts;

    // 신의 반응 (이모지)
    public TextMeshProUGUI reactionText;

    // 신의 평가
    public TextMeshProUGUI evaluationText;

    public void SetData(RoundResult result, Dictionary<string, string> slotColors, int roundNumber, string mission, List<string> finalWords)
    {
        roundText.text = $"라운드 {roundNumber}의 기록";
        OracleText.text = $"신탁: {mission}";

        evaluationText.text = $"신의 평가: {result.reason}";

        //string reactionEmoji = GetReactionEmoji(result.visualCue.effect);
        //reactionText.text = $"신의 반응 : {reactionEmoji}";

        DisplayFinalSentence(slotColors, finalWords);
    }

    private string GetReactionEmoji(string effect)
    {

        return "";
    }

    private void DisplayFinalSentence(Dictionary<string, string> slotColors, List<string> finalWords)
    {
        for (int i = 0; i < finalWords.Count && i < wordTexts.Count; i++)
        {
            if (i >= SlotVisualOrder.Length) continue;

            string slotRoleName = SlotVisualOrder[i];

            string colorName = slotColors.ContainsKey(slotRoleName) ? slotColors[slotRoleName] : "green";

            wordTexts[i].text = finalWords[i];

            wordTexts[i].color = GetUnityColor(colorName);
        }
    }

    private Color GetUnityColor(string colorName)
    {
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
                Debug.LogWarning($"Unknown color name: {colorName}. Defaulting to green.");
                return Color.green;
        }
    }
}