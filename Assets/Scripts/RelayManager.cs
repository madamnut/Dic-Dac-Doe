using UnityEngine;
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
        await UnityServices.InitializeAsync();
        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            Allocation alloc = await RelayService.Instance.CreateAllocationAsync(1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);

            var relayData = new RelayServerData(alloc, "udp");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
            NetworkManager.Singleton.StartHost();

            Debug.Log("Hosting with Join Code: " + joinCode);
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
            JoinAllocation joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
            var relayData = new RelayServerData(joinAlloc, "udp");
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayData);
            NetworkManager.Singleton.StartClient();

            Debug.Log("Client joined with code: " + joinCode);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Relay Join Error: " + e.Message);
            return false;
        }
    }
}
