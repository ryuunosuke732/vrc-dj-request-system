
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using System;

namespace ryuu
{

    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class RequestController : UdonSharpBehaviour
    {


        [Header("Options")]
        [SerializeField]
        [Min(0)]
        [Tooltip("Seconds until user can submit another request.")]
        // Seconds!
        int requestCooldown = 90;

        [Tooltip("Animate deletion of requests.")]
        public bool animateDelete = false;

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


        void Start()
        {
            lastRequestTime = DateTime.Now.AddMinutes(-requestCooldown);
            createList();
        }

        private void Update()
        {
            string buttonText;
            double secondsTillRequest = requestCooldown - (DateTime.Now - lastRequestTime).TotalSeconds;
            if (secondsTillRequest < 0)
            {
                buttonText = "送る";
            }
            else
            {
                buttonText = string.Format("{0:00}:{1:00}", Math.Floor(secondsTillRequest / 60), Math.Floor(secondsTillRequest % 60));
            }
            submitButton.text = buttonText;
        }

        public void createList()
        {
            // ---- DECODE LOGIC! ----
            int i = 0;
            int k = 0;
            string[] requestData = new string[2];
            int dataCount = 0;
            int requestIndex = 1;
            while (k < synced_requests.Length)
            {
                if (synced_requests[k] == '>')
                {
                    int stringLength = int.Parse(synced_requests.Substring(i, k - i));
                    string parsingString = synced_requests.Substring(k + 1, stringLength);
                    requestData[dataCount] = parsingString;
                    i = k + stringLength + 1;
                    k = i;
                    dataCount += 1;
                }
                k++;

                if (dataCount == 2)
                {
                    dataCount = 0;
                    // Generate list tile
                    GameObject listTile = Instantiate(listTilePrefab, contentParent.transform);
                    listTile.transform.Find("Index").GetComponent<UnityEngine.UI.Text>().text = $"#{requestIndex.ToString()}";
                    listTile.transform.Find("Player Name").GetComponent<UnityEngine.UI.Text>().text = requestData[0];
                    listTile.transform.Find("Request").GetComponent<UnityEngine.UI.Text>().text = requestData[1];
                    GameObject deleteButton = listTile.transform.Find("Delete").gameObject;
                    deleteButton.GetComponent<RemoveRequestButton>().requestController = this;
                    deleteButton.GetComponent<RemoveRequestButton>().index = requestIndex;

                    listTile.transform.SetAsFirstSibling();
                    requestIndex += 1;
                }
            }
            // ------- DECODE ---------
        }

        public void wipeList()
        {
            for (int i = 0; i < contentParent.transform.childCount; i++)
            {
                Destroy(contentParent.transform.GetChild(i).gameObject);
            }
        }

        public void addRequest()
        {
            if (requestInputField.text.Length == 0)
            {
                return;
            }
            if ((DateTime.Now - lastRequestTime).TotalSeconds < requestCooldown)
            {
                return;
            }
            string encodedText = "";

            encodedText += $"{Networking.LocalPlayer.displayName.Length}>{Networking.LocalPlayer.displayName}";
            encodedText += $"{requestInputField.text.Length}>{requestInputField.text}";

            requestInputField.text = "";

            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            synced_requests += encodedText;
            lastRequestTime = DateTime.Now;

            RequestSerialization();
            OnDeserialization();
        }

        public void removeRequest(int remove_index)
        {
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            int i = 0;
            int k = 0;
            string[] requestData = new string[2];
            int dataCount = 0;
            int requestIndex = 0;
            int remove_start = 0;
            int remove_end = 0;
            while (k < synced_requests.Length)
            {
                if (synced_requests[k] == '>')
                {
                    int stringLength = int.Parse(synced_requests.Substring(i, k - i));
                    string parsingString = synced_requests.Substring(k + 1, stringLength);
                    requestData[dataCount] = parsingString;
                    i = k + stringLength + 1;
                    k = i;
                    dataCount += 1;
                }
                if (dataCount == 2)
                {
                    dataCount = 0;
                    requestIndex += 1;
                    remove_end = k;
                }
                if (requestIndex == remove_index && dataCount == 0)
                {
                    remove_start = k - (requestData[0].Length + requestData[0].Length.ToString().Length + requestData[1].Length + requestData[1].Length.ToString().Length + 2);
                    remove_end = k;
                    break;
                }
                k++;
            }
            Debug.Log($"[RyuuRequest] Removed request {synced_requests.Substring(remove_start, remove_end - remove_start)}");
            synced_requests = synced_requests.Substring(0, remove_start) + synced_requests.Substring(remove_end);
            RequestSerialization();
            OnDeserialization();
        }
        public void clearRequests()
        {
            synced_requests = "";
            wipeList();
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            RequestSerialization();
            OnDeserialization();
        }

        // enables/disables requests depending on `synced_requestsEnabled`.
        void updateUserAllowState()
        {
            worldUI.SetActive(synced_requestsEnabled);
        }

        public void toggleWorldInterface()
        {
            synced_requestsEnabled = !synced_requestsEnabled;
            if (!Networking.IsOwner(gameObject))
            {
                Networking.SetOwner(Networking.LocalPlayer, gameObject);
            }
            RequestSerialization();
            OnDeserialization();
        }

        public override void OnDeserialization()
        {
            wipeList();
            createList();
            updateUserAllowState();
        }

    }
}
