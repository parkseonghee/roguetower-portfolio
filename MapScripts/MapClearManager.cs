using UnityEngine;

public class MapClearManager : MonoBehaviour
{
    public Transform monsterHolder; // 몬스터들이 담긴 부모 오브젝트
    private bool doorsOpened = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CheckAndOpenDoors()
    {
        Debug.Log("return문제?");
        if (doorsOpened) return;

        // 자식 오브젝트의 개수를 확인
        if (monsterHolder.childCount == 0)
        {
            WaveManager.Instance.OpenDoors();
            doorsOpened = true;
            Debug.Log("못 다잡음 문열림");
        }
        Debug.Log("연결은됐음");
        
    }
}
