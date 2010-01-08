#light

namespace FunctionalDivan 

module internal RecordMapping =
    open Microsoft.FSharp.Reflection
    open Microsoft.FSharp.Collections
    open System.Reflection
    
    open Newtonsoft.Json.Linq
    open Newtonsoft.Json

    let internal normalizeName = function
    | "rev" -> "_rev"
    | "id" -> "_id"
    | _ as v -> v

    let internal denormalizeName = function
    | "_rev" -> "rev"
    | "_id" -> "id"
    | _ as v -> v

    let private listGenerator = System.Type.GetType("FunctionalDivan.RecordMapping").GetMethod("makeGenericList", BindingFlags.NonPublic ||| BindingFlags.Static)
    let private arrayGenerator = System.Type.GetType("FunctionalDivan.RecordMapping").GetMethod("makeGenericArray", BindingFlags.NonPublic ||| BindingFlags.Static)
    
    let private makeGenericList<'a> (size: int) elemType (generator: int -> obj) = 
        List.init size (fun i -> (generator i) :?> 'a)

    let private makeGenericArray<'a> (size: int) elemType (generator: int -> obj) = 
        Array.init size (fun i -> (generator i) :?> 'a)

    let rec readJson recordType (jObj: JObject) =
        let t = FSharpType.GetRecordFields(recordType)

        let rec readValue targetType (value: JToken) asList = 
            match value with
            | :? JObject as nestedObj -> readJson targetType nestedObj
            | :? JArray as nestedArray -> 
                (if asList then listGenerator else arrayGenerator)
                    .MakeGenericMethod([|targetType|]).Invoke(
                        null, 
                        [|(nestedArray.Count); recordType;
                            (fun i -> 
                                match nestedArray.[i] with
                                | :? JValue as v -> v.Value
                                | _ as other -> readValue targetType other false)|])
            | :? JValue as v -> v.Value
            | _ as unk -> failwith <| sprintf "%A is an unsupported node type" unk

        let values = 
            Array.init 
                t.Length
                (fun i ->
                    let pType = t.[i].PropertyType
                    if (pType.IsArray) then readValue (pType.GetElementType()) (jObj.[normalizeName t.[i].Name]) false
                    elif pType.IsGenericType then readValue (pType.GetGenericArguments().[0]) (jObj.[normalizeName t.[i].Name]) true
                    else readValue (pType) (jObj.[normalizeName t.[i].Name]) false
                )
                
        FSharpValue.MakeRecord(recordType, values)

    let rec writeJson record (writer: JsonWriter) includeType =
        if includeType then 
            writer.WritePropertyName("record_type")
            writer
                .WriteValue(record.GetType()
                .Name
                .Substring(
                            System.Math.Max(
                                            System
                                                .Math
                                                .Max(record.GetType().Name.LastIndexOf("."), record.GetType().Name.LastIndexOf("+")),
                                            0)))
        else ()
    
        let rec writeValue (value: obj) = 
            if value = null then () 
            else
                if FSharpType.IsRecord(value.GetType()) then
                    writer.WriteStartObject()
                    writeJson value writer false
                    writer.WriteEndObject()
                elif not (value.GetType() = typeof<System.String>) && typeof<System.Collections.IEnumerable>.IsAssignableFrom(value.GetType()) then
                    writer.WriteStartArray()
                    for elem in (value :?> System.Collections.IEnumerable) do writeValue elem
                    writer.WriteEndArray()
                else
                    writer.WriteValue value

        let t = FSharpType.GetRecordFields(record.GetType())
        
        for prop in t do
            match prop.GetValue(record, null) with
            | null -> ()
            | _ as v ->
                writer.WritePropertyName (normalizeName prop.Name)
                writeValue <| v
                
