using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Net.Http;
using System.Threading;
using System.Net.Http.Headers;
using System.Web.Script.Serialization;
using System.IdentityModel.Tokens.Jwt;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;
using Microsoft.Graph;

namespace ConsoleApp1
{
    class Program
    {
        private static string resourceUri = "https://graph.microsoft.com";
        private static string clientId = "<Replace With Your Client ID> ";
        private static string redirectUri = "<Replace with your Redirect URI> ";
        private static string authority = "https://login.microsoftonline.com/";
        private static string tenantID = "<Replace with your TenantID>";
        private static string getCall = "<Get Call Method>";
        private static AuthenticationContext authContext = null;
        private static AuthenticationResult result = null;
        private static HttpClient httpClient = new HttpClient();
        static void Main(string[] args)
        {

            /*
             * Get Access token, Create random users, MSFT Graph SDK testing code.
             * 
            getAccessToken().Wait();
            // GetMembers utilizes HTTP Client, will print JSON Prettified in method
            // getMembers().Wait();


            // CreateRandomUsers utilizes http client as the MSGraph SDK doesn't support creating users yet. It will create random users and print the response body
            // createRandomUsers().Wait();

            // getUsersUsingGraphServiceClient will utilize the MS Graph SDK and page through 5 users at a time.
            getUsersUsingGraphServiceClient().Wait();

            Console.WriteLine("\n Press Enter to exit the program \n");
            Console.ReadLine();
            */


            Console.WriteLine("Welcome, this application will make a single get call to the Microsoft Graph. You cannot make Post calls using this application." +
                " You will need to enter your Application ID, Redirect URI, Tenant ID, and the Get Call you are trying to make in the following propmts below.");
            Console.WriteLine("\n Please Enter in your Client ID (AAD Application Registration Application ID) and then press enter. \n");
            clientId = Console.ReadLine();

            Console.WriteLine("\n Please Enter in your Redirect URI (AAD Application Registration Reply URL [first one]) and then press enter. \n");
            redirectUri = Console.ReadLine();

            Console.WriteLine("\n Please Enter in your Azure Active Directory Tenant ID that the AAD Application Registration is in and then press enter. \n");
            tenantID = Console.ReadLine();

            Console.WriteLine("\n Please Enter the Get Call to Microsoft Graph you would like to make. For Example : " + resourceUri + "/v1.0/users?$top=5 and then press enter.\n");
            getCall = Console.ReadLine();

            getAccessToken().Wait();
            // GetMembers utilizes HTTP Client, will print JSON Prettified in method
            // getMembers().Wait();

            // Make get call utilizes the call entered by the user during the get call input stage  
            makeGetCall().Wait();
            Console.WriteLine("\n Press Enter to exit the program \n");
            Console.ReadLine();

        }


        static async Task makeGetCall()
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            Console.WriteLine("\n \n Making get Call at {0}", DateTime.Now.ToString());
            Console.WriteLine("Making Get Call to " + getCall);
            HttpResponseMessage response = await httpClient.GetAsync(getCall);

            if (response.IsSuccessStatusCode)
            {
                // Read the response and output it to the console.
                string users = await response.Content.ReadAsStringAsync();
                Console.WriteLine("\n \n Printing out Users \n \n");
                Console.WriteLine(JsonPrettify(users));
                Console.WriteLine("Received Info");
            }
            else
            {
                Console.WriteLine("Failed to retrieve To Do list\nError:  {0}\n", response.ReasonPhrase);
            }
        }

        static async Task getUsersUsingGraphServiceClient()
        {
            var graphServiceClient = new GraphServiceClient(new DelegateAuthenticationProvider((requestMessage) =>
            {
                requestMessage
                    .Headers
                    .Authorization = new AuthenticationHeaderValue("bearer", result.AccessToken);

                return Task.FromResult(0);
            }));

            var users = graphServiceClient.Users.Request().Top(5)
                .GetAsync().GetAwaiter().GetResult();

            Console.WriteLine("\n Printing out first 5 users \n");
            for (int index = 0; index < users.CurrentPage.Count; ++index)
                Console.WriteLine(users.CurrentPage[index].DisplayName);

            while (users.NextPageRequest != null)
            {
                Console.WriteLine("\n Press Enter to get the next 5 users \n");
                Console.ReadLine();
                
                Console.WriteLine("\n Getting Next Page of Users \n");
                users = users.NextPageRequest.GetAsync().GetAwaiter().GetResult();


                Console.WriteLine("\n Printing Next 5 users \n");
                for (int index = 0; index < users.CurrentPage.Count; ++index)
                    Console.WriteLine(users.CurrentPage[index].DisplayName);


            }

        }

        static async Task createRandomUsers()
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            Console.WriteLine("\n \n Creating users {0}", DateTime.Now.ToString());

            for(int index = 0;index < 10; ++index)
            {
                var values = "{\r\n  \"accountEnabled\": true,\r\n  \"displayName\": \"TestName "+ index + "\",\r\n  \"mailNickname\": \"TestNameNickName\",\r\n  \"userPrincipalName\": \"TestNameNickName"+index+"@" + tenantID + "\",\r\n  \"passwordProfile\" : {\r\n    \"forceChangePasswordNextSignIn\": true,\r\n    \"password\": \"MyNewPassword123\"\r\n  }\r\n}";
              
                var content = new StringContent(values, Encoding.Default, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(resourceUri + "/v1.0/users", content);

                if (response.IsSuccessStatusCode)
                {
                    // Read the response and output it to the console.
                    string users = await response.Content.ReadAsStringAsync();
                    JavaScriptSerializer serializer = new JavaScriptSerializer();
                    Console.WriteLine("\n \n Printing Created User \n \n");
                    Console.WriteLine(JsonPrettify(users));
                    Console.WriteLine("Received Info");
                }
                else
                {
                    Console.WriteLine("Failed to retrieve To Do list\nError:  {0}\n", response.ReasonPhrase);
                }
            }
        }
        /** 
         * GetMembers function will get users using HTTP Client,
         */

        static async Task getMembers()
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.AccessToken);
            Console.WriteLine("\n \n Retrieving users {0}", DateTime.Now.ToString());
            HttpResponseMessage response = await httpClient.GetAsync(resourceUri + "/v1.0/users?$top=5");

            if (response.IsSuccessStatusCode)
            {
                // Read the response and output it to the console.
                string users = await response.Content.ReadAsStringAsync();
                JavaScriptSerializer serializer = new JavaScriptSerializer();

                Console.WriteLine("\n \n Printing out Users \n \n");
                Console.WriteLine(JsonPrettify(users));
                Console.WriteLine("Received Info");
            }
            else
            {
                Console.WriteLine("Failed to retrieve To Do list\nError:  {0}\n", response.ReasonPhrase);
            }
        }

        static async Task getAccessToken()
        {
            int retryCount = 0;
            bool retry = false;
            authContext = new AuthenticationContext(authority);
            // Create client Credential for getting an access token using Auth Code Flow
            // clientCredential = new ClientCredential(clientId, clientSecret);

            do
            {
                retry = false;
                try
                {
                    // ADAL includes an in memory cache, so this call will only send a message to the server if the cached token is expired.

                    /* result = await authContext.AcquireTokenAsync(clientId,
                                       resourceUri,
                                       new Uri(redirectUri),
                                       new PlatformParameters(PromptBehavior.Auto));
                    */

                    // AcquireToken using Auth Code
                    // result = await authContext.AcquireTokenByAuthorizationCodeAsync(authorizationCode, new Uri(redirectUri), clientCredential);

                    result = await authContext.AcquireTokenAsync(resourceUri, clientId, new Uri(redirectUri), new PlatformParameters(PromptBehavior.Auto));

                    Console.Write("My Access Token : \n");
                    await prettyJWTPrint(result.AccessToken);
                }
                catch (AdalException ex)
                {
                    if (ex.ErrorCode == "temporarily_unavailable")
                    {
                        retry = true;
                        retryCount++;
                        Thread.Sleep(3000);
                    }

                    Console.WriteLine(
                        String.Format("An error occurred while acquiring a token\nTime: {0}\nError: {1}\nRetry: {2}\n",
                        DateTime.Now.ToString(),
                        ex.ToString(),
                        retry.ToString()));
                }

            } while ((retry == true) && (retryCount < 3));

            if (result == null)
            {
                Console.WriteLine("Canceling attempt to get access token.\n");
                return;
            }

        }

        public static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }
        static async Task prettyJWTPrint(String myToken)
        {
            //Assume the input is in a control called txtJwtIn,
            //and the output will be placed in a control called txtJwtOut
            var jwtHandler = new JwtSecurityTokenHandler();
            var jwtInput = myToken;
            String prettyPrint = "";

            //Check if readable token (string is in a JWT format)
            var readableToken = jwtHandler.CanReadToken(jwtInput);

            if (readableToken != true)
            {
                Console.WriteLine("The token doesn't seem to be in a proper JWT format.");
            }
            if (readableToken == true)
            {
                var token = jwtHandler.ReadJwtToken(jwtInput);

                //Extract the headers of the JWT
                var headers = token.Header;
                var jwtHeader = "{";
                foreach (var h in headers)
                {
                    jwtHeader += '"' + h.Key + "\":\"" + h.Value + "\",";
                }
                jwtHeader += "}";
                prettyPrint = "Header:\r\n" + JToken.Parse(jwtHeader).ToString(Formatting.Indented);

                //Extract the payload of the JWT
                var claims = token.Claims;
                var jwtPayload = "{";
                foreach (System.Security.Claims.Claim c in claims)
                {
                    jwtPayload += '"' + c.Type + "\":\"" + c.Value + "\",";
                }
                jwtPayload += "}";
                prettyPrint += "\r\nPayload:\r\n" + JToken.Parse(jwtPayload).ToString(Formatting.Indented);

            }
            Console.WriteLine(prettyPrint);
        }
    }
}
