using System.Collections.Generic; //Allow the use of dictionaries
using UnityEngine; //Connect to Unity Engine
using RiptideNetworking; //Use Riptide networking components

public class Player : MonoBehaviour
{
    #region Variables
    //A Dictionary to store the list of players connected to the server
    public static Dictionary<ushort, Player> list = new Dictionary<ushort, Player>();
    //Property to get and set player ID's
    public ushort Id { get; private set; }
    //Property to get and set player usernames
    public string Username { get; private set; }
    //Property so other classes can get but not modify the movement class
    public PlayerMovement Movement => movement;
    [Header("Component References")]
    [Tooltip("Add the player movement class from this game object to this field")]
    [SerializeField] private PlayerMovement movement;
    #endregion
    #region Spawn & Destroy
    public static void Spawn(ushort id, string username)
    {
        //For each player in the list send the players ID to the new client using the SendSpawned function
        foreach (Player otherPlayer in list.Values) otherPlayer.SendSpawned(id);
        //Instantiate a player using the set prefab and store it's player class
        Player player = Instantiate(GameLogic.GameLogicInstance.PlayerPrefab, new Vector3(0,1,0), Quaternion.identity).GetComponent<Player>();
        //Set the name of the player to either the username or to Guest if a username is not available
        player.name = $"Player{id}({(string.IsNullOrEmpty(username) ? "Guest" : username)})";
        //Set the new players ID to match the one given
        player.Id = id;
        //Set the username of the player either to the username or to Guest if no username was given        
        player.Username = string.IsNullOrEmpty(username) ? "Guest" : username;
        //Use the sendspawned function to send the list of existing players to the new client
        player.SendSpawned();
        //Add the new player to the dictionary
        list.Add(id, player);
    }
    private void OnDestroy()
    {
        //When the object is destroyed remove its entry from the dictionary
        list.Remove(Id);
    }
    #endregion
    #region Messages

    private void SendSpawned()
    {
        //Use the network manager to send a message to all clients with the information of the new client that joined the game
        NetworkManager.NetworkManagerInstance.GameServer.SendToAll(AddSpawnData(Message.Create(MessageSendMode.reliable, (ushort)ServerToClientID.playerSpawned)));
    }
    private void SendSpawned(ushort toClientId)
    {
        //Use the network manager to send a message to the new client with the user information of the current clients
        NetworkManager.NetworkManagerInstance.GameServer.Send(AddSpawnData(Message.Create(MessageSendMode.reliable, (ushort)ServerToClientID.playerSpawned)), toClientId);
    }

    private Message AddSpawnData(Message message)
    {
        //Add the users ID to the message
        message.AddUShort(Id);
        //Add the users username to the message
        message.AddString(Username);
        //Add the users current position to the message
        message.AddVector3(transform.position);
        //Return the message for sending
        return message;
    }
    //Message handler to spawn an object using the ID and name sent by the client
    [MessageHandler((ushort)ClientToServerID.name)]
    private static void Name(ushort fromClientID, Message message)
    {
        Spawn(fromClientID, message.GetString());
    }
    //Message handler to receive the player inputs from the client and pass it to the relevant players SetInput method
    [MessageHandler((ushort)ClientToServerID.input)]
    private static void Input(ushort fromClientId, Message message)
    {
        if (list.TryGetValue(fromClientId, out Player player)) player.Movement.SetInput(message.GetBools(6), message.GetVector3());
    }
    #endregion
}
