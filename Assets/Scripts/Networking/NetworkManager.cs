using UnityEngine; //Connection to Unity engine
using RiptideNetworking; //Allows use of Riptide Networking components
using RiptideNetworking.Utils; //Allows use of RiptideLogger to log networking error messages
//Enum to store the values for Server Tick, player location and movement that will be passed from the server to the client
public enum ServerToClientID : ushort
{
    sync = 1,
    playerSpawned,
    playerMovement,
}
//Enum to store the player name and input values to send to the server
public enum ClientToServerID : ushort
{
    name = 1,
    input,
}
public class NetworkManager : MonoBehaviour
{
    #region Variables
    //There can be only one!!! We will have a private instance and a public property to control the instance
    private static NetworkManager _networkManagerInstance;
    public static NetworkManager NetworkManagerInstance
    {
        //Property read is public by default and reads the set instance
        get => _networkManagerInstance;
        private set
        {
            //Property private write sets instance to the value if the instance is null
            if (_networkManagerInstance == null)
            {
               _networkManagerInstance = value;
            }
            //If we already have an instance log an error and destroy the new one
            else if (_networkManagerInstance != value)
            {
                Debug.LogWarning($"{nameof(NetworkManager)} instance already exists, destroy duplicate! THERE CAN BE ONLY ONE!");
                Destroy(value);
            }
        }
    }
    //Property to get and set the GameServer to connect to
    public Server GameServer { get; private set; }
    //Property to get and set the current server tick count defaulted to 0
    public ushort CurrentTick { get; private set; } = 0;
    [Header("Server Settings")]
    [Tooltip("Set the port to that will be used to communicate with the clients")]
    [SerializeField] private ushort s_port;
    [Tooltip("Set the maximum amount of clients allowed to connect")]
    [SerializeField] private ushort s_maxClientCount;
    #endregion
    #region Setup
    private void Awake()
    {
        //When the object that this script is on activates, set the instance to this
        NetworkManagerInstance = this;
    }
    private void Start()
    {
        //Set the target framerate to 60 to limit the amount of excess calculations needed
        Application.targetFrameRate = 60;
        //Logs what the network is doing for debugs, info, warnings and errors and sets to not include timestamps
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
        //Create new server
        GameServer = new Server();
        //Starts the server at the set port with the maximum amount of allowed clients
        GameServer.Start(s_port, s_maxClientCount);
        //When a client leaves the server, run the PlayerLeft function
        GameServer.ClientDisconnected += PlayerLeft;
    }
    #endregion
    #region Server Function
    //Checking Server activity at set intervals
    private void FixedUpdate()
    {
        //Run the game server's tick function to handle server functions and messages
        GameServer.Tick();
        //if the remainder of the currentTick divided by 200 is 0 then it is time to send sync data
        if (CurrentTick % 200 == 0)
        {
            SendSync();
        }
        //Increment the current tick counter
        CurrentTick++;
    }
    //When the game closes it kills the connection to the server
    private void OnApplicationQuit()
    {
        GameServer.Stop();
    }
    private void PlayerLeft(object sender, ClientDisconnectedEventArgs e)
    {
        //When a player leaves the server, destroy the player object and remove from list
        if (Player.list.TryGetValue(e.Id, out Player player)) Destroy(player.gameObject);
    }

    private void SendSync()
    {
        //Create a new message to send the sync data to the clients
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientID.sync);
        //Add the current tick count to the message
        message.Add(CurrentTick);
        //Send the message to all clients
        GameServer.SendToAll(message);
    }
    #endregion
}
