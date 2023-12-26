
using System.Reflection.Metadata.Ecma335;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DbCom.Models;
[BsonDiscriminator("AbstractModel")]
public class AbstractModel
{
    [BsonIgnore]
    [JsonProperty("ChildType")]
    protected string _ChildType;
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ObjectId {get; set;}

    [BsonIgnore]
    public string ChildType{get;}
}