using UnityEngine;
using System.Collections.Generic;

public class Room
{
    public string RoomName;
    public List<string> Players = new List<string>();
}

public class RoomManager : MonoBehaviour
{
    public static RoomManager Instance;

    public List<Room> Rooms = new List<Room>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 유지
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 방이 있는지 확인
    public bool HasRoom()
    {
        return Rooms.Count > 0;
    }

    // 방 참가
    public void JoinRoom(int roomIndex)
    {
        if (roomIndex < 0 || roomIndex >= Rooms.Count) return;

        Room room = Rooms[roomIndex];
        room.Players.Add(GameManager.Instance.PlayerName); // 플레이어 닉네임 추가
        GameManager.Instance.CurrentRoomId = roomIndex;
    }

    // 현재 방 가져오기
    public Room GetCurrentRoom()
    {
        return Rooms[GameManager.Instance.CurrentRoomId];
    }

    // 테스트용: 임시 방 만들기
    public void CreateTestRoom()
    {
        Room newRoom = new Room();
        newRoom.RoomName = "테스트 방";
        Rooms.Add(newRoom);
    }
}
