
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace DbCom.Models;
[JsonObject(ItemTypeNameHandling =TypeNameHandling.Auto)]
[BsonDiscriminator("Example")]
public class Example:AbstractModel
{

    [BsonRepresentation(BsonType.String)]
    public string Name {get;set;}
    [BsonRepresentation(BsonType.Int32)]
    public byte Age {get;set;}
    [BsonRepresentation(BsonType.String)]  
    public string Surname {get;set;}

    [BsonConstructor]
    public Example(string name, byte age, string surname){
        this.Name=name;
        this.Age=age;
        this.Surname=surname;
        this._ChildType=typeof(Example).Name;
    }
    [BsonConstructor]
    public Example()
    {
        this._ChildType=typeof(Example).Name;
    }
}
