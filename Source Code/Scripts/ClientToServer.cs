using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using System.Threading.Tasks;
using Unity.VisualScripting;
using JetBrains.Annotations;
using System.Text;
using System.Net.NetworkInformation;
using UnityEngine.UI;
using Unity.Mathematics;
using UnityEngine.UIElements;

public class ClientToServer : MonoBehaviour
{
    [SerializeField] TMPro.TMP_InputField AddresServer;
    [SerializeField] public UnityEngine.UI.Button connectButton;
    Rigidbody2D playerBody, remotePlayerBody;
    public GameObject myPrefab;
    Rigidbody2D boxBody;
    //public InputField getIpAddress;
    GameObject remotePlayer, boxObject;
    bool valueUpdated,localPlayerSpawn = false;
    float timePassed = 0f;
    int count = 0;


    private Vector2 predictedPosition;
    private float predictionTime = 0.5f;
    public struct PlayerInfo
    {
        public Vector2 playerPosition;
        public float boxPosition;
    }

    private Socket clientsocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    private byte[] recBuffer = new byte[20];
    private byte[] sendBuffer = new byte[20];
    PlayerInfo pInfo, oInfo;
    int numberOfPlayers;

    public async void Start()
    {
        //AddresServer.ActivateInputField();
        //Debug.Log(connectButton.isActiveAndEnabled);
        //AddresServer.
        //connectButton.onClick.AddListener(ConnectToServer);

    }

    public async void ConnectToServer()
    {
        string serverIP = AddresServer.text;
        Debug.Log(serverIP);
        await ConnectClientAsync(serverIP);
    }
    public async void FixedUpdate()
    {
        try
        {
            if (clientsocket.Connected)
            {
                await clientsocket.ReceiveAsync(recBuffer, SocketFlags.None);
                oInfo = Deserialize<PlayerInfo>(recBuffer);

                if (valueUpdated == false)
                {
                    SpawnPlayer(oInfo.playerPosition);
                }
                else
                {
                    if (count < 3)
                    {
                        InterpolateBetween(remotePlayerBody.position, oInfo.playerPosition);
                        count++;
                    }
                    else
                    {
                        predictedPosition = PredictionMethod(remotePlayerBody.position, oInfo.playerPosition);
                        Debug.Log($"Predicted Position: {predictedPosition}");
                        //InterpolateBetween(remotePlayerBody.position, predictedPosition);
                        InterpolateBetween(remotePlayerBody.position, predictedPosition);
                        count = 0;
                    }
                    boxBody.MovePosition(new Vector2(oInfo.boxPosition, boxBody.position.y));
                }

            }
        }
        catch(Exception ex)
        {
            // Close the client socket when done
            clientsocket.Close();
        }
    }

    public void GetIpAddress()
    {
        //getIpAddress.text.ToString();
    }

    public Vector2 PredictionMethod(Vector2 lastPosition, Vector2 currentPosition)
    {
        Vector2 velocity = (currentPosition - lastPosition) / Time.deltaTime;
        return currentPosition + velocity * Time.deltaTime;
    }

    public async Task ConnectClientAsync(String serverIP)
    {
        Debug.Log($"Connecting to server at IP: {serverIP}");

        try
        {
            await clientsocket.ConnectAsync(new IPEndPoint(IPAddress.Parse(serverIP), 7777));
            Debug.Log("Connected to server");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to connect to server: {ex.Message}");
        }
    }

    public void SpawnPlayer(Vector2 playerPosition)
    {
        remotePlayer = Instantiate(myPrefab, new Vector3(playerPosition.x, playerPosition.y, 0) , Quaternion.identity);
        boxBody = this.gameObject.GetComponent<Rigidbody2D>();
        remotePlayerBody = remotePlayer.GetComponent<Rigidbody2D>();
        valueUpdated = true;
    }

    public void OnApplicationQuit()
    {
        //clientsocket.Shutdown(SocketShutdown.Both);
        //clientsocket.Close();
    }

    public async void InterpolateBetween(Vector2 lastPosition, Vector2 currentPosition)
    {
        float speed = math.abs(currentPosition.magnitude - lastPosition.magnitude)/Time.deltaTime;
        remotePlayerBody.position = Vector2.Lerp(lastPosition, currentPosition, Time.deltaTime*speed);
    }

    public static unsafe byte[] Serialize<T>(T value) where T : unmanaged
    {
        byte[] buffer = new byte[sizeof(T)];

        fixed (byte* bufferPtr = buffer)
        {
            Buffer.MemoryCopy(&value, bufferPtr, sizeof(T), sizeof(T));
        }

        return buffer;
    }

    public static unsafe T Deserialize<T>(byte[] buffer) where T : unmanaged
    {
        T result = new T();

        fixed (byte* bufferPtr = buffer)
        {
            Buffer.MemoryCopy(bufferPtr, &result, sizeof(T), sizeof(T));
        }

        return result;
    }
}
