using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
    private string apiKey = "";
    private const string apiEndpoint = "https://api.groq.com/openai/v1/chat/completions";
    private const string model = "openai/gpt-oss-20b";
    private const int maxNotesLength = 2000;   // the notes file is cut to this many letters

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
        "unless the player asks directly. Be friendly. " +
        "The section called 'Right now in the game' was written by the game itself and is TRUE. Use it. " +
        "Only use facts from this message. If you are not sure, say so.";

    private List<Message> chatHistory = new List<Message>();
    private List<string> recentEvents = new List<string>();
    private float timeSinceLastEvent = 0f;
    private bool waiting = false;

    // Things the helper looks at (found automatically, nothing to drag in)
    private StorageZone[] zones;
    private ManifestManager manifest;
    private ItemInspector inspector;
    private ScannerToggle scanner;
    private GridContainer grid;
    private ItemTracker tracker;

    private string notes = "";
    private string logPath;

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
        LoadKey();
        LoadNotes();
        logPath = Path.Combine(Application.persistentDataPath, "ai_log.txt");
    }

    void Start()
    {
        zones = FindObjectsByType<StorageZone>(FindObjectsSortMode.None);
        manifest = FindFirstObjectByType<ManifestManager>();
        inspector = FindFirstObjectByType<ItemInspector>();
        scanner = FindFirstObjectByType<ScannerToggle>();
        grid = FindFirstObjectByType<GridContainer>();
        tracker = ItemTracker.Instance;

        if (chatPanel != null) chatPanel.SetActive(false);
        if (sendButton != null) sendButton.onClick.AddListener(OnSend);
        if (userInputField != null) userInputField.onSubmit.AddListener(_ => OnSend());   // Enter key
        if (hintText != null) hintText.text = "";
    }

    // Reads Assets/Resources/groq_key.txt and cleans it up
    void LoadKey()
    {
        TextAsset keyFile = Resources.Load<TextAsset>("groq_key");
        if (keyFile == null)
        {
            Debug.LogError("AIHelper: can't find groq_key.txt in Assets/Resources. " +
                           "(If Unity's Project window shows it as 'groq_key.txt' WITH the .txt, its real name is groq_key.txt.txt. Rename it.)");
            return;
        }

        // Use the first line that has something on it, and remove quotes and spaces
        string key = "";
        foreach (string line in keyFile.text.Split('\n'))
        {
            string t = line.Trim().Trim('"', '\'', '\uFEFF').Trim();
            if (t != "") { key = t; break; }
        }

        // In case "Bearer " got pasted in front
        if (key.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) key = key.Substring(7).Trim();

        apiKey = key;

        if (apiKey.Length == 0)
            Debug.LogError("AIHelper: groq_key.txt is empty.");
        else if (!apiKey.StartsWith("gsk_"))
            Debug.LogWarning("AIHelper: your key does not start with gsk_ . Open groq_key.txt and make sure it holds ONLY the key.");
        else
            Debug.Log("AIHelper: key loaded (" + apiKey.Length + " characters).");
    }

    // Reads Assets/Resources/ai_notes.txt (the team's tips for the AI)
    void LoadNotes()
    {
        TextAsset notesFile = Resources.Load<TextAsset>("ai_notes");
        if (notesFile == null) return;

        notes = notesFile.text.Trim();
        if (notes.Length > maxNotesLength)
        {
            Debug.LogWarning("AIHelper: ai_notes.txt is longer than " + maxNotesLength + " letters. The end is ignored. Shorten it.");
            notes = notes.Substring(0, maxNotesLength);
        }
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

    // ---------- The "screen in words" ----------
    string BuildSnapshot()
    {
        StringBuilder sb = new StringBuilder();

        ItemInfo held = inspector != null ? inspector.heldItem : null;
        Camera cam = (inspector != null && inspector.playerCamera != null) ? inspector.playerCamera : Camera.main;

        // Scanner
        if (scanner != null)
            sb.AppendLine("Scanner: " + (scanner.IsEquipped ? "out" : "PUT AWAY (Q and T do not work until it is taken out with 1 / X)"));

        // Hands and crosshair
        sb.AppendLine("Holding: " + (held != null ? Describe(held) + ScanNote(held) : "nothing"));

        ItemInfo looking = GetLookedAtBox(cam);
        if (looking != null && looking != held)
            sb.AppendLine("Crosshair is on: " + Describe(looking) + ScanNote(looking));

        // What the scanner panel says right now
        if (inspector != null && inspector.canvasGroup != null && inspector.infoText != null && inspector.canvasGroup.alpha > 0.5f)
            sb.AppendLine("Scanner panel on screen says: " + Shorten(Clean(inspector.infoText.text), 350));

        // Where the player is
        if (cam != null)
        {
            if (grid != null)
                sb.AppendLine("Player is " + (grid.IsInside(cam.transform.position) ? "inside" : "outside") + " the cargo hold.");

            StorageZone nearest = null;
            float best = 6f;
            if (zones != null)
            {
                foreach (StorageZone z in zones)
                {
                    if (z == null) continue;
                    float d = Vector3.Distance(cam.transform.position, z.transform.position);
                    if (d < best) { best = d; nearest = z; }
                }
            }
            if (nearest != null)
                sb.AppendLine("Nearest rack: " + nearest.zoneName + " [" + nearest.zoneCode + "], about " + best.ToString("0") + " m away");
        }

        // Manifest
        if (manifest != null)
        {
            sb.AppendLine("Manifest screen: " + (manifest.IsOpen ? "OPEN on the " + manifest.CurrentApp + " app" : "closed") +
                          ", set to Day " + manifest.currentDay);

            List<ItemData> dayItems = manifest.GetItemsForDay(manifest.currentDay);
            sb.Append("Manifest items for Day " + manifest.currentDay + " (" + dayItems.Count + "): ");
            for (int i = 0; i < dayItems.Count && i < 10; i++)
                sb.Append(dayItems[i].id + " " + Shorten(dayItems[i].itemName, 30) + "; ");
            if (dayItems.Count > 10) sb.Append("and " + (dayItems.Count - 10) + " more");
            sb.AppendLine();
        }

        // Progress and problems
        List<TrackedRecord> records = tracker != null ? tracker.GetRecords() : new List<TrackedRecord>();
        HashSet<ItemInfo> stored = new HashSet<ItemInfo>();
        foreach (TrackedRecord r in records) stored.Add(r.item);

        ItemInfo[] allBoxes = FindObjectsByType<ItemInfo>(FindObjectsSortMode.None);
        int notScanned = 0;
        int notStored = 0;
        foreach (ItemInfo box in allBoxes)
        {
            if (tracker != null && !tracker.HasScanned(box)) notScanned++;
            if (!stored.Contains(box)) notStored++;
        }
        sb.AppendLine("Boxes in the scene: " + allBoxes.Length + ". Not scanned yet: " + notScanned + ". Location not stored yet: " + notStored + ".");

        sb.AppendLine("Checks done by the game (these are true):");
        bool anyCheck = false;

        if (scanner != null && !scanner.IsEquipped)
        {
            sb.AppendLine("- The scanner is put away.");
            anyCheck = true;
        }
        if (held != null && tracker != null && !tracker.HasScanned(held))
        {
            sb.AppendLine("- The held box has NOT been scanned with Q yet.");
            anyCheck = true;
        }

        int shown = 0;
        foreach (TrackedRecord r in records)
        {
            if (shown >= 5) break;
            if (r.wrongZone)
            {
                sb.AppendLine("- WRONG ZONE: " + r.itemName + " (" + r.id + ") is at " + r.location + " but belongs in zone " + r.requiredZone + ".");
                anyCheck = true;
                shown++;
            }
            else if (tracker.CurrentLocation(r) != r.location)
            {
                sb.AppendLine("- MOVED since it was stored: " + r.itemName + " (" + r.id + "). It needs a rescan.");
                anyCheck = true;
                shown++;
            }
        }
        if (!anyCheck) sb.AppendLine("- No problems found.");

        // Recent actions
        if (recentEvents.Count > 0)
            sb.AppendLine("What the player did recently (oldest first):\n- " + string.Join("\n- ", recentEvents));
        else
            sb.AppendLine("The player has not done anything yet.");

        return sb.ToString();
    }

    string ScanNote(ItemInfo item)
    {
        if (tracker == null) return "";
        string note = tracker.HasScanned(item) ? " [scanned]" : " [NOT scanned yet]";
        foreach (TrackedRecord r in tracker.GetRecords())
        {
            if (r.item == item) { note += " [location stored: " + r.location + "]"; break; }
        }
        return note;
    }

    ItemInfo GetLookedAtBox(Camera cam)
    {
        if (cam == null) return null;
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 4f))
            return hit.collider.GetComponentInParent<ItemInfo>();
        return null;
    }

    // Removes the <b> <color> style tags from screen text
    string Clean(string text)
    {
        if (text == null) return "";
        return Regex.Replace(text, "<.*?>", "").Replace("\n", " | ").Trim();
    }

    string Shorten(string text, int max)
    {
        if (text.Length <= max) return text;
        return text.Substring(0, max) + "...";
    }

    string BuildSystemPrompt(string snapshot)
    {
        StringBuilder sb = new StringBuilder(gameRules);

        if (notes != "")
            sb.Append("\n\nTeam notes from past playtests (trust these):\n" + notes);

        if (zones != null && zones.Length > 0)
        {
            sb.Append("\n\nStorage zones in the hold (name and CSV zone code): ");
            foreach (StorageZone z in zones)
            {
                if (z != null) sb.Append(z.zoneName + " [" + z.zoneCode + "], ");
            }
        }

        sb.Append("\n\nRight now in the game:\n" + snapshot);
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

        string snapshot = BuildSnapshot();
        Message userMsg = new Message { role = "user", content = text };

        List<Message> msgs = new List<Message>();
        msgs.Add(new Message { role = "system", content = BuildSystemPrompt(snapshot) });
        msgs.AddRange(chatHistory);   // so the AI remembers the conversation
        msgs.Add(userMsg);

        StartCoroutine(SendToGroq(msgs, (reply, ok) =>
        {
            responseText.text = reply;
            if (ok)
            {
                WriteLog("QUESTION", text, reply, snapshot);
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
        string snapshot = BuildSnapshot();
        string ask = "The player has not done anything for a while and may be stuck. " +
                     "Based on what the game sees, give ONE short hint (max 2 sentences). Do not give the full answer.";

        List<Message> msgs = new List<Message>();
        msgs.Add(new Message { role = "system", content = BuildSystemPrompt(snapshot) });
        msgs.Add(new Message { role = "user", content = ask });

        yield return SendToGroq(msgs, (reply, ok) =>
        {
            if (!ok) return;
            WriteLog("AUTO HINT", "(player was idle)", reply, snapshot);

            if (hintText != null && !IsChatOpen)
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

    // ---------- The log file ----------
    void WriteLog(string kind, string question, string answer, string snapshot)
    {
        try
        {
            string entry =
                "==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  [" + kind + "] ====\n" +
                "Player: " + question + "\n" +
                "AI: " + answer + "\n" +
                "--- what the game saw ---\n" + snapshot + "\n";
            File.AppendAllText(logPath, entry);
        }
        catch (Exception e)
        {
            Debug.LogWarning("AIHelper: could not write the log. " + e.Message);
        }
    }

    // Right-click the AIHelper script in the Inspector and pick "Open log folder"
    [ContextMenu("Open log folder")]
    void OpenLogFolder()
    {
        Debug.Log("AIHelper log folder: " + Application.persistentDataPath);
        Application.OpenURL("file://" + Application.persistentDataPath);
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

                if (request.responseCode == 401)
                    onDone("AI key problem (401). Check Assets/Resources/groq_key.txt.", false);
                else
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