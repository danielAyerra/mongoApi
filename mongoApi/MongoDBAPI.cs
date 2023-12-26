namespace DbCom;
using Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

public static class MongoDbApi
{
    private static MongoClient _mongoClient;
    private static IMongoDatabase _mongoDatabase;

    private static readonly JsonSerializerSettings _settings=new JsonSerializerSettings{
                TypeNameHandling=TypeNameHandling.Auto,
                NullValueHandling=NullValueHandling.Include
            };

    public static bool RegisterClassMaps(){
        if(!BsonClassMap.IsClassMapRegistered(typeof(AbstractModel)))
            BsonClassMap.RegisterClassMap<AbstractModel>(cm =>
            {
                cm.AutoMap();
                cm.SetDiscriminatorIsRequired(true);
            });
        if(!BsonClassMap.IsClassMapRegistered(typeof(Ejercicio)))
            BsonClassMap.RegisterClassMap<Ejercicio>();
        if(!BsonClassMap.IsClassMapRegistered(typeof(Usuario)))
            BsonClassMap.RegisterClassMap<Usuario>();
        return true;
    }

    public static bool Connect(string connection, string database){
        bool connOk;
        try{
            _mongoClient=new MongoClient(connection);
            _mongoDatabase=_mongoClient.GetDatabase(database);
            connOk=true;
        }catch(Exception e){
            PrintFail("Error al conectar base de datos: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            connOk=false;
        }
        return connOk;    
    }

    public static bool Insert(string jsonString, Type type){   
        bool insertOk;
        try{          
            AbstractModel retrievedData = GetDataObjectFromJsonString(jsonString, type);
            var collection = _mongoDatabase.GetCollection<AbstractModel>(type.Name);
            collection.InsertOne(retrievedData);
            insertOk=true;
        }
        catch(Exception e){
            PrintFail("Error al insertar en la colección: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            insertOk=false; 
        }
        return insertOk;
    }

    private static AbstractModel GetDataObjectFromJsonString(string jsonString, Type type)
    {
        JObject jObj=JObject.Parse(jsonString);
        string tipo = jObj["TipoHijo"].ToString();
        if(type.Name!=tipo)
            throw new Exception("Tipos no coinciden");
        AbstractModel retrievedObject =(AbstractModel)jObj.ToObject(type);
        return retrievedObject;
    }

    public static bool InsertMany (string jsonString, Type type){
        bool insertOk;
        try{
            List<AbstractModel> data = GetDataListFromJsonString(jsonString, type);
            var collection = _mongoDatabase.GetCollection<AbstractModel>(type.Name);
            collection.InsertMany(data);
            insertOk=true;
        }
        catch(Exception e){
            PrintFail("Error al insertar en la colección: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            insertOk=false;
        }
        return insertOk;
    }

    private static List<AbstractModel> GetDataListFromJsonString(string jsonString, Type type)
    {
        JArray jArr=JArray.Parse(jsonString);
        List<AbstractModel> elements = new List<AbstractModel>();
        foreach(JObject jObj in jArr){
            string tipo = jObj["TipoHijo"].ToString();
            if(type.Name!=tipo)
                throw new Exception("Tipos no coinciden");
            AbstractModel retrievedObject=(AbstractModel)jObj.ToObject(type);
            elements.Add(retrievedObject);
        }
        return elements;
    }

    public static bool Select(out List<AbstractModel> data, string? jsonParams, Type type){
        bool selectOk;
        data = new List<AbstractModel>();
        try{
            
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            List<BsonDocument> findObject;
            BsonDocument filterDocument=composeFilter(jsonParams);
            findObject = collection.Find<BsonDocument>(filterDocument).ToList<BsonDocument>();
            foreach(BsonDocument bsonElement in findObject){
                data.Add(BsonSerializer.Deserialize<AbstractModel>(bsonElement));
            }
            selectOk=true;
        }catch(Exception e){
            PrintFail("Error al buscar en la colección: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            selectOk=false;
            data=new List<AbstractModel>();
        }
        return selectOk; 
    }
    
    public static bool Update(object data, Type type, Dictionary<string, string> newVals){
        bool updateOk;
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            AbstractModel oldObject = (AbstractModel)data;
            if(oldObject.ObjectId!=null){
                ObjectId id = new ObjectId(oldObject.ObjectId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                List<UpdateDefinition<BsonDocument>> updateParams = new List<UpdateDefinition<BsonDocument>>();
                foreach(string key in newVals.Keys){
                    updateParams.Add(Builders<BsonDocument>.Update.Set(key, newVals[key]));
                }
                var updater = Builders<BsonDocument>.Update.Combine(updateParams);
                collection.UpdateOne(filter, updater);
            }else{
                return false;
            }
            updateOk=true;
        }catch(Exception e){
            PrintFail("Error al actualizar el objeto: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            updateOk=false;
        }
        return updateOk;
    }
    
    public static bool UpdateMany(Type type, string jsonParams, Dictionary<string,string> newVals){
        bool updateManyOk;
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            BsonDocument filter = composeFilter(jsonParams);
            List<UpdateDefinition<BsonDocument>> updateParams = new List<UpdateDefinition<BsonDocument>>();
            foreach(string key in newVals.Keys){
                updateParams.Add(Builders<BsonDocument>.Update.Set(key, newVals[key]));
            }
            var updater = Builders<BsonDocument>.Update.Combine(updateParams);
            collection.UpdateMany(filter, updater);
            updateManyOk= true;
        }catch(Exception e){
            PrintFail("Error al actualizar objetos: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            updateManyOk=false;
        }
        return updateManyOk;
    }
    
    public static bool Delete(object data, string collectionName){
        bool deleteOk;
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(collectionName);
            AbstractModel objectToDelete = (AbstractModel) data;
            if(objectToDelete.ObjectId!=null){
                ObjectId id = new ObjectId(objectToDelete.ObjectId);
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                collection.DeleteOne(filter);
            }else{
                throw new Exception("El objeto no existe en la base de datos");
            }
            deleteOk= true;
        }catch(Exception e){
            PrintFail("Error al borrar el objeto: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            deleteOk=false;
        }
        return deleteOk;
    }
    
    public static bool DeleteMany(Type type, string jsonParams){
        bool deleteManyOk;
        try{
            var collection=_mongoDatabase.GetCollection<BsonDocument>(type.Name);
            BsonDocument filter = composeFilter(jsonParams);
            collection.DeleteMany(filter);
            deleteManyOk=true;
        }
        catch(Exception e){
             PrintFail("Error al borrar los objetos: ", e.Message);
            if(e.Source!=null)
                PrintFail("Origen: ", e.Source);
            if(e.StackTrace!=null)
                PrintFail("Pila de llamadas: ", e.StackTrace);
            deleteManyOk=false;
        }
        return deleteManyOk;
    }
    
    public static BsonDocument composeFilter(string jsonParams){
        JObject jsonObject = JObject.Parse(jsonParams);
        BsonDocument filter = BsonDocument.Parse(jsonObject.ToString());
        return filter;
    }
    
    private static void PrintFail(string message, string exceptionMessage){
        Console.Write(message);
        Console.WriteLine(exceptionMessage);
        Console.WriteLine();
    }

}
