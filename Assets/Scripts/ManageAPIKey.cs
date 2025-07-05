using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManageAPIKey : MonoBehaviour
{
    [Header("API Key Input UI")]
    public GameObject apiKeyInputPanel;
    public TMP_InputField apiKeyInputField;
    public Button submitApiKeyButton;

    [Header("Main UI")]
    public GameObject mainPanel;

    private const string ApiKeyPref = "UserApiKey";

    void Start()
    {
        //PlayerPrefs.DeleteAll();
        // Check if the API key is already saved. [1]
        if (PlayerPrefs.HasKey(ApiKeyPref))
        {
            // If the key exists, hide the API input UI and show the main UI. [12, 15]
            apiKeyInputPanel.SetActive(false);
            mainPanel.SetActive(true);
        }
        else
        {
            // If the key does not exist, show the API input UI and hide the main UI. [12, 15]
            apiKeyInputPanel.SetActive(true);
            mainPanel.SetActive(false);

            // Add a listener to the submit button.
            submitApiKeyButton.onClick.AddListener(SaveApiKey);
        }
    }

    void SaveApiKey()
    {
        string apiKey = apiKeyInputField.text;

        // Simple validation to ensure the input is not empty.
        if (!string.IsNullOrEmpty(apiKey))
        {
            // Save the API key to PlayerPrefs. [3]
            PlayerPrefs.SetString(ApiKeyPref, apiKey);
            // It's good practice to explicitly save PlayerPrefs. [11]
            PlayerPrefs.Save();

            // Hide the API input UI and show the main UI. [12, 15]
            apiKeyInputPanel.SetActive(false);
            mainPanel.SetActive(true);
        }
        else
        {
            // Optionally, provide feedback to the user that the API key cannot be empty.
            Debug.LogWarning("API Key input field is empty!");
        }
    }
}