using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class HistoryItem : MonoBehaviour
{
    // 완성 문장 (4개 단어)
    public List<TextMeshProUGUI> wordTexts;

    // 신의 반응 (이모지)
    public TextMeshProUGUI reactionText;

    // 신의 평가
    public TextMeshProUGUI evaluationText;

    public void SetData(RoundResult result, Dictionary<string, string> slotColors, int roundNumber, List<string> finalWords)
    {
        evaluationText.text = $"신의 평가: {result.finalSentence}";

        string reactionEmoji = GetReactionEmoji(result.visualCue.effect);
        reactionText.text = $"신의 반응 : {reactionEmoji}";

        DisplayFinalSentence(slotColors, finalWords);
    }

    private string GetReactionEmoji(string effect)
    {
        // HP 상승 or 하락에 따른 이모지
        if (effect.Contains("success") || effect.Contains("bloom")) return "🌸";
        if (effect.Contains("fail") || effect.Contains("thunder")) return "⚡";
        return "";
    }

    private void DisplayFinalSentence(Dictionary<string, string> slotColors, List<string> finalWords)
    {
        for (int i = 0; i < finalWords.Count && i < wordTexts.Count; i++)
        {
            string slotId = $"slot{i + 1}";
            // 기본 색상 초록
            string colorName = slotColors.ContainsKey(slotId) ? slotColors[slotId] : "green";

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
                // 매칭되는 색이 없을 경우 디버그 메시지를 출력하고 기본 색상을 반환
                Debug.LogWarning($"Unknown color name: {colorName}. Defaulting to green.");
                return Color.green;
        }
    }
}