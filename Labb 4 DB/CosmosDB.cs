﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Newtonsoft.Json;
using System.Diagnostics;

namespace Labb_4_DB
{
    public class CosmosDB
    {
        private const string EndpointUrl = "https://adventuredb.documents.azure.com:443/";
        private const string PKey = "flO2gE3q4Evebl52oLD1WYLVn5DG95E1DYpEYzPTWZnSP4NZpir7FhAo49W9gBYTGAZFCtJLRMZtt2RSWOElfA==";
        private DocumentClient client;

        private static string databaseName = "info";
        private static string[] collections = { "Emails", "NonExaminedPhotos", "ExaminedPhotos" };
        private UserEmail emailDoc;
        private UserPhoto photoDoc;

        // constructor 
        public CosmosDB()
        {
            client = new DocumentClient(new Uri(EndpointUrl), PKey);
        }

        public async Task CreateDBIfNotExists()
        {
            await client.CreateDatabaseIfNotExistsAsync(new Database { Id = databaseName });
        }

        public async Task CreateCollectionsIfNotExists()
        {

            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), new DocumentCollection { Id = collections[0] });

            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), new DocumentCollection { Id = collections[1] });

            await client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(databaseName), new DocumentCollection { Id = collections[2] });
        }

        // Create documents with class - instances.
        public async Task CreateDocuments(string emailAdress, string photoUrl)
        {
            emailDoc = new UserEmail(emailAdress);
            photoDoc = new UserPhoto(photoUrl, emailAdress);
        }

        //private void WriteToConsole(string format, params object[] args)   // Debug syfte
        //{
        //    Debug.WriteLine(format, args);
        //    Debug.WriteLine("Press any key to continue ...");
        //    Console.ReadKey();
        //}

        private class UserEmail
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string EmailAdress { get; set; }

            public UserEmail(string emailAdress)
            {
                this.Id = emailAdress;
                this.EmailAdress = emailAdress;
            }
        }

        private class UserPhoto
        {
            [JsonProperty(PropertyName = "id")]
            public string Id { get; set; }
            public string PhotoUrl { get; set; }

            public UserPhoto(string photoUrl, string emailAdress)
            {
                this.Id = emailAdress;
                this.PhotoUrl = photoUrl;
            }
        }

        // CREATES USER IF NOT EXISTS!
        public async Task<HttpResponseMessage> InsertUserIfNotExists(Func<string, bool, HttpRequestMessage, HttpResponseMessage> messageCallback, HttpRequestMessage req) /*where T : //?*/
        {
            try
            {
                await client.ReadDocumentAsync(UriFactory.CreateDocumentUri(databaseName, collections.First(), this.emailDoc.Id));

                return messageCallback($"User {emailDoc.EmailAdress} is already in DB!", false, req);
            }
            catch (DocumentClientException de)
            {
                if (de.StatusCode == HttpStatusCode.NotFound)
                {
                    await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collections[0]), this.emailDoc);

                    await client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(databaseName, collections[1]), this.photoDoc);

                    return messageCallback("Added user " + this.emailDoc.EmailAdress, true, req);

                }
                else
                {
                    return messageCallback("User was not added " + this.emailDoc.EmailAdress, false, req);
                }
            }
        }

        public HttpResponseMessage ExecuteSimpleQuery(Func<string, bool, HttpRequestMessage, HttpResponseMessage> messageCallback, HttpRequestMessage req)
        {
            // Set some common query options
            //FeedOptions queryOptions = new FeedOptions { MaxItemCount = -1 };

            string nonExaminedPhotosAsString = "NON-EXAMINED PHOTOS\n\n";

            IQueryable<string> nonExaminedPhotos = this.client.CreateDocumentQuery<UserPhoto>(
                    UriFactory.CreateDocumentCollectionUri(databaseName, collections[1]), null)
                    .Select(p => p.PhotoUrl);

            foreach (var photo in nonExaminedPhotos)
            {
                // callback Invoke!
                nonExaminedPhotosAsString += photo + "\n";
            }
            return messageCallback(nonExaminedPhotosAsString, true, req);
        }
    }
}
