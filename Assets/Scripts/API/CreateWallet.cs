using UnityEngine;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Collections;
using System.Net.Http;
using UnityEngine.SceneManagement;

public class CreateWallet : MonoBehaviour
{
    // Replace this URL with your Node.js API server address
    public string apiBaseUrl = "http://localhost:3000";

    // Unique playerId for each player (We reference this in our SendTransaction.cs script)
    public string playerId;

    // The wallet address for the signer
    private string walletAddress;

    // Data class for the JSON payload
    [Serializable]
    public class RequestData
    {
        public string playerId;
    }

    // Data class for the response codes coming from the server
    [Serializable]
    public class ErrorResponse
    {
        public string error;
        public string message;
    }

    // Response class for GenerateWallet API endpoint
    [Serializable]
    public class GenerateWalletResponse
    {
        public string address;
    }

    // Singleton for us to have access to this script through other scripts
    public static CreateWallet instance;

    public void Start()
    {
        // Singleton
        instance = this;

        // If the connecting player doesn't have a playerId, we generate a new one and store it the playerPrefs to be reused
        if (!PlayerPrefs.HasKey("playerID"))
        {
            Debug.Log("No ID Found, Generating a new one");
            string newPlayerID = Guid.NewGuid().ToString();

            // Generate a unique playerId for the player and store it in playerPrefs
            PlayerPrefs.SetString("playerID", newPlayerID);
            playerId = newPlayerID;
        }
        else
        {
            playerId = PlayerPrefs.GetString("playerID");
        }
    }

    // We can call this function to await the creation of our new signer
    public async void InvokeWalletCreate()
    {
        await GenerateSigner();
    }

    // Invoke this method to generate a new signer and assign it to the playerID
    public async Task GenerateSigner()
    {
        Debug.Log("Your PlayerID: " + playerId);

        // Create the JSON payload with playerId
        var requestData = new RequestData
        {
            playerId = playerId
        };
        var json = JsonUtility.ToJson(requestData);
        var contentBytes = Encoding.UTF8.GetBytes(json);

        // Call our API endpoint ./generateWallet
        using (var request = UnityWebRequest.PostWwwForm(apiBaseUrl + "/generateWallet", ""))
        {
            // Set the request content type
            request.SetRequestHeader("Content-Type", "application/json");

            // Set the request data
            request.uploadHandler = new UploadHandlerRaw(contentBytes);

            // Send the request
            await request.SendWebRequest();

            Debug.Log("Generating Wallet for: " + playerId);

            if (request.result == UnityWebRequest.Result.Success)
            {
                var responseJson = request.downloadHandler.text;
                var responseData = JsonUtility.FromJson<GenerateWalletResponse>(responseJson);
                Debug.Log("Wallet generated for player " + playerId + " - Address: " + responseData.address);

                // Adding our wallet address variable
                walletAddress = responseData.address;

                // Provide gas to the created wallet (You'll need to configure your own gas distribution API for the below)
                // SKALE network does this best as their gas token has no value, so you can mine transactions and distribute gas for free.
                StartCoroutine(SendGas("https://corsproxy.io/?https://example-gas-api.onrender.com/claim/" + walletAddress));
            }
            else
            {
                {
                    // Check if there's an error in the response data
                    var responseJson = request.downloadHandler.text;
                    if (!string.IsNullOrEmpty(responseJson))
                    {
                        // Deserialize the JSON error message from the response
                        var errorData = JsonUtility.FromJson<ErrorResponse>(responseJson);
                        Debug.LogError("Error generating wallet: " + errorData.message);

                        // The only reason we'd get this error is to let us know we already have a signer, which means we can load the level
                        SceneManager.LoadScene("GameScene");
                    }
                    else
                    {
                        // If there's no response data or error message, display the generic error
                        Debug.LogError("Error generating wallet: " + request.result);
                    }
                }
            }
        }
    }


    // Our API call to refill a players gas if they are new or running low
    IEnumerator SendGas(string uri)
    {
        Debug.Log("Attempting to send gas to: " + walletAddress);

        UnityWebRequest webRequest = UnityWebRequest.Get(uri);
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            switch (webRequest.result)
            {
                case UnityWebRequest.Result.Success:
                    Debug.Log("Gas Request has been successful. Please wait...");

                    // If we successfully sent gas, we can load the next scene
                    new WaitForSeconds(3);

                    SceneManager.LoadScene("GameScene");
                    break;
                case UnityWebRequest.Result.ConnectionError:
                    Debug.Log("Connection Error!");
                    break;
            }
        }
    }





}
