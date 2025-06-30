using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System;

public class ImageGenerator : MonoBehaviour
{
    // --- UI Elements ---
    [Header("UI Elements")]
    public TMP_InputField promptInputField;
    public TMP_Dropdown resolutionDropdown;
    public Button generateButton;
    public RawImage resultImage;
    public TextMeshProUGUI statusText;

    // --- API Configuration ---
    [Header("API Configuration")]
    public string apiKey = "tgp_v1_o22BAWeNWFpwtrZy3emEVHrucGvX4vgDsyXW0lswRNU"; // Replace with your Together AI API key

    private const string ApiUrl = "https://api.together.xyz/v1/images/generations";
    private const string OutputFolderName = "AI_Generated_Images";

    private void Start()
    {
        // --- Initialize UI ---
        generateButton.onClick.AddListener(OnGenerateButtonClick);

        // Check for API Key
        if (string.IsNullOrEmpty(apiKey) || apiKey == "YOUR_API_KEY")
        {
            statusText.text = "API Key not set. Please enter your API Key in the Inspector.";
            generateButton.interactable = false;
        }
    }

    private void OnGenerateButtonClick()
    {
        StartCoroutine(GenerateImage());
    }

    private IEnumerator GenerateImage()
    {
        // --- Get User Input ---
        string prompt = promptInputField.text;
        if (string.IsNullOrEmpty(prompt))
        {
            statusText.text = "Please enter a prompt.";
            yield break;
        }

        // --- Set UI to Loading State ---
        generateButton.interactable = false;
        statusText.text = "Generating image...";

        // --- Prepare API Request ---
        var requestData = new RequestData
        {
            prompt = prompt,
            model = "black-forest-labs/FLUX.1-schnell-Free",
            steps = 4,
        };

        (requestData.width, requestData.height) = GetResolution(resolutionDropdown.value);

        string jsonData = JsonUtility.ToJson(requestData);

        // --- Send API Request ---
        using (UnityWebRequest request = new UnityWebRequest(ApiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                statusText.text = $"Error: {request.error}";
                generateButton.interactable = true;
                yield break;
            }

            // --- Process API Response ---
            ResponseData responseData = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
            if (responseData.data == null || responseData.data.Length == 0)
            {
                statusText.text = "No image URL received in response.";
                generateButton.interactable = true;
                yield break;
            }

            string imageUrl = responseData.data[0].url;

            // --- Download and Display Image ---
            using (UnityWebRequest imageRequest = UnityWebRequestTexture.GetTexture(imageUrl))
            {
                yield return imageRequest.SendWebRequest();

                if (imageRequest.result != UnityWebRequest.Result.Success)
                {
                    statusText.text = $"Error downloading image: {imageRequest.error}";
                    generateButton.interactable = true;
                    yield break;
                }

                Texture2D texture = ((DownloadHandlerTexture)imageRequest.downloadHandler).texture;
                resultImage.texture = texture;
                resultImage.gameObject.SetActive(true);

                // --- Save Image to Device ---
                SaveImage(texture);
            }
        }
    }

    private void SaveImage(Texture2D texture)
    {
        byte[] bytes = texture.EncodeToJPG();
        string folderPath = Path.Combine(Application.persistentDataPath, OutputFolderName);

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        string fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
        string filePath = Path.Combine(folderPath, fileName);

        try
        {
            File.WriteAllBytes(filePath, bytes);
            statusText.text = $"Image saved to: {filePath}";
        }
        catch (Exception e)
        {
            statusText.text = $"Error saving image: {e.Message}";
        }
        finally
        {
            generateButton.interactable = true;
        }
    }

    private (int, int) GetResolution(int dropdownIndex)
    {
        switch (dropdownIndex)
        {
            case 0: return (1024, 1024); // 1:1
            case 1: return (1344, 768);  // 16:9
            case 2: return (1152, 896);  // 4:3
            case 3: return (768, 1344);  // 9:16
            default: return (1024, 1024);
        }
    }

    // --- Data Structures for JSON Serialization ---
    [System.Serializable]
    private class RequestData
    {
        public string prompt;
        public string model;
        public int steps;
        public int width;
        public int height;
    }

    [System.Serializable]
    private class ResponseData
    {
        public ImageInfo[] data;
    }

    [System.Serializable]
    private class ImageInfo
    {
        public string url;
    }
}