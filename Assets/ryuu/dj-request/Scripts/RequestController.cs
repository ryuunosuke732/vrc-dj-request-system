
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using System;

[UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
public class RequestController : UdonSharpBehaviour {


    [Header("Options")]
    [SerializeField]
    [Min(0)]
    [Tooltip("Seconds until user can submit another request.")]
    // Seconds!
    int requestCooldown = 180;

    [Space(10)]
    [Header("Internal")]

    DateTime lastRequestTime;

    [Space(5)]
    [Header("Client")]

    [SerializeField]
    UnityEngine.UI.InputField requestInputField;

    [SerializeField]
    UnityEngine.UI.Text submitButton;

    [Header("Display")]

    [SerializeField]
    GameObject contentParent;

    [SerializeField]
    GameObject listTilePrefab;


    [Header("Admin")]

    [SerializeField]
    GameObject worldUI;


    [UdonSynced]
    public bool synced_requestsEnabled;

    [UdonSynced]
    [HideInInspector]
    public string synced_requests = "";


    void Start() {
        lastRequestTime = DateTime.Now.AddMinutes(-requestCooldown);
        createList();
    }

    private void Update() {
        string buttonText;
        double secondsTillRequest = requestCooldown - (DateTime.Now - lastRequestTime).TotalSeconds;
        if (secondsTillRequest < 0) {
            buttonText = "Submit";
        } else {
            buttonText = string.Format("{0:00}:{1:00}", Math.Floor(secondsTillRequest / 60), Math.Floor(secondsTillRequest % 60));
        }
        submitButton.text = buttonText;
    }

    public void createList() {
        // ---- DECODE LOGIC! ----
        int i = 0;
        int k = 0;
        string[] requestData = new string[2];
        int dataCount = 0;
        int requestIndex = 1;
        while (k < synced_requests.Length) {
            if (synced_requests[k] == '>') {
                int stringLength = int.Parse(synced_requests.Substring(i, k - i));
                string parsingString = synced_requests.Substring(k + 1, stringLength);
                requestData[dataCount] = parsingString;
                i = k + stringLength + 1;
                k = i;
                dataCount += 1;
            }
            k++;

            if (dataCount == 2) {
                dataCount = 0;
                // Generate list tile
                GameObject listTile = Instantiate(listTilePrefab, contentParent.transform);
                listTile.transform.Find("Index").GetComponent<UnityEngine.UI.Text>().text = requestIndex.ToString();
                listTile.transform.Find("Player Name").GetComponent<UnityEngine.UI.Text>().text = requestData[0];
                listTile.transform.Find("Request").GetComponent<UnityEngine.UI.Text>().text = requestData[1];
                listTile.transform.SetAsFirstSibling();
                requestIndex += 1;
            }
        }
        // ------- DECODE ---------
    }

    public void wipeList() {
        for (int i = 0; i < contentParent.transform.childCount; i++) {
            Destroy(contentParent.transform.GetChild(i).gameObject);
        }
    }

    public void addRequest() {
        if (requestInputField.text.Length == 0) {
            return;
        }
        if ((DateTime.Now - lastRequestTime).TotalSeconds < requestCooldown) {
            return;
        }
        string encodedText = "";

        encodedText += $"{Networking.LocalPlayer.displayName.Length}>{Networking.LocalPlayer.displayName}";
        encodedText += $"{requestInputField.text.Length}>{requestInputField.text}";

        requestInputField.text = "";

        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        synced_requests += encodedText;
        lastRequestTime = DateTime.Now;

        RequestSerialization();
        OnDeserialization();
    }

    public void clearRequests() {
        synced_requests = "";
        wipeList();
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        RequestSerialization();
        OnDeserialization();
    }

    // enables/disables requests depending on `synced_requestsEnabled`.
    void updateUserAllowState() {
        worldUI.SetActive(synced_requestsEnabled);
    }

    public void toggleWorldInterface() {
        synced_requestsEnabled = !synced_requestsEnabled;
        if (!Networking.IsOwner(gameObject)) {
            Networking.SetOwner(Networking.LocalPlayer, gameObject);
        }
        RequestSerialization();
        OnDeserialization();
    }


    public override void OnDeserialization() {
        wipeList();
        createList();
        updateUserAllowState();
    }

}
