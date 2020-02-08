using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
using System.Net.Sockets;
using System.Net;

public class NetworkMan : MonoBehaviour
{
    public UdpClient udp;
    public GameObject cube;
    List<GameObject> cubes;
    Stack<string> newID;
    Stack<string> dropID;
    int spawnCounter;
    string ownedID;

    // Start is called before the first frame update
    void Start()
    {
        spawnCounter = 0;
        cubes = new List<GameObject>();
        newID = new Stack<string>();
        dropID = new Stack<string>();

        udp = new UdpClient();
        
        udp.Connect("3.86.206.4", 12345);

        Byte[] sendBytes = Encoding.ASCII.GetBytes("connect");
      
        udp.Send(sendBytes, sendBytes.Length);

        udp.BeginReceive(new AsyncCallback(OnReceived), udp);

        InvokeRepeating("HeartBeat", 1, 0.033f);
       // InvokeRepeating("SendPosition", 1, 0.033f);

    }

    void OnDestroy(){
        udp.Dispose();
    }

  
    public enum commands{
        NEW_CLIENT,
        UPDATE,
        CLIENT_LIST,
        DROP,
        OWN_ID
    };
    
    [Serializable]
    public class Message{
        public commands cmd;
    }
    
    [Serializable]
    public class Player{
        public string id;
        [Serializable]
        public struct receivedColor{
            public float R;
            public float G;
            public float B;
        }
        public receivedColor color;

        public Vector3 position;
        public Vector3 rotation;
    }
    [Serializable]
    class PlayerInfo
    {
        public Vector3 position;
        public Vector3 rotation;
    }
    [Serializable]
    public class OwnID
    {
        public Player ownID;
    }
    [Serializable]
    public class NewPlayer{
        public Player[] player;
    }

    [Serializable]
    public class GameState{
        public Player[] players;
    }

    public Message latestMessage;
    public GameState lastestGameState;
    void OnReceived(IAsyncResult result){
        // this is what had been passed into BeginReceive as the second parameter:
        UdpClient socket = result.AsyncState as UdpClient;
        
        // points towards whoever had sent the message:
        IPEndPoint source = new IPEndPoint(0, 0);

        // get the actual message and fill out the source:
        byte[] message = socket.EndReceive(result, ref source);
        
        // do what you'd like with `message` here:
        string returnData = Encoding.ASCII.GetString(message);
        Debug.Log("Got this: " + returnData);
        
        latestMessage = JsonUtility.FromJson<Message>(returnData);
        try{
            switch(latestMessage.cmd){
                
                case commands.NEW_CLIENT:
                    NewPlayer p = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach(var it in p.player)
                    {
                        newID.Push(it.id);
                    }
                    break;
                case commands.UPDATE:
                    lastestGameState = JsonUtility.FromJson<GameState>(returnData);
                    break;
                case commands.CLIENT_LIST:
                    NewPlayer np = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach (var it in np.player)
                    {
                        newID.Push(it.id);
                    }
                    Debug.Log("CLIENT_LIST");
                    break;
                case commands.DROP:
                    NewPlayer dp = JsonUtility.FromJson<NewPlayer>(returnData);
                    foreach (var it in dp.player)
                    {
                        dropID.Push(it.id);
                    }
                    Debug.Log("DROP");
                    break;
                case commands.OWN_ID:
                    OwnID oID = JsonUtility.FromJson<OwnID>(returnData);
                    ownedID = oID.ownID.id;
                    Debug.Log("OWNID");
                    break;
                default:
                    Debug.Log("Error");
                    break;
            }
        }
        catch (Exception e){
            Debug.Log(e.ToString());
        }
        
        // schedule the next receive operation once reading is done:
        socket.BeginReceive(new AsyncCallback(OnReceived), socket);
    }

    void SpawnPlayers(string id){
        foreach (var it in cubes)
        {
            if (it.GetComponent<NetworkID>().id == id)
                return;
        }

        GameObject temp = Instantiate(cube, new Vector3(-5 + spawnCounter, 0, 0), cube.transform.rotation);
        temp.GetComponent<NetworkID>().id = id; // this is this player's id
        if (id == ownedID)
        {
            temp.AddComponent<PlayerController>();
            temp.GetComponent<PlayerController>().netMan = this;
        }
        cubes.Add(temp);
        spawnCounter++;
    }

    void UpdatePlayers(){
        foreach(var it in cubes)
        {
            foreach (var p in lastestGameState.players)
            {
                if (it.GetComponent<NetworkID>().id == p.id)
                {
                    // change it to position & rotation
                    it.transform.position = p.position;
                    it.transform.eulerAngles = p.rotation;

                    //Color c = new Color(p.color.R, p.color.G, p.color.B);
                    //it.GetComponent<Renderer>().material.SetColor("_Color", c);
                }
            }
        }
    }

    void DestroyPlayers(string id){
        for(int i = 0; i < cubes.Count; i++)
        {
            var it = cubes[i];
            if (it.GetComponent<NetworkID>().id == id)
            {
                var temp = it;
                cubes.Remove(it);
                Destroy(temp);
            }
        }

    }

  
    void HeartBeat(){
        Byte[] sendBytes = Encoding.ASCII.GetBytes("heartbeat");
        udp.Send(sendBytes, sendBytes.Length);
    }

    public void SendPosition(Vector3 pos, Vector3 rot)
    {
        PlayerInfo info = new PlayerInfo();
        info.position = pos;
        info.rotation = rot;
        string jsonString = JsonUtility.ToJson(info);
        Debug.Log(jsonString);
        Byte[] sendBytes = Encoding.ASCII.GetBytes(jsonString);
        udp.Send(sendBytes, sendBytes.Length);
    }

    void Update(){
        if (newID.Count > 0)
        {
            for(int i = 0; i < newID.Count; i++)
            {
                var it = newID.Pop();
                SpawnPlayers(it);
            }
        }
        UpdatePlayers();

        if (dropID.Count > 0)
        {
            for (int i = 0; i < dropID.Count; i++)
            {
                var it = dropID.Pop();
                DestroyPlayers(it);
            }
        }
    }
}