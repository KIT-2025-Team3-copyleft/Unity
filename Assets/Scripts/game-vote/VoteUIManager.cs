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

    [Header("Step1 UI")]
    public GameObject voteRequestPanel;
    public TextMeshProUGUI requestTimerText;
    public TextMeshProUGUI requestCountText;
    public Button agreeButton;
    public Button disagreeButton;

    [Header("Step2 UI")]
    public GameObject hereticPanel;
    public TextMeshProUGUI hereticTimerText;
    public List<Button> playerVoteButtons;
    public List<Transform> vMarkParents;

    public List<List<GameObject>> vMarksForButtons = new List<List<GameObject>>();

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

    // ------------------- Step1 UI -------------------
    public void ShowStep1()
    {
        voteRequestPanel.SetActive(true);
        hereticPanel.SetActive(false);
        resultMessage.gameObject.SetActive(false);
    }

    public void UpdateStep1Count(int count)
    {
        requestCountText.text = $"{count}/4";
    }

    public void UpdateStep1Timer(int t)
    {
        requestTimerText.text = t.ToString();
    }

    // ------------------- Step2 UI -------------------
    public void ShowStep2(List<string> nicknames)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(true);
        resultMessage.gameObject.SetActive(false);

        for (int i = 0; i < playerVoteButtons.Count; i++)
        {
            int idx = i;

            playerVoteButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = nicknames[i];

            playerVoteButtons[i].onClick.RemoveAllListeners();
            playerVoteButtons[i].onClick.AddListener(() =>
            {
                // 로컬 테스트이므로 voterIndex = 0
                VoteManager.Instance.OnVoteStep2(0, idx);
            });
        }
    }

    public void UpdateStep2Timer(int t)
    {
        hereticTimerText.text = t.ToString();
    }

    public void UpdateVMark(int playerIdx, int count)
    {
        for (int i = 0; i < vMarksForButtons[playerIdx].Count; i++)
        {
            vMarksForButtons[playerIdx][i].SetActive(i < count);
        }
    }

    // ------------------- Step3 UI -------------------
    public void ShowResult(string msg)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(false);

        resultMessage.text = msg;
        resultMessage.gameObject.SetActive(true);
    }
}
