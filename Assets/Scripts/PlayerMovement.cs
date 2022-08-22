using RiptideNetworking; //Use Riptide networking components
using UnityEngine; //Connect to unity engine
//Require a character controller to be connected to the game oject this script is attached to
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    #region Variables
    [Header("Component References")]
    [Tooltip("Drag the player class from this game object into this field")]
    [SerializeField] private Player _player;
    [Tooltip("Drag the character controller component into this field")]
    [SerializeField] private CharacterController _controller;
    [Header("Camera")]
    [Tooltip("Drag the camera or proxy camera object that is a child of this game object into this field")]
    [SerializeField] private Transform _camProxy;
    [Header("Movement")]
    [Tooltip("Set the strength of gravity for the scene")]
    [SerializeField] private float _gravity;
    [Tooltip("Set the speed for the characters to move at")]
    [SerializeField] private float _movementSpeed;
    [Tooltip("Set the height for players to jump to")]
    [SerializeField] private float _jumpHeight;
    //Variables for calculated gravity, movement and jump values
    private float _gravityAcceleration;
    private float _moveSpeed;
    private float _jumpSpeed;
    //A bool array to store the user inputs that will be sent by the user
    private bool[] inputs;
    //A float to calculate and store the velocity of jumping taking into account gravity 
    private float yVelocity;
    #endregion
    #region Setup
    private void OnValidate()
    {
        //If we dont have character controller and player class stored retrieve them from the game object
        if (_controller == null) _controller = GetComponent<CharacterController>();
        if (_player == null) _player = GetComponent<Player>();
        //Run the Initialize function
        Initialize();
    }
    private void Start()
    {
        //Run the Initialize function
        Initialize();
        //Set the value of inputs to a new array of 6 values
        inputs = new bool[6];
    }
    private void Initialize()
    {
        //Gravity acceleration equals the gravity value multiplied twice by fixed deltatime
        _gravityAcceleration = _gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;
        //Speed of movement equals the set movement speed multiplied by fixed deltatime
        _moveSpeed = _movementSpeed * Time.fixedDeltaTime;
        //Jump speed is the square root of the jump height value multiplied by -2f and by gravity acceleration
        _jumpSpeed = Mathf.Sqrt(_jumpHeight * -2f * _gravityAcceleration);
    }
    #endregion
    #region Movement
    private void FixedUpdate()
    {
        //Set the inputs to zero at the beginning of each fixed update
        Vector2 inputDirection = Vector2.zero;
        //Retrieve values from the bools array and for any true values add the corresponding value to our input movement value
        if (inputs[0])
        {
            inputDirection.y += 1;
        }
        if (inputs[1])
        {
            inputDirection.y -= 1;
        }
        if (inputs[2])
        {
            inputDirection.x -= 1;
        }
        if (inputs[3])
        {
            inputDirection.x += 1;
        }
        //Run the move function passing through the input value and the remaining bools for jumping and sprinting
        Move(inputDirection, inputs[4], inputs[5]);
    }    
    private void Move(Vector2 inputDirection, bool jump, bool sprint)
    {
        //New move direction value equals a normalized value from the inputs with a y value that is set to 0 in the FlattenVector3 method
        Vector3 moveDirection = Vector3.Normalize(_camProxy.right * inputDirection.x + Vector3.Normalize(FlattenVector3(_camProxy.forward)) * inputDirection.y);
        //Multiply the direction by the move speed to determine distance to move
        moveDirection *= _moveSpeed;
        //If we are sprinting double the movement distance
        if (sprint)
        {
            moveDirection *= 2f;
        }
        //If we are grounded and we have a jump input our y velocity equals the jump speed
        if (_controller.isGrounded)
        {
            yVelocity = 0f;
            if (jump)
            {
                yVelocity = _jumpSpeed;
            }
        }
        //Add gravity to the jump velocity to help control the jump height and allow object to come back down
        yVelocity += _gravityAcceleration;
        //our y direction is the new y velocity
        moveDirection.y = yVelocity;
        //Move in the direction we have now calculated
        _controller.Move(moveDirection);
        //Run the SendMovement function to send the movement information to the client
        SendMovement();
    }
    private Vector3 FlattenVector3(Vector3 vector)
    {
        //Make the y axis of the vector3 equal 0 and return the vector3 
        vector.y = 0;
        return vector;
    }
    public void SetInput(bool[] inputs, Vector3 forward)
    {
        //Recieve the inputs array sent from the client and make it the bool array to use in this class
        this.inputs = inputs;
        //Camera facing direction is the Vector3 direction passed from the client
        _camProxy.forward = forward;
    }
    #endregion
    #region Messages
    private void SendMovement()
    {
        //Set an if statement to return out of this function every second tick so we only send messages once every second tick
        if (NetworkManager.NetworkManagerInstance.CurrentTick % 2 != 0) return;
        //Create new message to send the movement information
        Message message = Message.Create(MessageSendMode.unreliable, (ushort)ServerToClientID.playerMovement);
        //Add the players ID to the message
        message.AddUShort(_player.Id);
        //Add the current server tick count to the message
        message.AddUShort(NetworkManager.NetworkManagerInstance.CurrentTick);
        //Add the transform position of the object after having moved to the message
        message.AddVector3(transform.position);
        //Add the forward direction vector3 to the message
        message.AddVector3(_camProxy.forward);
        //Use the network manager to send the message to all clients
        NetworkManager.NetworkManagerInstance.GameServer.SendToAll(message);
    }
    #endregion
}
