#light

namespace FunctionalDivan 

module Dsl =
    open Divan
    open RecordMapping
    open Microsoft.FSharp.Reflection
    open System.Reflection

    let server address port = CouchServer(address, port)
    let db name (server: CouchServer) = server.GetDatabase(name)
    let query design view (db: CouchDatabase) = db.Query(design, view)
    let byKeys (key: obj[]) (query: CouchQuery) = query.Keys key
    let byKey (key: obj) (query: CouchQuery) = query.Key key
    let limitTo limit (query: CouchQuery) = query.Limit limit
    let offsetBy offset (query: CouchQuery) = query.Skip offset
    let startAt (startKey: obj) (query: CouchQuery) = query.StartKey startKey
    let endAt (endKey: obj) (query: CouchQuery) = query.EndKey endKey
    let select (query: CouchQuery) = query.GetResult()
    let selectDocs<'a when 'a: (new: unit -> 'a) and 'a:> ICouchDocument> (query: CouchQuery) = 
        List.ofSeq <| query.IncludeDocuments().GetResult().Documents<'a>()

    let selectRecords<'a> (query: CouchQuery) = 
        let results = (select query).Documents<CouchJsonDocument>()
        List.ofSeq results |>
        List.map (fun elem -> readJson typeof<'a> elem.Obj) 
        
    let from<'a> (db: CouchDatabase) id =
        let jsonDoc = db.GetDocument(id)
        if not (jsonDoc = null) then Some <| (readJson typeof<'a> (jsonDoc.Obj) :?> 'a)
        else None
        
    let into (db: CouchDatabase) r =
        let parms, hasId, hasRev = 
            Array.fold 
                (fun s (e: PropertyInfo) -> 
                            let map, id, rev = s
                            (Map.add (e.Name) e map, id || e.Name = "id", rev || e.Name = "rev")) 
                (Map.empty, false, false) 
                (FSharpType.GetRecordFields(r.GetType()))
                
        if not (hasId && hasRev) then
            failwith "both id and rev are required on the record"
        else 
            let _id, _rev = ref (parms.["id"].GetValue(r, null) :?> string), ref (parms.["rev"].GetValue(r, null) :?> string)
            ignore <| db.SaveDocument
                { new ICouchDocument with 
                    member x.ReadJson jObj = ()
                    member x.WriteJson writer = writeJson r writer
                    member x.Id with get() = !_id and set v = _id := v
                    member x.Rev with get() = !_rev and set v = _rev := v }
            
            !_id, !_rev

        
    module Fti =
        open Divan.Lucene
    
        let query name index (db: CouchDatabase) = db.Query(CouchLuceneViewDefinition(name, index, null))
        let q text (query: CouchLuceneQuery) = query.Q text
        let limitTo limit (query: CouchLuceneQuery) = query.Limit limit
        let offsetBy offset (query: CouchLuceneQuery) = query.Skip offset
        let select (query: CouchLuceneQuery) = query.GetResult()
        let selectDocs<'a when 'a: (new: unit -> 'a) and 'a:> ICouchDocument> (query: CouchLuceneQuery) = 
            List.ofSeq <| query.IncludeDocuments().GetResult().GetDocuments<'a>()
        let selectRecords<'a> (query: CouchLuceneQuery) = 
            let results = (select query).GetDocuments<CouchJsonDocument>()
            List.ofSeq results |>
            List.map (fun elem -> readJson typeof<'a> elem.Obj) 
        