using UnityEngine; //Connect to Unity Engine

public class GameLogic : MonoBehaviour
{
    #region Variables
    //There can be only one!!! We will have a private instance and a public property to control the instance
    private static GameLogic _gameLogicInstance;
    public static GameLogic GameLogicInstance
    {
        //Property read is public by default and reads the set instance
        get => _gameLogicInstance;
        private set
        {
            //Property private write sets instance to the value if the instance is null
            if (_gameLogicInstance == null)
            {
                //If we don't have an instance already, make this one the instance.
                _gameLogicInstance = value;
            }
            else if (_gameLogicInstance != value)
            {
                //Send warning that we already have an instance created if we try to load another one.
                Debug.LogWarning($"{nameof(NetworkManager)} instance already exists, destroy duplicate! THERE CAN BE ONLY ONE!");
                //Destroy the new instance because it is not the real Jet Li
                Destroy(value);
            }
        }
    }
    [Header("Prefabs")]
    [Tooltip("Add the prefab to use as the player objects")]
    [SerializeField] private GameObject _playerPrefab;
    //Public property allows other classes to get the prefab but not change it
    public GameObject PlayerPrefab => _playerPrefab;
    #endregion
    private void Awake()
    {
        //Sets the singleton to this
        GameLogicInstance = this;
    }
}
