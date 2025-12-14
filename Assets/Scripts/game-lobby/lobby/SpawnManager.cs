/*using UnityEngine;
using System.Collections.Generic;
public class SpawnManager : MonoBehaviour {
    public static SpawnManager Instance; 
    public Transform[] spawnPoints; 
    void Awake() {
        if (Instance != null && Instance != this) 
            Destroy(this.gameObject); 
        else Instance = this; } 
    public Vector3 GetSpawnPosition(int playerIndex) {
        if (spawnPoints.Length == 0) return Vector3.zero; 
        return spawnPoints[playerIndex % spawnPoints.Length].position; 
    } 
}*/