using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.IO;
using System;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class ImageGenerator : MonoBehaviour
{
    // --- UI Elements (No changes here) ---
    [Header("UI Elements")]
    public TMP_InputField promptInputField;
    public TMP_Dropdown resolutionDropdown;
    public Button generateButton;
    public RawImage resultImage;
    public TextMeshProUGUI statusText;
    
    // --- API Configuration (No changes here) ---
    [Header("API Configuration")]
    private string apiKey = ""; // Replace with your Together AI API key
    private const string ApiKeyPref = "UserApiKey";
    private const string ApiUrl = "https://api.together.xyz/v1/images/generations";
    private const string OutputFolderName = "AI_Generated_Images"; // The name of our public folder

    // --- Start and other UI methods (No changes here) ---
    private void Start()
    {
        generateButton.onClick.AddListener(OnGenerateButtonClick);

        if(PlayerPrefs.HasKey(ApiKeyPref))
        {
            apiKey = PlayerPrefs.GetString(ApiKeyPref);
        }
        else
        {
            statusText.text = "API Key not set. Please enter your API Key in the Inspector.";
            generateButton.interactable = false;
        }
    }

    private void OnGenerateButtonClick()
    {
        StartCoroutine(GenerateImage());
    }

    // --- GenerateImage Coroutine (Minor change to call the new Save method) ---
    private IEnumerator GenerateImage()
    {
        string prompt = promptInputField.text;
        if (string.IsNullOrEmpty(prompt))
        {
            statusText.text = "Please enter a prompt.";
            yield break;
        }

        generateButton.interactable = false;
        statusText.text = "Generating image...";

        var requestData = new RequestData
        {
            prompt = prompt,
            model = "black-forest-labs/FLUX.1-schnell-Free",
            steps = 4,
        };

        (requestData.width, requestData.height) = GetResolution(resolutionDropdown.value);
        string jsonData = JsonUtility.ToJson(requestData);

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

            ResponseData responseData = JsonUtility.FromJson<ResponseData>(request.downloadHandler.text);
            if (responseData.data == null || responseData.data.Length == 0)
            {
                statusText.text = "No image URL received in response.";
                generateButton.interactable = true;
                yield break;
            }

            string imageUrl = responseData.data[0].url;
            statusText.text = "Downloading image...";

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

                // This now calls the new, corrected Save method
                SaveImageToGallery(texture);
            }
        }
    }

    // --- COMPLETELY REWRITTEN SaveImageToGallery METHOD ---
    private void SaveImageToGallery(Texture2D texture)
    {
        statusText.text = "Saving image...";
        string fileName = $"AI-Image-{DateTime.Now:yyyyMMdd_HHmmss}.jpg";

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            // Use Android's MediaStore API
            AndroidJavaClass mediaStoreClass = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
            AndroidJavaObject contentValues = new AndroidJavaObject("android.content.ContentValues");
            
            // Add metadata for the image
            contentValues.Call("put", mediaStoreClass.GetStatic<string>("DISPLAY_NAME"), fileName);
            contentValues.Call("put", mediaStoreClass.GetStatic<string>("MIME_TYPE"), "image/jpg");
            // This line is crucial for Scoped Storage. It specifies the subfolder within the public Pictures directory.
            contentValues.Call("put", mediaStoreClass.GetStatic<string>("RELATIVE_PATH"), "Pictures/" + OutputFolderName);

            // Get the ContentResolver and insert the new image record
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject currentActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject contentResolver = currentActivity.Call<AndroidJavaObject>("getContentResolver");
            
            AndroidJavaObject uri = contentResolver.Call<AndroidJavaObject>("insert", mediaStoreClass.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI"), contentValues);

            // Open an output stream to write the image data
            AndroidJavaObject outputStream = contentResolver.Call<AndroidJavaObject>("openOutputStream", uri);

            byte[] jpgBytes = texture.EncodeToJPG();
            outputStream.Call("write", jpgBytes);
            outputStream.Call("close");

            statusText.text = $"Image saved to Pictures/{OutputFolderName}";
            Debug.Log($"Image saved successfully to gallery: {uri.Call<string>("toString")}");
        }
        catch (Exception e)
        {
            statusText.text = "Error saving image: Check Logcat.";
            Debug.LogError($"Error saving image to gallery: {e.Message}\n{e.StackTrace}");
        }
        finally
        {
            generateButton.interactable = true;
        }

#else // Fallback for Unity Editor and other platforms
        try
        {
            string folderPath = Path.Combine(Application.persistentDataPath, OutputFolderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, fileName);
            File.WriteAllBytes(filePath, texture.EncodeToJPG());

            statusText.text = $"Image saved to: {filePath}";
            Debug.Log($"Image saved to: {filePath}");
        }
        catch (Exception e)
        {
            statusText.text = "Error saving image.";
            Debug.LogError($"Error saving image: {e.Message}");
        }
        finally
        {
            generateButton.interactable = true;
        }
#endif
    }

    // --- GetResolution and data structures (No changes here) ---
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

    [System.Serializable] private class RequestData { public string prompt, model; public int steps, width, height; }
    [System.Serializable] private class ResponseData { public ImageInfo[] data; }
    [System.Serializable] private class ImageInfo { public string url; }
}