using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YuGiOh_TTS_Deck_Builder
{
    class TTS
    {
        public List<ObjectState> ObjectStates { get; set; }
    }

    public class ObjectStateTransform
    {
        public int posX = 0;
        public int posY = 1;
        public int posZ = 0;
        public int rotX = 0;
        public int rotY = 180;
        public int rotZ = 180;
        public int scaleX = 1;
        public int scaleY = 1;
        public int scaleZ = 1;
    }

    public class ContainedObjectTransform
    {
        public int posX = 0;
        public int posY = 0;
        public int posZ = 0;
        public int rotX = 0;
        public int rotY = 180;
        public int rotZ = 180;
        public int scaleX = 1;
        public int scaleY = 1;
        public int scaleZ = 1;
    }

    public class ContainedObject
    {
        public string Name { get; set; }
        public string Nickname { get; set; }
        public ContainedObjectTransform Transform;
        public int CardID { get; set; }
    }

    public class DeckInfo
    {
        public int NumWidth { get; set; }
        public int NumHeight { get; set; }
        public string FaceURL { get; set; }
        public string BackURL { get; set; }
    }

    public class CustomDeck
    {
        [JsonProperty("1")]
        public DeckInfo one { get; set; }

        [JsonProperty("2")]
        public DeckInfo two { get; set; }

        [JsonProperty("3")]
        public DeckInfo three { get; set; }
    }

    public class ObjectState
    {
        public ObjectStateTransform Transform { get; set; }
        public string Name { get; set; }
        public List<ContainedObject> ContainedObjects { get; set; }
        public List<int> DeckIDs { get; set; }
        public CustomDeck CustomDeck { get; set; }
    }
}
