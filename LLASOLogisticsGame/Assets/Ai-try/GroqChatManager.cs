using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class GroqChatManager : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string apiKey = "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";
    [SerializeField] private string apiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    [SerializeField] private string model = "openai/gpt-oss-20b";

    [Header("UI")]
    public TMP_InputField userInputField;
    public TextMeshProUGUI responseText;
    public Button sendButton;

    [Header("Coach")]
    [TextArea(4, 8)]
    [SerializeField]
    private string coachInstructions =
        "You are the packing coach inside a NASA HUNCH LASSO training simulation. " +
        "Help the player stow cargo correctly. Use the packing state included in this prompt. " +
        "Give one short hint. Reveal the full procedure only when the player asks for the answer. " +
        "If the packing state is missing, say which fact you need from the simulation.";

    [TextArea(4, 10)]
    [SerializeField] private string packingState = "No packing state has been reported yet.";

    [SerializeField] private int maxHistoryMessages = 12;

    private readonly List<Message> history = new List<Message>();
    private bool requestInFlight;

    [Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [Serializable]
    public class ChatRequest
    {
        public string model;
        public List<Message> messages;
    }

    [Serializable]
    public class Choice
    {
        public Message message;
    }

    [Serializable]
    public class ChatResponse
    {
        public List<Choice> choices;
    }

    void Start()
    {
        if (sendButton != null)
            sendButton.onClick.AddListener(OnSendButtonClicked);
    }

    void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
    }

    // Call this from your packing scripts whenever the task changes.
    public void SetPackingState(string state)
    {
        if (!string.IsNullOrWhiteSpace(state))
            packingState = state;
    }

    // Call this from a "I'm stuck" button, or from your own stuck timer.
    public void OfferHint()
    {
        Ask("I am stuck. Use the current packing state and give me one hint for the next action.");
    }

    public void Ask(string playerText)
    {
        if (requestInFlight || string.IsNullOrWhiteSpace(playerText))
            return;

        StartCoroutine(SendChat(playerText.Trim()));
    }

    void OnSendButtonClicked()
    {
        if (userInputField == null || string.IsNullOrWhiteSpace(userInputField.text))
            return;

        string playerText = userInputField.text;
        userInputField.text = "";
        Ask(playerText);
    }

    IEnumerator SendChat(string playerText)
    {
        requestInFlight = true;
        if (sendButton != null)
            sendButton.interactable = false;
        if (responseText != null)
            responseText.text = "Thinking...";

        history.Add(new Message { role = "user", content = playerText });
        TrimHistory();

        var messages = new List<Message>
        {
            new Message
            {
                role = "system",
                content = coachInstructions + "\n\nCurrent packing state:\n" + packingState
            }
        };
        messages.AddRange(history);

        ChatRequest requestData = new ChatRequest
        {
            model = model,
            messages = messages
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.timeout = 60;
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                history.RemoveAt(history.Count - 1);
                if (responseText != null)
                    responseText.text = "Error connecting to AI.";
                Debug.LogError(request.responseCode + " " + request.error + "\n" + request.downloadHandler.text);
            }
            else
            {
                ChatResponse responseObj = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                string answer = ReadAnswer(responseObj);
                if (string.IsNullOrEmpty(answer))
                {
                    history.RemoveAt(history.Count - 1);
                    if (responseText != null)
                        responseText.text = "The AI reply could not be read.";
                    Debug.LogError(request.downloadHandler.text);
                }
                else
                {
                    history.Add(new Message { role = "assistant", content = answer });
                    TrimHistory();
                    if (responseText != null)
                        responseText.text = answer;
                }
            }
        }

        requestInFlight = false;
        if (sendButton != null)
            sendButton.interactable = true;
    }

    static string ReadAnswer(ChatResponse responseObj)
    {
        if (responseObj == null || responseObj.choices == null || responseObj.choices.Count == 0)
            return null;
        if (responseObj.choices[0].message == null)
            return null;
        return responseObj.choices[0].message.content;
    }

    void TrimHistory()
    {
        int extra = history.Count - maxHistoryMessages;
        if (extra > 0)
            history.RemoveRange(0, extra);
    }
}