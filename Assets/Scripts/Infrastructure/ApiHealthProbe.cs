using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public sealed class ApiHealthProbe : MonoBehaviour
{
    [SerializeField] private string baseUrl = "http://localhost:5000";
    private string status = "Checking API...";

    private void Start() => StartCoroutine(CheckHealth());

    private IEnumerator CheckHealth()
    {
        using var request = UnityWebRequest.Get($"{baseUrl.TrimEnd('/')}/health");
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            status = $"API: {request.downloadHandler.text}";
            Debug.Log(status);
        }
        else
        {
            status = $"API unavailable: {request.error}";
            Debug.LogWarning(status);
        }
    }

    private void OnGUI() => GUI.Label(new Rect(16, 16, 900, 24), status);
}
