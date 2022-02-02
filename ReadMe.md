# Key generation service

## Table of contents

1. [Description](#description)
2. [Quick feature summary](#quick-feature-summary)
3. [Purpose](#purpose)
4. [Tech stack](#tech-stack)
5. [Install & Run](#install-&-run)
6. [Usage & Api key](#usage-&-api-key)
7. [SQL database tables](#sql-database-tables)
8. [Key generation and seeding](#key-generation-and-seeding)
9. [Caching](#caching)

## Description

Key generation service is a .net core web API that returns globally unique keys between 8 and 16 characters in length. These keys are then marked as taken and will not be given out to any other request. The keys can also be returned back to being available if they are no longer used. This API is currently hosted on azure with this URL https://keygenerationservice.azurewebsites.net/. Please read the Usage section for getting started.

## Quick feature summary

- Unique keys of 8-16 characters in length are generated and seeded in the database on a request that can not be fulfilled with enough keys.
- Each instance of this service caches a certain number of keys marking them as taken in order to allow of quicker response times and to remove the possibility of collisions between service instances.
- This service requires an API key to be used in the header in order to set a daily rate limit on the client, this daily rate limit is reset by an Azure function.
- Hosted on Azure with the URL provided above.

## Purpose

The main purpose of this microservice is to allow for different instances of the same (or different if applicable) API hosted in the cloud to request and receive globally unique keys and not have to worry about duplicate keys being created between instances. This allows for easier scalability of microservices that require unique keys to be used. Some real world applications are Youtube video ids, twitter tweet ids etc. This service was used in another personal project which is a URL shortner which can be found here.

## Tech stack

- .net core 5.0
- Redis for caching
- SQL database
- Azure for hosting
- Azure function for daily usage reset

## Install & Run

- ` git clone https://github.com/DanilLinkov/KeyGenerationService.git`
- In a command window at the root level of the project run `dotnet restore` followed by `dotnet run`

## Usage & Api key

With every request provide a header with the following key value pair

`API-KEY: {A valid API key that is contained in the ApiKeys table}`

If using the already hosted https://keygenerationservice.azurewebsites.net/ version then can use "test" as the API key

There is a daily limit to the number of keys that can be produced by each API key and with the test key it is set to 1000 keys.

The number of keys created with a given API key is reset to 0 daily using an azure function.

1) `GET /api/key` or `GET /api/key?size={number between 8-16}`

   Returned JSON:

   ```json
   {
       "id": int,
       "key": string of given size or 8 by default,
       "size": int size of key,
       "creationDate": Date time of key creation,
       "takenDate": Date time of key being taken
   }
   ```

   

2) `GET /api/keys/{count}` `GET /api/keys/{count}?size={number between 8-16}`

   Returned JSON:

   ```json
   [
       {
           "id": int,
           "key": string of given size or 8 by default,
           "size": int size of key,
           "creationDate": Date time of key creation,
           "takenDate": Date time of key being taken
   	}
   ]
   ```

   

3) `POST /api/ReturnKeys`

   ```json
   {
       "keys"; ["string"]
   }
   ```

   

## SQL database tables

- AvailableKeys table
  - PK: Id - int
  - SK: Key - nvarchar
  - Size - int
  - CreationDate - datetime
- TakenKeys table
  - PK: Id - int
  - SK: Key - nvarchar
  - Size - int
  - CreationDate - datetime
  - TakenDate - datetime

- ApiKeys table
  - PK: Id - int
  - SK: Key - nvarchar
  - OwnerName - nvarchar
  - KeysCreatedToday - int
  - KeysAllowedToday - int

## Key generation and seeding

To generate the keys an algorithm that makes the use of random number generation (a singleton so can not produce the same number twice) and bit conversion is used. The allowed characters are "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890" and the output size is set to range between 8 and 16. If multiple instances of this service are used for key generation then duplicate keys will be skipped and not inserted into the database. In the real world these keys would be generated once and not be required to be generated again for efficiency.

Whenever a request for a certain number of keys can not be fulfilled as there are not enough keys of required size in the cache or database then the generation process begins and the AvailableKeys table is seeded with an "x" number of generated keys that can be configured in the settings. For saving storage and given there are not going to be many requests to the hosted project this number is set to 50 by default however in a real world scenario this would be set to millions.

## Caching

To allow for quicker response times an instance of this service will take a certain number of keys from the database and keep them in a distributed Redis cache. These keys are put in the Taken table when put in cache. Whenever there is a request that does not get fulfilled by a cache, the request is fulfilled by accessing the database and a background job is executed to refill the cache. By default the max keys in Cache is set to 10 and the cache expires within 30 minutes (to save hosted Redis storage space).

