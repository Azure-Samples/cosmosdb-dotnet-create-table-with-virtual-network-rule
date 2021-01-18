---
page_type: sample
languages:
- csharp
products:
- azure
extensions:
  services: Cosmos-DB
  platforms: dotnet
---

# Create a CosmosDB configured with a Virtual Network Rule using C# #

 Azure CosmosDB sample for using Virtual Network ACL rules.
  - Create a Virtual Network with two subnets.
  - Create an Azure Table CosmosDB account configured with a Virtual Network Rule
  - Add another virtual network rule in the CosmosDB account
  - List all virtual network rules.
  - Delete a virtual network.
  - Delete the CosmosDB.


## Running this Sample ##

To run this sample:

Set the environment variable `AZURE_AUTH_LOCATION` with the full path for an auth file. See [how to create an auth file](https://github.com/Azure/azure-libraries-for-net/blob/master/AUTH.md).

    git clone https://github.com/Azure-Samples/cosmosdb-dotnet-create-table-with-virtual-network-rule.git

    cd cosmosdb-dotnet-create-table-with-virtual-network-rule

    dotnet build

    bin\Debug\net452\CreateCosmosDBTableWithVirtualNetworkRule.exe

## More information ##

[Azure Management Libraries for C#](https://github.com/Azure/azure-sdk-for-net/tree/Fluent)
[Azure .Net Developer Center](https://azure.microsoft.com/en-us/develop/net/)
If you don't have a Microsoft Azure subscription you can get a FREE trial account [here](http://go.microsoft.com/fwlink/?LinkId=330212)

---

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.