using UnityEngine;

public class Car : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log(FolderMaker.Creat(this,"HAHA"+Random.Range(1,150)));
        }
    }
}