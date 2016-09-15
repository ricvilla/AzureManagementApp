using System;
using System.Threading.Tasks;
using System.Configuration;
using Microsoft.Azure.Management.Compute;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Globalization;
using Microsoft.Azure.Management.Compute.Models;

namespace AzureManagementApp
{
    class Program
    {
        public static Uri aadInstance = new Uri(ConfigurationManager.AppSettings["ida:AADInstance"]);
        public static string tenant = ConfigurationManager.AppSettings["ida:Tenant"];
        public static string tenantId = ConfigurationManager.AppSettings["ida:TenantId"];
        public static string clientId = ConfigurationManager.AppSettings["ida:ClientId"];
        public static Uri redirectUri = new Uri(ConfigurationManager.AppSettings["ida:RedirectUri"]);
        public static Uri AzureUri = new Uri(ConfigurationManager.AppSettings["ida:AzureUri"]);
        private static string authority = String.Format(CultureInfo.InvariantCulture, aadInstance.ToString(), tenant);

        static void Main(string[] args)
        {
            //Get Access Token using AAD
            var getAccessTokenTask = GetAccessTokenAsync();
            Task.WaitAll(getAccessTokenTask);
            string token = getAccessTokenTask.Result;

            //Get List of Virtual Machines for a specific Resource Group
            string resourceGroup = "rv-group";
            var getVMListTask = GetVMListAsync(token, resourceGroup);
            Task.WaitAll(getVMListTask);
            Hyak.Common.LazyList<VirtualMachine> VMs = getVMListTask.Result;
            //VMs.ForEach(x => Console.WriteLine(x.Name));
            Console.WriteLine(VMs.Count);
        }

        private static async Task<string> GetAccessTokenAsync()
        {
            AuthenticationContext authContext = new AuthenticationContext(authority);

            try
            {
                AuthenticationResult result = await authContext.AcquireTokenAsync(AzureUri.ToString(), clientId, redirectUri, new PlatformParameters(PromptBehavior.Always));
                return result.CreateAuthorizationHeader().Substring("Bearer ".Length);
            }
            catch (AdalException ex)
            {
                return null;
            }
        }

        private static async Task<Hyak.Common.LazyList<VirtualMachine>> GetVMListAsync(string token, string resourceGroup)
        {
            var credential = new Microsoft.Azure.TokenCloudCredentials(ConfigurationManager.AppSettings["ida:SubscriptionId"], token);

            var computeClient = new ComputeManagementClient(credential);
            VirtualMachineListResponse result = await computeClient.VirtualMachines.ListAsync(resourceGroup);
            return (Hyak.Common.LazyList<VirtualMachine>)result.VirtualMachines;
        }
    }
}
