// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using Azure;
using Azure.Core;
using Azure.Identity;
using Azure.ResourceManager.Resources.Models;
using Azure.ResourceManager.Samples.Common;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager;
using Azure.ResourceManager.CosmosDB.Models;
using Azure.ResourceManager.CosmosDB;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents;

namespace CosmosDBTableWithVirtualNetworkRule
{
    public class Program
    {
        private static ResourceIdentifier? _resourceGroupId = null;
        private const int _maxStalenessPrefix = 100000;
        private const int _maxIntervalInSeconds = 300;

        /**
         * Azure CosmosDB sample for using Virtual Network ACL rules.
         *  - Create a Virtual Network with two subnets.
         *  - Create an Azure Table CosmosDB account configured with a Virtual Network Rule
         *  - Add another virtual network rule in the CosmosDB account
         *  - List all virtual network rules.
         *  - Delete a virtual network.
         *  - Delete the CosmosDB.
         */
        public static async Task RunSample(ArmClient client)
        {
            try
            {
                // Get default subscription
                SubscriptionResource subscription = await client.GetDefaultSubscriptionAsync();

                // Create a resource group in the EastUS region
                string rgName = Utilities.CreateRandomName("CosmosDBTemplateRG");
                Utilities.Log($"Creating a resource group..");
                ArmOperation<ResourceGroupResource> rgLro = await subscription.GetResourceGroups().CreateOrUpdateAsync(WaitUntil.Completed, rgName, new ResourceGroupData(AzureLocation.EastUS));
                ResourceGroupResource resourceGroup = rgLro.Value;
                _resourceGroupId = resourceGroup.Id;
                Utilities.Log("Created a resource group with name: " + resourceGroup.Data.Name);

                // ============================================================
                // Create a virtual network with two subnets.
                Console.WriteLine("Creating a CosmosDB...");

                var virtualNetwork = azure.Networks.Define(vnetName)
                    .WithRegion(Region.USEast)
                    .WithNewResourceGroup(rgName)
                    .WithAddressSpace("192.168.0.0/16")
                    .DefineSubnet("subnet1")
                        .WithAddressPrefix("192.168.1.0/24")
                        .WithAccessFromService(ServiceEndpointType.MicrosoftAzureCosmosDB)
                        .Attach()
                    .DefineSubnet("subnet2")
                        .WithAddressPrefix("192.168.2.0/24")
                        .WithAccessFromService(ServiceEndpointType.MicrosoftAzureCosmosDB)
                        .Attach()
                    .Create();

                Console.WriteLine("Created a virtual network");
                // Print the virtual network details
                Utilities.PrintVirtualNetwork(virtualNetwork);


                //============================================================
                // Create a CosmosDB
                Console.WriteLine("Creating a CosmosDB...");

                ICosmosDBAccount cosmosDBAccount = azure.CosmosDBAccounts.Define(cosmosDBName)
                        .WithRegion(Region.USWest)
                        .WithExistingResourceGroup(rgName)
                        .WithDataModelAzureTable()
                        .WithEventualConsistency()
                        .WithWriteReplication(Region.USEast)
                        .WithVirtualNetworkRule(virtualNetwork.Id, "subnet1")
                        .Create();

                Console.WriteLine("Created CosmosDB");
                Utilities.Print(cosmosDBAccount);

                // ============================================================
                // Get the virtual network rule created above.

                var vnetRules = cosmosDBAccount.VirtualNetworkRules;

                Console.WriteLine("CosmosDB Virtual Network Rules:");
                foreach (var vnetRule in vnetRules)
                {
                    Console.WriteLine("\t" + vnetRule.Id);
                }


                // ============================================================
                // Add new virtual network rules.

                cosmosDBAccount.Update()
                    .WithVirtualNetworkRule(virtualNetwork.Id, "subnet2")
                    .Apply();


                // ============================================================
                // List then remove all virtual network rules.
                Console.WriteLine("Listing all virtual network rules in CosmosDB account.");

                vnetRules = cosmosDBAccount.VirtualNetworkRules;

                Console.WriteLine("CosmosDB Virtual Network Rules:");
                foreach (var vnetRule in vnetRules)
                {
                    Console.WriteLine("\t" + vnetRule.Id);
                }

                cosmosDBAccount.Update()
                    .WithVirtualNetworkRules(null)
                    .Apply();

                azure.Networks.DeleteById(virtualNetwork.Id);

                //============================================================
                // Delete CosmosDB
                Utilities.Log("Deleting the CosmosDB");
            }
            finally
            {
                try
                {
                    if (_resourceGroupId is not null)
                    {
                        Utilities.Log($"Deleting Resource Group: {_resourceGroupId}");
                        await client.GetResourceGroupResource(_resourceGroupId).DeleteAsync(WaitUntil.Completed);
                        Utilities.Log($"Deleted Resource Group: {_resourceGroupId}");
                    }
                }
                catch (NullReferenceException)
                {
                    Utilities.Log("Did not create any resources in Azure. No clean up is necessary");
                }
                catch (Exception e)
                {
                    Utilities.Log(e.StackTrace);
                }
            }
        }

        public static async Task Main(string[] args)
        {
            try
            {
                //=================================================================
                // Authenticate
                var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
                var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
                var tenantId = Environment.GetEnvironmentVariable("TENANT_ID");
                var subscription = Environment.GetEnvironmentVariable("SUBSCRIPTION_ID");
                ClientSecretCredential credential = new ClientSecretCredential(tenantId, clientId, clientSecret);
                ArmClient client = new ArmClient(credential, subscription);

                await RunSample(client);
            }
            catch (Exception e)
            {
                Utilities.Log(e.Message);
                Utilities.Log(e.StackTrace);
            }
        }
    }
}
