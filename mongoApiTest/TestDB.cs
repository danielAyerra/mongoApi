using DbComm;
using JsonConvert = Newtonsoft.Json.JsonConvert;
using Newtonsoft.Json;

namespace Tests;
[TestClass]

public class UnitTestDB{

    private readonly string connection = "mongodb://localhost:27017";
    private readonly string database = "MockData";

    [TestMethod]
    public void TestConnection(){
        Assert.IsTrue(MongoDbApi.Connect(connection, database));
    }

    [TestMethod]
    public void TestRegisterClassMaps(){
        Assert.IsTrue(MongoDbApi.RegisterClassMaps());
    }

    [TestMethod]
    public void CreationExample(){
        _=MongoDbApi.Connect(connection, database);
        Example ej = new Example(){Nombre="Prueba Delete", Nivel=(byte)NivelEnum.Dificil};
        Type type = typeof(Example);
        var settings = new JsonSerializerSettings{
            TypeNameHandling=TypeNameHandling.Auto,
            NullValueHandling=NullValueHandling.Include
        };
        string jsonEj = JsonConvert.SerializeObject(ej, Formatting.Indented, settings);
        Assert.IsTrue(MongoDbApi.Insert(jsonEj, type));
    }
    [TestMethod]
    public void CreationExamples(){
        _=MongoDbApi.Connect(connection, database);
        Example ej1 = new Example(){Nombre="Lista 1", Nivel=(byte)NivelEnum.Dificil};
        Example ej2 = new Example(){Nombre="Lista 2", Nivel=(byte)NivelEnum.Facil};
        List<Example> list = new List<Example>(){ej1, ej2};
        var settings = new JsonSerializerSettings{
            TypeNameHandling=TypeNameHandling.Auto,
            NullValueHandling=NullValueHandling.Include
        };
        string jsonList = JsonConvert.SerializeObject(list,Formatting.Indented, settings);
        Assert.IsTrue(MongoDbApi.InsertMany(jsonList, typeof(Example)));
    }

    [TestMethod]
    public void SelectExamples(){
        _=MongoDbApi.RegisterClassMaps();
        _=MongoDbApi.Connect(connection,database);
        List<AbstractModel> listaExamples;
        string paramName = nameof(Example.Nombre);
        string jsonFilter = $"{{'{paramName}':'Una prueba'}}";
        Assert.IsTrue(MongoDbApi.Select(out listaExamples,jsonFilter, typeof(Example)));
    }

    [TestMethod]
    public void UpdateExample(){
        _=MongoDbApi.RegisterClassMaps();
        _=MongoDbApi.Connect(connection,database);
        Example ej = new Example(){Nombre="Prueba Update", Nivel=(byte)NivelEnum.Medio}; 
        ej.ObjectId="";
        Dictionary<string, string> Valores = new Dictionary<string, string>();
        Valores.Add("Nombre", "Prueba Update Aprobada");
        Assert.IsTrue(MongoDbApi.Update(ej, typeof(Example).Name, Valores));

    }

    [TestMethod]
    public void UpdateExampleIdNull(){}
    [TestMethod]
    public void DeleteExample(){
        _=MongoDbApi.RegisterClassMaps();
        _=MongoDbApi.Connect(connection,database);
        Example ejercicio = new Example();
        ejercicio.ObjectId="";
        Assert.IsTrue(MongoDbApi.Delete(ejercicio,typeof(Example).Name));
    }

    [TestMethod]
    public void DeleteExampleIdNull(){}
}