using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    private Coroutine step1TimerCoroutine;
    private Coroutine step2TimerCoroutine;

    // ============================ LINK UI ===============================
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

        // 초기 숨김
        HideAll();
        Debug.Log("[VoteUIManager] UI 연결 완료");
    }

    // ============================ HIDE ALL ===============================
    public void HideAll()
    {
        voteRequestPanel?.SetActive(false);
        hereticPanel?.SetActive(false);
        resultMessage?.gameObject.SetActive(false);
    }

    // ============================ STEP1 ===============================
    public void ShowStep1()
    {
        voteRequestPanel.SetActive(true);
        hereticPanel.SetActive(false);
        resultMessage.gameObject.SetActive(false);

        requestCountText.text = "0/4";
        requestTimerText.text = "-";

        agreeButton.interactable = true;
        disagreeButton.interactable = true;

        agreeButton.onClick.RemoveAllListeners();
        disagreeButton.onClick.RemoveAllListeners();

        agreeButton.onClick.AddListener(() =>
        {
            VoteManager.Instance.SendStep1Vote(true);
            LockStep1Buttons();
        });

        disagreeButton.onClick.AddListener(() =>
        {
            VoteManager.Instance.SendStep1Vote(false);
            LockStep1Buttons();
        });
    }
    public void ResetAllVMarks()
    {
        for (int i = 0; i < vMarksForButtons.Count; i++)
        {
            for (int j = 0; j < vMarksForButtons[i].Count; j++)
            {
                vMarksForButtons[i][j].SetActive(false);
            }
        }
    }

    public void UpdateStep1Timer(int t) => requestTimerText.text = t.ToString();
    public void UpdateStep1Count(int agree, int total) => requestCountText.text = $"{agree}/{total}";

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

        // 시간 종료 시 버튼 잠금
        LockStep1Buttons();
    }

    public void LockStep1Buttons()
    {
        agreeButton.interactable = false;
        disagreeButton.interactable = false;
    }

    // ============================ STEP2 ===============================
    public void ShowStep2(List<PlayerManager> players)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(true);
        resultMessage.gameObject.SetActive(false);

        ResetAllVMarks();

        for (int i = 0; i < playerVoteButtons.Count; i++)
        {
            bool active = i < players.Count;
            playerVoteButtons[i].gameObject.SetActive(active);

            if (active)
            {
                var player = players[i];

                // 닉네임 적용
                playerVoteButtons[i].GetComponentInChildren<TextMeshProUGUI>().text = player.nickname;

                // 색상 적용
                playerVoteButtons[i].GetComponentInChildren<TextMeshProUGUI>().color = ParseColor(player.colorName);

                int idx = i;
                playerVoteButtons[i].onClick.RemoveAllListeners();
                playerVoteButtons[i].onClick.AddListener(() =>
                {
                    VoteManager.Instance.SendStep2Vote(idx);
                    LockStep2Buttons(idx);
                });
            }

            foreach (var mark in vMarksForButtons[i])
                mark.SetActive(false);

            playerVoteButtons[i].interactable = true; // 버튼 초기화
        }
    }

    public void StartStep2Timer(int seconds)
    {
        if (step2TimerCoroutine != null) StopCoroutine(step2TimerCoroutine);
        step2TimerCoroutine = StartCoroutine(Step2TimerRoutine(seconds));
    }

    private IEnumerator Step2TimerRoutine(int remaining)
    {
        while (remaining >= 0)
        {
            UpdateStep2Timer(remaining);
            yield return new WaitForSeconds(1f);
            remaining--;
        }

        // 시간 종료 시 버튼 잠금
        LockStep2Buttons(-1);
    }

    public void UpdateStep2Timer(int t) => hereticTimerText.text = t.ToString();

    public void LockStep2Buttons(int votedIndex)
    {
        for (int i = 0; i < playerVoteButtons.Count; i++)
            playerVoteButtons[i].interactable = false;

        if (votedIndex >= 0 && votedIndex < playerVoteButtons.Count)
        {
            ColorBlock cb = playerVoteButtons[votedIndex].colors;
            cb.normalColor = new Color(0.6f, 1f, 0.6f);
            playerVoteButtons[votedIndex].colors = cb;
        }
    }

    public void UpdateVMark(int targetIndex, int voteCount)
    {
        if (targetIndex < 0 || targetIndex >= vMarksForButtons.Count) return;

        for (int i = 0; i < vMarksForButtons[targetIndex].Count; i++)
            vMarksForButtons[targetIndex][i].SetActive(i < voteCount);
    }
    private Color ParseColor(string colorStr)
    {
        return colorStr.ToLower() switch
        {
            "red" => Color.red,
            "blue" => Color.blue,
            "green" => Color.green,
            "yellow" => Color.yellow,
            _ => Color.white
        };
    }

    // ============================ RESULT ===============================
    public void ShowResult(string msg, float duration = 3f)
    {
        voteRequestPanel.SetActive(false);
        hereticPanel.SetActive(false);

        ResetAllVMarks();

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
