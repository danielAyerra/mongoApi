
using System.Reflection.Metadata.Ecma335;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DbCom.Models;
[BsonDiscriminator("AbstractModel")]
public class AbstractModel
{
    [BsonIgnore]
    [JsonProperty("TipoHijo")]
    protected string _TipoHijo;
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? ObjectId {get; set;}

    [BsonIgnore]
    public string TipoHijo{get;}
}