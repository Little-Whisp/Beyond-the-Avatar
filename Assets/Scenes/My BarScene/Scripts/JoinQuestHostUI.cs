using UnityEngine;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class JoinQuestHostUI : MonoBehaviour
{
    [SerializeField] TMP_InputField ipInputField;

    public void OnJoinButtonPressed()
    {
        var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport as UnityTransport;
        if (transport == null) return;

        string ip = ipInputField.text.Trim();
        transport.ConnectionData.Address = ip;

        XRMultiplayer.XRINetworkGameManager.Instance.JoinLocalConnection();
    }
}
