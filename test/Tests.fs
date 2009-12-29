#light

namespace FunctionalDivan

module Test =
    open NUnit.Framework
    open Dsl
    open RecordMapping

    type address = {
        street: string
        zip: string
    }
    
    type person = {
        id: string
        rev: string
        firstname: string
        lastname: string
        primaryAddress: address
        otherAddresses: address list
    }

    [<TestFixture>]
    type CRUDTests() =
        let svr = server "172.16.10.78" 5984
        let database = db "crud_tests" svr
        
        let addy = { street = "michigan ave"; zip = "60606" }
        let me = {id = null; rev = null; firstname = "alex"; lastname="pedenko"; primaryAddress = addy; otherAddresses = [{addy with street = "dee st"}]}

        [<TestFixtureTearDown>]
        member x.Cleanup() = 
            database.Delete()
        
        [<Test>]
        member x.CanCreate() =
            let id, rev = me |> into database
            Assert.NotNull(id)
            Assert.NotNull(rev)
            
        [<Test; ExpectedException()>]
        member x.MustFailOnBadRecord() =
            ignore <| (addy |> into database)

        [<Test>]
        member x.CanCreateAndRead() =
            let id, rev = { me with id = "mytest" } |> into database
            Assert.NotNull(id)
            Assert.NotNull(rev)
            let (r: person option) = "mytest" |> from database
            Assert.That(r.IsSome, "no record found")
            Assert.AreEqual("mytest", r.Value.id)
            
    [<TestFixture>]
    type ViewTests() =
        let svr = server "localhost" 5984
        let database = db "view_tests" svr
        do ignore <| database.NewDesignDocument("people").AddView("names", "function (doc) { emit(doc.firstname, null); }")
        do database.SynchDesignDocuments()
        
        let addy = { street = "michigan ave"; zip = "60606" }
        let me = {id = null; rev = null; firstname = "alex"; lastname="pedenko"; primaryAddress = addy; otherAddresses = [{addy with street = "dee st"}]}
        
        do for i in 0..10 do
            ignore ({ me with firstname = sprintf "%s%d" me.firstname i } |> into database)

        [<TestFixtureTearDown>]
        member x.Cleanup() = 
            database.Delete()
        
        [<Test>]
        member x.CanRead2() =
            let (result: person list) = 
                selectRecords (
                    query "people" "names" database |> limitTo 2
                )
                
            Assert.AreEqual(2, List.length result)
            
        [<Test>]
        member x.CanReadKeyRange() =
            let (result: person list) = 
                selectRecords (
                    query "people" "names" database |> limitTo 2 |> startAt "alex1" |> endAt "alex2"
                )
                
            Assert.AreEqual(2, List.length result)
