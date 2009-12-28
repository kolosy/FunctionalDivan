#light

namespace FunctionalDivan

module Test =
    open NUnit.Framework
    open Dsl
    open RecordMapping

    type childRecord = {
        first: string
        last: string
    }
    
    type testRecord = {
        id: string
        rev: string
        something: int64
        firstChild: childRecord
        allChildren: childRecord list
    }

    [<TestFixture>]
    type CRUDTests() =
        let svr = server "localhost" 5984
        let database = db "tests" svr
        
        let child = { first = "hello"; last = "world" }
        
        let test = {id = null; rev = null; something = int64 5; firstChild = child; allChildren = [{child with first = "hello again"}]}

        [<TestFixtureTearDown>]
        member x.Cleanup() = 
            database.Delete()
        
        [<Test>]
        member x.CanCreate() =
            let id, rev = test |> into database
            Assert.NotNull(id)
            Assert.NotNull(rev)
            
        [<Test; ExpectedException()>]
        member x.MustFailOnBadRecord() =
            ignore <| (child |> into database)

        [<Test>]
        member x.CanCreateAndRead() =
            let id, rev = { test with id = "mytest" } |> into database
            Assert.NotNull(id)
            Assert.NotNull(rev)
            let r = "mytest" |> from<testRecord> database
            Assert.That(r.IsSome, "no record found")
            Assert.AreEqual("mytest", r.Value.id)
            
