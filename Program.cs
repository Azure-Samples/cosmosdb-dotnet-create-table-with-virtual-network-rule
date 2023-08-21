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
using Azure.ResourceManager.Network;
using Azure.ResourceManager.Network.Models;

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
                Console.WriteLine("Create a virtual network with two subnets....");

                string vnetName = Utilities.CreateRandomName("vnet");
                VirtualNetworkData vnetInput = new VirtualNetworkData()
                {
                    Location = resourceGroup.Data.Location,
                    AddressPrefixes = { "10.10.0.0/16" },
                    Subnets =
                    {
                        new SubnetData()
                        {
                            Name = "subnet1",
                            AddressPrefix = "10.10.1.0/24",
                            ServiceEndpoints ={ new ServiceEndpointProperties(){ Service  = "Microsoft.AzureCosmosDB" } }
                        },
                        new SubnetData()
                        {
                            Name = "subnet2",
                            AddressPrefix = "10.10.2.0/24",
                            ServiceEndpoints ={ new ServiceEndpointProperties(){ Service  = "Microsoft.AzureCosmosDB" } }
                        },
                    }
                };
                var vnetLro = await resourceGroup.GetVirtualNetworks().CreateOrUpdateAsync(WaitUntil.Completed, vnetName, vnetInput);
                VirtualNetworkResource vnet = vnetLro.Value;
                Console.WriteLine($"Created a virtual network {vnet.Id.Name}");


                //============================================================
                // Create a CosmosDB
                Utilities.Log("Creating a CosmosDB...");
                string dbAccountName = Utilities.CreateRandomName("dbaccount");
                CosmosDBAccountKind cosmosDBKind = CosmosDBAccountKind.GlobalDocumentDB;
                var locations = new List<CosmosDBAccountLocation>()
                {
                    new CosmosDBAccountLocation(){ LocationName  = AzureLocation.WestUS, FailoverPriority = 0 },
                };
                var dbAccountInput = new CosmosDBAccountCreateOrUpdateContent(AzureLocation.WestUS, locations)
                {
                    Kind = cosmosDBKind,
                    ConsistencyPolicy = new Azure.ResourceManager.CosmosDB.Models.ConsistencyPolicy(DefaultConsistencyLevel.BoundedStaleness)
                    {
                        MaxStalenessPrefix = _maxStalenessPrefix,
                        MaxIntervalInSeconds = _maxIntervalInSeconds
                    },
                    VirtualNetworkRules =
                    {
                        new CosmosDBVirtualNetworkRule(){ Id = vnet.Data.Subnets[0].Id }
                    },
                    IsVirtualNetworkFilterEnabled = true,
                    EnableAutomaticFailover = false,
                    DisableKeyBasedMetadataWriteAccess = false,
                    ConnectorOffer = ConnectorOffer.Small,
                };

                dbAccountInput.Tags.Add("key1", "value");
                dbAccountInput.Tags.Add("key2", "value");
                var accountLro = await resourceGroup.GetCosmosDBAccounts().CreateOrUpdateAsync(WaitUntil.Completed, dbAccountName, dbAccountInput);
                CosmosDBAccountResource dbAccount = accountLro.Value;
                Utilities.Log($"Created CosmosDB {dbAccount.Id.Name}");

                // ============================================================
                // Get the virtual network rule created above.

                var vnetRules = dbAccount.Data.VirtualNetworkRules;

                Console.WriteLine("CosmosDB Virtual Network Rules:");
                foreach (var vnetRule in vnetRules)
                {
                    Console.WriteLine("\t" + vnetRule.Id);
                }


                // ============================================================
                // Add new virtual network rules.

                Console.WriteLine("Add new virtual network rules to CosmosDB account..");
                CosmosDBAccountPatch updataContent = new CosmosDBAccountPatch()
                {
                    VirtualNetworkRules =
                    {
                        new CosmosDBVirtualNetworkRule(){ Id = vnet.Data.Subnets[0].Id },
                        new CosmosDBVirtualNetworkRule(){ Id = vnet.Data.Subnets[1].Id }
                    }
                };
                var updated_dbAcccount = await dbAccount.UpdateAsync(WaitUntil.Completed, updataContent);

                // ============================================================
                // List then remove all virtual network rules.
                Console.WriteLine("Listing all virtual network rules in CosmosDB account.");

                vnetRules = updated_dbAcccount.Value.Data.VirtualNetworkRules;

                Console.WriteLine("CosmosDB Virtual Network Rules:");
                foreach (var vnetRule in vnetRules)
                {
                    Console.WriteLine("\t" + vnetRule.Id);
                }

                //============================================================
                // Delete CosmosDB
                Utilities.Log("Deleting the CosmosDB");
                await dbAccount.DeleteAsync(WaitUntil.Completed);
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
