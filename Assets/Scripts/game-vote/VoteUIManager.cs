using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoteUIManager : MonoBehaviour
{
    public static VoteUIManager Instance;

    private void Awake()
    {
        Instance = this;
    }

    // ------------------- Step1 UI -------------------
    [Header("Step1 UI")]
    public GameObject voteRequestPanel;
    public TextMeshProUGUI requestTimerText;
    public TextMeshProUGUI requestCountText;
    public Button agreeButton;
    public Button disagreeButton;

    // ------------------- Step2 UI -------------------
    [Header("Step2 UI")]
    public GameObject hereticPanel;
    public TextMeshProUGUI hereticTimerText;
    public List<Button> playerVoteButtons;
    public List<Transform> vMarkParents;

    public List<List<GameObject>> vMarksForButtons = new List<List<GameObject>>();

    // ------------------- Step3 UI -------------------
    [Header("Step3 UI")]
    public TextMeshProUGUI resultMessage;

    private void Start()
    {
        InitVMarks();
    }

    private void InitVMarks()
    {
        vMarksForButtons.Clear();

        foreach (Transform parent in vMarkParents)
        {
            List<GameObject> marks = new List<GameObject>();
            foreach (Transform child in parent)
            {
                marks.Add(child.gameObject);
                child.gameObject.SetActive(false);
            }
            vMarksForButtons.Add(marks);
        }
    }

    // =====================================================
    // STEP 1 UI
    // =====================================================

    public void ShowStep1()
    {
        voteRequestPanel.SetActive(true);
        hereticPanel.SetActive(false);
        resultMessage.gameObject.SetActive(false);

        requestCountText.text = "0/4";
        requestTimerText.text = "-";
    }

    public void SetStep1Buttons()
    {
        agreeButton.onClick.RemoveAllListeners();
        disagreeButton.onClick.RemoveAllListeners();

        agreeButton.onClick.AddListener(() => VoteManager.Instance.SendStep1Vote(true));
        disagreeButton.onClick.AddListener(() => VoteManager.Instance.SendStep1Vote(false));
    }

    public void UpdateStep1Timer(int t)
    {
        requestTimerText.text = t.ToString();
    }

    public void UpdateStep1Count(int count, int total)
    {
        requestCountText.text = $"{count}/{total}";
    }

    // =====================================================
    // STEP 2 UI
    // =====================================================

    public void ShowStep2(List<string> nicknames)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(true);
        resultMessage.gameObject.SetActive(false);

        for (int i = 0; i < playerVoteButtons.Count; i++)
        {
            int idx = i;
            var btn = playerVoteButtons[i];

            btn.GetComponentInChildren<TextMeshProUGUI>().text = nicknames[i];

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => VoteManager.Instance.SendStep2Vote(idx));
        }
    }

    public void UpdateStep2Timer(int t)
    {
        hereticTimerText.text = t.ToString();
    }

    public void UpdateVMark(int targetIndex, int count)
    {
        for (int i = 0; i < vMarksForButtons[targetIndex].Count; i++)
        {
            vMarksForButtons[targetIndex][i].SetActive(i < count);
        }
    }

    // =====================================================
    // STEP 3 UI
    // =====================================================

    public void ShowResult(string msg)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(false);

        resultMessage.text = msg;
        resultMessage.gameObject.SetActive(true);
    }
}
