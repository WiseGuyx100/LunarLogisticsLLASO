using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro; // Include if using TextMeshPro
using UnityEngine.UI;

public class GroqChatManager : MonoBehaviour
{
    private string apiKey = "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";
    private string apiEndpoint = "https://api.groq.com/openai/v1/chat/completions";

    [Header("UI References")]
    public TMP_InputField userInputField;
    public TextMeshProUGUI responseText;
    public Button sendButton;

    [Serializable]
    public class Message { public string role; public string content; }
    [Serializable]
    public class ChatRequest { public string model; public List<Message> messages; }
    [Serializable]
    public class Choice { public Message message; }
    [Serializable]
    public class ChatResponse { public List<Choice> choices; }

    void Start()
    {
        // Hook up the button click event automatically
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
    }

    void OnSendButtonClicked()
    {
        if (!string.IsNullOrEmpty(userInputField.text))
        {
            string playerText = userInputField.text;
            userInputField.text = ""; // Clear input field
            StartCoroutine(SendChatToGroq(playerText));
        }
    }

    public IEnumerator SendChatToGroq(string userPrompt)
    {
        responseText.text = "Thinking...";

        ChatRequest requestData = new ChatRequest
        {
            model = "openai/gpt-oss-20b",
            messages = new List<Message>
            {
                new Message { role = "system", content = "You are a witty NPC in a video game." },
                new Message { role = "user", content = userPrompt }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                responseText.text = "Error connecting to AI.";
                Debug.LogError(request.error);
            }
            else
            {
                ChatResponse responseObj = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                if (responseObj != null && responseObj.choices.Count > 0)
                {
                    responseText.text = responseObj.choices[0].message.content;
                }
            }
        }
    }
}