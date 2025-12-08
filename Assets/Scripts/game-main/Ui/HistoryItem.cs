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

    // 🌟 SetData에서 slotColors는 이제 PlayerManager 데이터 기반으로 생성된 딕셔너리입니다.
    public void SetData(RoundResult result, Dictionary<string, string> slotColors, int roundNumber, string mission, List<string> finalWords)
    {
        // 🌟 UI 요소들이 연결되어 있는지 확인
        if (roundText != null) roundText.text = $"라운드 {roundNumber}의 기록";
        if (OracleText != null) OracleText.text = $"신탁: {mission}";

        if (evaluationText != null) evaluationText.text = $"신의 평가: {result.reason}";

<<<<<<< HEAD
        //string reactionEmoji = GetReactionEmoji(result.visualCue.effect);
        //reactionText.text = $"신의 반응 : {reactionEmoji}";
=======
        string reactionEmoji = GetReactionEmoji(result.visualCue.effect);
        if (reactionText != null) reactionText.text = $"신의 반응 : {reactionEmoji}";
>>>>>>> 3470fcd (Stash 2025/12/09 12:54)

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

            // 🌟 slotColors의 키는 SUBJECT, TARGET 등 역할 이름입니다.
            string colorName = slotColors.ContainsKey(slotRoleName) ? slotColors[slotRoleName] : "green";

            if (wordTexts[i] != null)
            {
                wordTexts[i].text = finalWords[i];
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