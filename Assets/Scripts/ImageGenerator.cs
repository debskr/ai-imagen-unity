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
    public InputField promptInputField;
    public TMP_Dropdown resolutionDropdown;
    public Button generateButton;
    public RawImage[] resultImage;
    public TextMeshProUGUI statusText;

    // The base URL for Pollination AI image generation. The prompt and other parameters will be appended to this.
    private const string ApiUrl = "https://image.pollinations.ai/prompt/";
    private const string OutputFolderName = "AI_Generated_Images"; // The name of our public folder

    // --- Start and other UI methods (No changes here) ---
    private void Start()
    {
        generateButton.onClick.AddListener(OnGenerateButtonClick);
    }

    private void OnGenerateButtonClick()
    {
        StartCoroutine(GenerateImage());
    }

    // --- GenerateImage Coroutine (Revised to include a random seed) ---
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

        // Get the selected resolution
        (int width, int height) = GetResolution(resolutionDropdown.value);

        // --- URL Construction for Pollination AI (GET Request) ---
        // The prompt is URL-encoded and appended to the base URL.
        // Other parameters like width, height, model, and seed are added as query parameters.
        string encodedPrompt = UnityWebRequest.EscapeURL(prompt);

        // --- NEW: Generate a random seed for unique images ---
        int randomSeed = UnityEngine.Random.Range(0, 1000000); // Generate a random integer for the seed

        // Construct the full URL with all parameters, including the new random seed
        string fullUrl = $"{ApiUrl}{encodedPrompt}?width={width}&height={height}&model=turbo&seed={randomSeed}&enhance=true";

        Debug.Log("Requesting URL: " + fullUrl); // Log the URL for debugging purposes

        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullUrl))
        {

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                statusText.text = $"Error: {request.error}";
                generateButton.interactable = true;
                yield break;
            }

            // With Pollination AI, the response is the image itself, so we can directly get the texture.
            Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
            
            if(resolutionDropdown.value == 0)
            {
                resultImage[0].texture = texture;
                resultImage[0].gameObject.SetActive(true);
                resultImage[1].gameObject.SetActive(false);
                resultImage[2].gameObject.SetActive(false);
            }
            else if(resolutionDropdown.value == 1)
            {
                resultImage[1].texture = texture;
                resultImage[1].gameObject.SetActive(true);
                resultImage[0].gameObject.SetActive(false);
                resultImage[2].gameObject.SetActive(false);
            }
            else if(resolutionDropdown.value == 2)
            {
                resultImage[2].texture = texture;
                resultImage[2].gameObject.SetActive(true);
                resultImage[0].gameObject.SetActive(false);
                resultImage[1].gameObject.SetActive(false);
            }
            else
            {
                resultImage[0].texture = texture;
                resultImage[0].gameObject.SetActive(true);
                resultImage[1].gameObject.SetActive(false);
                resultImage[2].gameObject.SetActive(false);
            }

            //resultImage.texture = texture;
            //resultImage.gameObject.SetActive(true);
            Debug.Log(resolutionDropdown.value);
            // Save the downloaded image to the gallery
            SaveImageToGallery(texture);
        }
    }

    // --- COMPLETELY REWRITTEN SaveImageToGallery METHOD (No changes here) ---
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

    // --- GetResolution and data structures (No changes here, and Request/Response classes are not used) ---
    private (int, int) GetResolution(int dropdownIndex)
    {
        switch (dropdownIndex)
        {
            case 0: return (1280, 1280); // 1:1
            case 1: return (1600, 900);  // 16:9
            case 2: return (900, 1600);  // 9:16
            default: return (1280, 1280);
        }
    }

    // The following data structures are no longer needed for the Pollination AI implementation
    // as the request is a simple URL and the response is the image texture directly.
    // [System.Serializable] private class RequestData { public string prompt, model; public int steps, width, height; }
    // [System.Serializable] private class ResponseData { public ImageInfo[] data; }
    // [System.Serializable] private class ImageInfo { public string url; }
}