using UnityEngine;

public class OpenURL : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OpenTogetherURL()
    {
        Application.OpenURL("https://api.together.xyz/v1/images/");
    }
}
