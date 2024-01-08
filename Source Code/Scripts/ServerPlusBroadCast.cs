using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ServerSide : MonoBehaviour
{
    [SerializeField] private Text ServerIpText;
    IPEndPoint localEndPoint;
    public class ClientStates
    {
        public Socket clientSocket = null;
        public bool isConnected = false;
        public byte[] sendData = new byte[20];
        public byte[] recvData = new byte[20];
    }

    public int noofPlayers;

    List<ClientStates> clients = new List<ClientStates>();

    public struct playerInfo
    {
        public Vector2 playerPosition;
        public float boxPosition;
    }

    private Socket serverSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
    Rigidbody2D playerBody, boxBody;
    GameObject boxObject;
    playerInfo localPlayer;
    public byte[] sendData = new byte[20];
    string externalIp;
    float timePassed = 0f;

    public void Awake()
    {
        playerBody = this.gameObject.GetComponent<Rigidbody2D>();
        boxObject = GameObject.FindWithTag("Box");
        boxBody = boxObject.GetComponent<Rigidbody2D>(); 
    }
    public async void Start()
    {
        setupIPAddress();
        Debug.Log($"External IP Address: {localEndPoint}");
        await SetUpServerAsync();
        playerBody.position = Vector3.zero;
        localPlayer.playerPosition = playerBody.position;
        localPlayer.boxPosition = boxBody.position.x;
    }

    public async void SendDataToClients(ClientStates client)
    {
        client.sendData = Serialize<playerInfo>(localPlayer);
        client.clientSocket.SendAsync(sendData, SocketFlags.None);
    }

    public void setupIPAddress()
    {
        IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[1];
        Debug.Log(IPAddress.Parse(ipAddress.ToString()));
        localEndPoint = new IPEndPoint(ipAddress, 7777);
    }

    public async void Update()
    {
        HandlePlayerInput(playerBody);
        localPlayer.playerPosition = playerBody.position;
        localPlayer.boxPosition = boxBody.position.x;
        sendData = Serialize<playerInfo>(localPlayer);
        foreach (ClientStates client in clients)
        {
            try
            {
                if (client.clientSocket.Connected)
                {
                    timePassed += Time.deltaTime;
                    if (timePassed > 0.3f)
                    {
                        SendDataToClients(client);
                        timePassed = 0f;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("Client has disconnected " + e);
                clients.Remove(client);
            }
        }
    }

    public static string GetLocalIPAddress()  //obtains external IP address of the server for connection
    {
            IPHostEntry serverHost = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in serverHost.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return "127.0.0.1"; //return local host ip otherwise
    }

    public async Task SetUpServerAsync()
    {
        serverSocket.Bind(localEndPoint);
        serverSocket.Listen(10);
        Debug.Log("Beginning to listen for clients");
        ServerIpText.text = $"IP to spectate: {localEndPoint}";
        while (true)
        {
            Socket clientSocket = await serverSocket.AcceptAsync();
            _ = HandleClientAsync(clientSocket);
        }
    }

    public void OnApplicationQuit()
    {
        Debug.Log("Shutting down server");
        serverSocket.Shutdown(SocketShutdown.Both);
        serverSocket.Close();
    }

    public async Task HandleClientAsync(Socket clientSocket)
    {
        Debug.Log("Client connected " + clientSocket.RemoteEndPoint);
        clients.Add(new ClientStates { clientSocket = clientSocket, isConnected = true });
        //SendDataToClients();
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

   public void HandlePlayerInput(Rigidbody2D body)
    {
        float dirX = Input.GetAxisRaw("Horizontal");
        body.velocity = new Vector2(dirX * 7f, body.velocity.y);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            body.velocity = new Vector2(body.velocity.x, 7);
        }
    }
}


