using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using System.Threading.Tasks;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    async void Start()
    {
        try
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log("Signed in. Player ID: " + AuthenticationService.Instance.PlayerId);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Unity Services Initialization or SignIn failed: " + e.Message);
        }
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Debug.Log("Creating Relay Allocation...");
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(1);

            if (alloc == null)
            {
                Debug.LogError("CreateAllocation returned null!");
                return null;
            }

            Debug.Log("Allocation created. ID: " + alloc.AllocationId);

            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
            Debug.Log("Join code received: " + joinCode);

            var relayData = new RelayServerData(alloc, "udp");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport not found on NetworkManager!");
                return null;
            }

            transport.SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();

            Debug.Log("Host started successfully.");

            SceneManager.LoadScene("Game");

            return joinCode;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Relay Host Error: " + e.Message);
            return null;
        }
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log("Joining Relay with code: " + joinCode);
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);

            if (joinAlloc == null)
            {
                Debug.LogError("JoinAllocation returned null!");
                return false;
            }

            Debug.Log("Join allocation successful. Allocation ID: " + joinAlloc.AllocationId);

            var relayData = new RelayServerData(joinAlloc, "udp");

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport not found on NetworkManager!");
                return false;
            }

            transport.SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            Debug.Log("Client started successfully.");

            SceneManager.LoadScene("Game");

            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Relay Join Error: " + e.Message);
            return false;
        }
    }
}
