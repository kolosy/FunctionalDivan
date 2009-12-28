#light

namespace FunctionalDivan 

module RecordMapping =
    open Microsoft.FSharp.Reflection
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

    let rec readJson recordType (jObj: JObject) =
        let t = FSharpType.GetRecordFields(recordType)

        let rec readValue targetType (value: JToken) = 
            match value with
            | :? JObject as nestedObj -> readJson targetType nestedObj
            | :? JArray as nestedArray -> 
                (Array.init 
                    nestedArray.Count 
                    (fun i -> 
                        match nestedArray.[i] with
                        | :? JValue as v -> v.Value
                        | _ as other -> readValue targetType other)) :> obj
            | :? JValue as v -> v.Value
            | _ as unk -> failwith <| sprintf "%A is an unsupported node type" unk

        let values = 
            Array.init 
                t.Length
                (fun i ->
                    let pType = t.[i].PropertyType
                    if (pType.IsArray) then readValue (pType.GetElementType()) (jObj.[normalizeName t.[i].Name])
                    elif pType.IsGenericType then readValue (pType.GetGenericArguments().[0]) (jObj.[normalizeName t.[i].Name])
                    else readValue (pType) (jObj.[normalizeName t.[i].Name])
                )
                
        recordType.GetConstructor(Array.map (fun (e: PropertyInfo) -> e.PropertyType) t).Invoke values

    let rec writeJson record (writer: JsonWriter) =
        let rec writeValue (value: obj) = 
            if value = null then () 
            else
                if FSharpType.IsRecord(value.GetType()) then
                    writer.WriteStartObject()
                    writeJson value writer
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
                
