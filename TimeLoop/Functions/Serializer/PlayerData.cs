using System.Xml.Serialization;

namespace TimeLoop.Functions
{
    public class PlayerData
    {
        [XmlAttribute]
        public string ID;

        [XmlAttribute]
        public string PlayerName;

        [XmlAttribute]
        public bool SkipTimeLoop;


        public PlayerData() 
        {
            this.ID = "12345678901234567";
            this.PlayerName = "John Doe";
            this.SkipTimeLoop = false;            
        }


        public PlayerData(ClientInfo clientInfo)
        {
            this.ID = clientInfo.CrossplatformId.CombinedString;
            this.PlayerName = clientInfo.playerName;
            this.SkipTimeLoop = false;
        }
    }
}
