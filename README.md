FunctionalDivan: a CouchDB library in F#
========================================

FunctionalDivan is a small F# dsl written over the [Foretagsplatsen Divan](/foretagsplatsen/Divan) library.

It also "orm" for F# records into couch documents, requiring only that they have the 'id' and 'rev' properties.

Samples
------------

    type address = {
        street: string
        zip: string
    }
    
    type person = {
        id: string // required
        rev: string // required
        firstname: string
        lastname: string
        primaryAddress: address
        otherAddresses: address list
    }

    // set up some sample data
    let addy = { street = "michigan ave"; zip = "60606" }
    let me = {id = null; rev = null; firstname = "alex"; lastname="pedenko"; 
    						primaryAddress = addy; otherAddresses = [{addy with street = "dee st"}]}
    						
    						
    // connect
    let svr = server "localhost" 5984
    let database = db "view_tests" svr

    // save me! and remember the new id and rev
    let id, rev = me |> into db

		// pull two people from the db (assuming someone else has written them there)
    let (result: person list) = 
        selectRecords (
            query "people" "names" database |> limitTo 2
        )

Reference
---------

The following commands are supported.

Standard
--------
* server - creates a new server object
* db - creates a new database object
* query - query against the design/view in the db
* byKey(s) - restricts the resultset to the given key(s)
* limitTo - limits the resultset
* offsetBy - skips the first n records
* start/endAt - supplies startkey/endkey values
* start/endAtId - suppliyes startdocid/enddocid values
* select - runs the query
* selectDocs - runs the query with the 'include_docs' parameter set to true, returning an f# list of documents. the supplied type parameter must of a class that implements ICouchDocument
* selectRecords - runs the query with the 'include_docs' parameter set to true, returning an f# list records that correspond to the documents brought back. the records must have an 'id' and a 'rev' property.
* from - retrieves a document by id, as an f# record
* into - stores an f# record as a couch document, returning a tuple of the id/rev this generated

Lucene
------
* query - queries the full text index
* q - sets the lucene query
* limitTo - limits the resultset
* offsetBy - skips the first n records
* select - runs the query
* selectDocs - runs the query with the 'include_docs' parameter set to true, returning an f# list of documents. the supplied type parameter must of a class that implements ICouchDocument
* selectRecords - runs the query with the 'include_docs' parameter set to true, returning an f# list records that correspond to the documents brought back. the records must have an 'id' and a 'rev' property.
