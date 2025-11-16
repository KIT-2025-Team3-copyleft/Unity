using UnityEngine;
using System;
using System.Collections.Generic;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private static readonly Queue<Action> _jobs = new Queue<Action>();

    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            return _instance;
        }
    }

    void Awake()
    {
        // 씬에 이미 존재하는 Dispatcher만 사용
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        lock (_jobs)
        {
            while (_jobs.Count > 0)
                _jobs.Dequeue().Invoke();
        }
    }

    public void Enqueue(Action job)
    {
        lock (_jobs)
        {
            _jobs.Enqueue(job);
        }
    }
}
