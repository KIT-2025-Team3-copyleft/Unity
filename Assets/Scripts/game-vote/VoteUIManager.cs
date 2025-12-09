using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class VoteUIManager : MonoBehaviour
{
    public static VoteUIManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
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

    public List<Button> playerVoteButtons = new List<Button>();
    public List<List<GameObject>> vMarksForButtons = new List<List<GameObject>>();

    [Header("Step3 UI")]
    public TextMeshProUGUI resultMessage;

    private Coroutine hideCoroutine;
    private Coroutine step1TimerCoroutine; // Step1 타이머 코루틴

    public void LinkVoteUI(GameObject localPlayerRoot)
    {
        if (localPlayerRoot == null)
        {
            Debug.LogError("VoteUIManager: localPlayerRoot is null.");
            return;
        }

        Transform canvasRoot = localPlayerRoot.transform.Find("Canvas");
        if (canvasRoot == null)
        {
            Debug.LogError("VoteUIManager: Canvas not found in localPlayerRoot.");
            return;
        }

        // Step1
        Transform step1 = canvasRoot.Find("VoteRequestPanel");
        if (step1 != null)
        {
            voteRequestPanel = step1.gameObject;
            requestTimerText = step1.Find("Timer")?.GetComponent<TextMeshProUGUI>();
            requestCountText = step1.Find("Count")?.GetComponent<TextMeshProUGUI>();
            agreeButton = step1.Find("agree")?.GetComponent<Button>();
            disagreeButton = step1.Find("disagree")?.GetComponent<Button>();
        }

        // Step2
        Transform step2 = canvasRoot.Find("HereticVotePanel");
        if (step2 != null)
        {
            hereticPanel = step2.gameObject;
            hereticTimerText = step2.Find("Timer")?.GetComponent<TextMeshProUGUI>();

            playerVoteButtons.Clear();
            vMarksForButtons.Clear();

            for (int i = 1; i <= 4; i++)
            {
                Transform p = step2.Find($"playercheck{i}");
                if (p != null)
                {
                    Button btn = p.GetComponent<Button>();
                    playerVoteButtons.Add(btn);

                    Transform vmarks = p.Find("VMarks");
                    List<GameObject> markList = new List<GameObject>();
                    if (vmarks != null)
                    {
                        foreach (Transform child in vmarks)
                        {
                            child.gameObject.SetActive(false);
                            markList.Add(child.gameObject);
                        }
                    }
                    vMarksForButtons.Add(markList);
                }
            }
        }

        // Step3
        resultMessage = canvasRoot.Find("ResultMessage")?.GetComponent<TextMeshProUGUI>();

        voteRequestPanel?.SetActive(false);
        hereticPanel?.SetActive(false);
        resultMessage?.gameObject.SetActive(false);
    }

    // ============================ STEP 1 ===============================
    public void ShowStep1()
    {
        voteRequestPanel.SetActive(true);
        hereticPanel.SetActive(false);
        resultMessage.gameObject.SetActive(false);

        requestCountText.text = "0/4";
        requestTimerText.text = "-";

        agreeButton.onClick.RemoveAllListeners();
        disagreeButton.onClick.RemoveAllListeners();

        agreeButton.onClick.AddListener(() => VoteManager.Instance.SendStep1Vote(true));
        disagreeButton.onClick.AddListener(() => VoteManager.Instance.SendStep1Vote(false));
    }

    public void UpdateStep1Timer(int t) => requestTimerText.text = t.ToString();
    public void UpdateStep1Count(int agree, int total) => requestCountText.text = $"{agree}/{total}";

    // Step1 시간 흐르게 하는 코루틴
    public void StartStep1Timer(int seconds)
    {
        if (step1TimerCoroutine != null) StopCoroutine(step1TimerCoroutine);
        step1TimerCoroutine = StartCoroutine(Step1TimerRoutine(seconds));
    }

    private IEnumerator Step1TimerRoutine(int remaining)
    {
        while (remaining >= 0)
        {
            UpdateStep1Timer(remaining);
            yield return new WaitForSeconds(1f);
            remaining--;
        }
    }

    // ============================ STEP 2 ===============================
    public void ShowStep2(List<string> names)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(true);
        resultMessage.gameObject.SetActive(false);

        for (int i = 0; i < playerVoteButtons.Count; i++)
        {
            bool active = i < names.Count;
            playerVoteButtons[i].gameObject.SetActive(active);

            if (active)
            {
                playerVoteButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = names[i];

                int idx = i;
                playerVoteButtons[i].onClick.RemoveAllListeners();
                playerVoteButtons[i].onClick.AddListener(() =>
                    VoteManager.Instance.SendStep2Vote(idx)
                );
            }

            foreach (var mark in vMarksForButtons[i])
                mark.SetActive(false);
        }
    }

    public void UpdateStep2Timer(int t) => hereticTimerText.text = t.ToString();

    public void UpdateVMark(int targetIndex, int voteCount)
    {
        if (targetIndex < 0 || targetIndex >= vMarksForButtons.Count) return;

        for (int i = 0; i < vMarksForButtons[targetIndex].Count; i++)
            vMarksForButtons[targetIndex][i].SetActive(i < voteCount);
    }

    // ============================ RESULT ===============================
    public void ShowResult(string msg, float duration = 3f)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(false);

        resultMessage.text = msg;
        resultMessage.gameObject.SetActive(true);

        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideResult(duration));
    }

    private IEnumerator HideResult(float d)
    {
        yield return new WaitForSeconds(d);
        resultMessage.gameObject.SetActive(false);
    }
}
