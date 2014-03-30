using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace MyApp.Services
{
    //The Serializable attributes are only necessary because the serializer being used by ServiceProxy.Zmq is the BinaryFormatter
    //This will be removed when/if support is added for different serializers
    //Note: ServiceProxy itself has no dependencies or notions of serialization

    [Serializable]
    [DataContract]
    public class Catalog
    {
        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public IEnumerable<Item> Items { get; set; }
    }

    [Serializable]
    [DataContract]
    public class Item
    {
        [DataMember]
        public string Code { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public decimal Price { get; set; }
    }

    [Serializable]
    [DataContract]
    public class ItemDetails
    {
        [DataMember]
        public Item Item { get; set; }

        [DataMember]
        public DateTime DateTimeField { get; set; }
        [DataMember]
        public int IntField { get; set; }
        [DataMember]
        public long LongField { get; set; }
        [DataMember]
        public double? NullableDoubleField { get; set; }
        [DataMember]
        public Guid GuidField { get; set; }
        [DataMember]
        public IEnumerable<RandomType> RandomTypeField { get; set; }
    }

    [Serializable]
    [DataContract]
    public class RandomType
    {
        [DataMember]
        public int[] IntArrayField { get; set; }
        [DataMember]
        public char CharField { get; set; }
    }
}
