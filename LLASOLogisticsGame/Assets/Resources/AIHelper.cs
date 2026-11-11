using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;   // your game uses the NEW Input System
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class AIHelper : MonoBehaviour
{
    // ---------- Easy access from other scripts ----------
    public static AIHelper Instance;
    public static bool IsChatOpen = false;   // other scripts check this so keys don't fire while typing

    // ---------- Things you drag in the Inspector ----------
    [Header("Chat UI")]
    public GameObject chatPanel;             // the panel that holds the chat box
    public TMP_InputField userInputField;
    public TextMeshProUGUI responseText;
    public Button sendButton;

    [Header("Auto Hint (optional)")]
    public TextMeshProUGUI hintText;         // small text on screen for auto hints
    public float stuckSeconds = 90f;         // no actions for this long = player might be stuck

    // ---------- Settings ----------
    private string apiKey;
    private const string apiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const string model = "openai/gpt-oss-20b";

    // EDIT THIS TEXT to teach the AI about your game
    private const string gameRules =
        "You are the AI helper inside a NASA training game called Pixel Packers. " +
        "The player packs cargo boxes into a lunar cargo hold for a 14-day mission. " +
        "Controls (keyboard / controller): E / A picks up or drops a box. Arrow keys / D-pad rotate the held box. " +
        "1 / X takes out or puts away the scanner. Q / ZR scans the box in the middle of the screen (needs the scanner out). " +
        "T / ZL stores the box's location (needs the scanner out AND the box must be scanned with Q first). " +
        "M / + opens the manifest screen. Tab / - switches between the MANIFEST app and the TRACKER app. " +
        "] and [ (R and L) change the day on the manifest or pick an item in the tracker. " +
        "F / Y makes the picked tracker item glow so the player can find it. K shows all keybinds. " +
        "Game rules: The manifest lists what to load each day, starting at Day 14 and counting down to Day 1. " +
        "Every box has a required zone (shown on the scanner info). The cargo hold has colored zone mats and zone signs. " +
        "Putting a box in the wrong zone flashes red and the tracker marks it WRONG ZONE, but the player is not blocked. " +
        "If the player drops a box in front of the right shelf level, it slides into the rack if it fits. " +
        "Otherwise the screen says DOES NOT FIT. " +
        "Rules for you: keep answers SHORT (1-3 sentences). Give hints, do not give the whole solution " +
        "unless the player asks directly. Be friendly. Only use facts from this message. If you are not sure, say so.";

    private List<Message> chatHistory = new List<Message>();
    private List<string> recentEvents = new List<string>();
    private float timeSinceLastEvent = 0f;
    private bool waiting = false;

    private StorageZone[] zones;
    private ManifestManager manifest;

    // ---------- JSON classes (same as your test scene) ----------
    [Serializable] public class Message { public string role; public string content; }
    [Serializable] public class ChatRequest { public string model; public List<Message> messages; }
    [Serializable] public class Choice { public Message message; }
    [Serializable] public class ChatResponse { public List<Choice> choices; }

    // ---------- Setup ----------
    void Awake()
    {
        Instance = this;
        IsChatOpen = false;

        // The key lives in Assets/Resources/groq_key.txt (NOT uploaded to GitHub)
        TextAsset keyFile = Resources.Load<TextAsset>("Groq_key");
        if (keyFile != null) apiKey = keyFile.text.Trim();
        else Debug.LogError("AIHelper: could not find Assets/Resources/groq_key.txt");
    }

    void Start()
    {
        zones = FindObjectsByType<StorageZone>(FindObjectsSortMode.None);
        manifest = FindFirstObjectByType<ManifestManager>();

        if (chatPanel != null) chatPanel.SetActive(false);
        if (sendButton != null) sendButton.onClick.AddListener(OnSend);
        if (userInputField != null) userInputField.onSubmit.AddListener(_ => OnSend());   // Enter key
        if (hintText != null) hintText.text = "";
    }

    void Update()
    {
        Keyboard k = Keyboard.current;
        if (k != null)
        {
            if (!IsChatOpen && k.hKey.wasPressedThisFrame) OpenChat();           // H opens
            else if (IsChatOpen && k.escapeKey.wasPressedThisFrame) CloseChat(); // Esc closes
        }

        // Stuck timer
        if (!IsChatOpen && !waiting)
        {
            timeSinceLastEvent += Time.deltaTime;
            if (timeSinceLastEvent >= stuckSeconds)
            {
                timeSinceLastEvent = 0f;
                StartCoroutine(AskForAutoHint());
            }
        }
    }

    // ---------- Open / close ----------
    void OpenChat()
    {
        chatPanel.SetActive(true);
        IsChatOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        StartCoroutine(FocusNextFrame());   // waits a frame so the "h" is not typed into the box
    }

    IEnumerator FocusNextFrame()
    {
        yield return null;
        if (IsChatOpen) userInputField.ActivateInputField();
    }

    void CloseChat()
    {
        chatPanel.SetActive(false);
        IsChatOpen = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------- "Watching" the player ----------
    // Other scripts call this:  AIHelper.Report("Player scanned box 12");
    public static void Report(string what)
    {
        if (Instance != null) Instance.AddEvent(what);
    }

    // Turns a box into words the AI can use, like: Suit (EVA-01, belongs in zone EVA)
    public static string Describe(ItemInfo item)
    {
        if (item == null) return "a box";
        ItemData d = InventoryDatabase.Instance != null ? InventoryDatabase.Instance.Get(item.itemId) : null;
        if (d == null) return "box " + item.itemId;
        return d.itemName + " (" + d.id + ", belongs in zone " + d.requiredZone + ")";
    }

    void AddEvent(string what)
    {
        recentEvents.Add(what);
        if (recentEvents.Count > 10) recentEvents.RemoveAt(0);   // only keep last 10
        timeSinceLastEvent = 0f;                                  // player did something
    }

    string BuildSystemPrompt()
    {
        StringBuilder sb = new StringBuilder(gameRules);

        if (zones != null && zones.Length > 0)
        {
            sb.Append("\n\nStorage zones in the hold (name and CSV zone code): ");
            foreach (StorageZone z in zones)
            {
                if (z != null) sb.Append(z.zoneName + " [" + z.zoneCode + "], ");
            }
        }

        if (manifest != null)
            sb.Append("\n\nThe manifest screen is on Day " + manifest.currentDay + " (days load from 14 down to 1).");

        if (recentEvents.Count > 0)
            sb.Append("\n\nWhat the player did recently (oldest first):\n- " + string.Join("\n- ", recentEvents));
        else
            sb.Append("\n\nThe player has not done anything yet.");

        return sb.ToString();
    }

    // ---------- Player asks a question ----------
    void OnSend()
    {
        if (waiting) return;
        string text = userInputField.text.Trim();
        if (text == "") return;

        userInputField.text = "";
        responseText.text = "Thinking...";

        Message userMsg = new Message { role = "user", content = text };

        List<Message> msgs = new List<Message>();
        msgs.Add(new Message { role = "system", content = BuildSystemPrompt() });
        msgs.AddRange(chatHistory);   // so the AI remembers the conversation
        msgs.Add(userMsg);

        StartCoroutine(SendToGroq(msgs, (reply, ok) =>
        {
            responseText.text = reply;
            if (ok)
            {
                chatHistory.Add(userMsg);
                chatHistory.Add(new Message { role = "assistant", content = reply });
                while (chatHistory.Count > 12) chatHistory.RemoveAt(0);
            }
            if (IsChatOpen) userInputField.ActivateInputField();
        }));
    }

    // ---------- Automatic hint when stuck ----------
    IEnumerator AskForAutoHint()
    {
        List<Message> msgs = new List<Message>();
        msgs.Add(new Message { role = "system", content = BuildSystemPrompt() });
        msgs.Add(new Message
        {
            role = "user",
            content = "The player has not done anything for a while and may be stuck. " +
                      "Based on what they did recently, give ONE short hint (max 2 sentences). Do not give the full answer."
        });

        yield return SendToGroq(msgs, (reply, ok) =>
        {
            if (ok && hintText != null && !IsChatOpen)
            {
                hintText.text = "Hint: " + reply;
                CancelInvoke(nameof(ClearHint));
                Invoke(nameof(ClearHint), 15f);   // hide after 15 seconds
            }
        });
    }

    void ClearHint()
    {
        if (hintText != null) hintText.text = "";
    }

    // ---------- Talk to Groq ----------
    IEnumerator SendToGroq(List<Message> msgs, Action<string, bool> onDone)
    {
        waiting = true;

        ChatRequest data = new ChatRequest { model = model, messages = msgs };
        string jsonBody = JsonUtility.ToJson(data);

        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);

            yield return request.SendWebRequest();

            waiting = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError(request.error + " | " + request.downloadHandler.text);
                onDone("Error connecting to AI.", false);
            }
            else
            {
                ChatResponse resp = JsonUtility.FromJson<ChatResponse>(request.downloadHandler.text);
                if (resp != null && resp.choices != null && resp.choices.Count > 0)
                    onDone(resp.choices[0].message.content, true);
                else
                    onDone("The AI sent an empty answer.", false);
            }
        }
    }
}