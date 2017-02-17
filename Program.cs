using System;
using System.IO;
using System.Net;

namespace SlackCleaner
{
    class Program
    {
        static void Main(string[] args)
        {
            // Get a token here: https://api.slack.com/docs/oauth-test-tokens
            string token = Environment.GetEnvironmentVariable("SLACK_TOKEN", EnvironmentVariableTarget.User);

            if (string.IsNullOrEmpty(token))
            {
                WriteError("No Slack token found!\n");

                Console.Write("   1. Get a Slack token here: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine("https://api.slack.com/docs/oauth-test-tokens");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;

                Console.WriteLine("   2. Set an environment variable for the token by using this command:");
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("      SETX SLACK_TOKEN <token>");
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            else
            {
                // get today's date and default to get everything ONE YEAR OLD or older
                DateTime today = DateTime.Today;

                // see here: https://api.slack.com/methods/search.all
                string query = string.Format("from:me before:{0}-{1}-{2}", today.Year - 1, today.Month, today.Day);
                Console.WriteLine("Enter query (defaults to '{0}')", query);
                string enteredQuery = Console.ReadLine();
                if (!string.IsNullOrEmpty(enteredQuery))
                {
                    query = enteredQuery;
                }

                int maxMessageCount = 1000;
                string searchUrl = string.Format("https://slack.com/api/search.all?token={0}&query={1}&count={2}", token, query, maxMessageCount);

                Console.WriteLine("Accessing {0}\n", searchUrl);

                HttpWebRequest searchRequest = (HttpWebRequest)WebRequest.Create(searchUrl);
                searchRequest.Timeout = 10000;
                searchRequest.ReadWriteTimeout = 10000;

                WebResponse searchResponse = searchRequest.GetResponse();
                dynamic jsonObject = GetJSONObject(searchResponse);
                searchResponse.Close();

                //if (jsonObject.ok == "true") //NewtonSoft.Json
                if (jsonObject.ok) //SimpleJson
                {
                    Console.WriteLine("Found {0} messages.\n", jsonObject.messages.total);

                    //Console.WriteLine("Response:\n {0}", jsonObject);

                    // See here; https://api.slack.com/methods/chat.delete
                    string deleteUrl = "https://slack.com/api/chat.delete?token={0}&ts={1}&channel={2}";

                    foreach (var message in jsonObject.messages.matches)
                    {
                        Console.WriteLine("[{0}] {1}: {2}", message.channel.name, message.username, message.text);

                        searchRequest = (HttpWebRequest)WebRequest.Create(string.Format(deleteUrl, token, message.ts, message.channel.id));
                        searchRequest.Timeout = 5000;
                        searchRequest.ReadWriteTimeout = 5000;
                        try
                        {
                            HttpWebResponse deleteResponse = (HttpWebResponse)searchRequest.GetResponse();
                            dynamic deleteObject = GetJSONObject(deleteResponse);

                            //if (deleteObject.ok == "true") //NewtonSoft.Json
                            if (deleteObject.ok) //SimpleJson
                            {
                                Console.WriteLine("Deleting... OK\n");
                            }
                            else
                            {
                                WriteError("Deleting... Error - {0}\n", deleteObject.error);
                            }

                            deleteResponse.Close();
                        }
                        catch (Exception e)
                        {
                            WriteError("Deleting... Exception Thrown: - {0}\n", e.Message);
                        }

                    }

                    Console.WriteLine("Delete files? (Y/N)");
                    var pressedKey = Console.ReadKey();
                    if (pressedKey.Key == ConsoleKey.Y)
                    {
                        // See here; https://api.slack.com/methods/files.delete
                        string deleteFileUrl = "https://slack.com/api/files.delete?token={0}&file={1}";

                        foreach (var file in jsonObject.files.matches)
                        {
                            Console.WriteLine("[FILE] {0}", file.id);

                            searchRequest = (HttpWebRequest)WebRequest.Create(string.Format(deleteFileUrl, token, file.id));
                            searchRequest.Timeout = 5000;
                            searchRequest.ReadWriteTimeout = 5000;
                            try
                            {
                                HttpWebResponse deleteResponse = (HttpWebResponse)searchRequest.GetResponse();
                                dynamic deleteObject = GetJSONObject(deleteResponse);

                                //if (deleteObject.ok == "true") //NewtonSoft.Json
                                if (deleteObject.ok) //SimpleJson
                                {
                                    Console.WriteLine("Deleting... OK\n");
                                }
                                else
                                {
                                    WriteError("Deleting... Error - {0}\n", deleteObject.error);
                                }

                                deleteResponse.Close();
                            }
                            catch (Exception e)
                            {
                                WriteError("Deleting... Exception Thrown: - {0}\n", e.Message);
                            }

                        }
                    }
                }
                else
                {
                    WriteError("Error: {0}.\n", jsonObject.error);
                }
            }

            Console.WriteLine("Complete. Press any key to close.");
            Console.ReadKey();
        }

        private static dynamic GetJSONObject(WebResponse webResponse)
        {
            Stream responseStream = webResponse.GetResponseStream();
            StreamReader objReader = new StreamReader(responseStream);

            string stringResponse = objReader.ReadToEnd();

            // Using NewtonSoft.Json (Json.NET)
            //dynamic jsonObject = JsonConvert.DeserializeObject(stringResponse);

            // Using SimpleJson
            // NOTE: Requires defining the SIMPLE_JSON_DYNAMIC preprocessor, as described here:
            // http://stackoverflow.com/questions/7853744/net-simplejson-deserialize-json-to-dynamic-object
            dynamic jsonObject = SimpleJson.DeserializeObject(stringResponse);

            return jsonObject;
        }

        private static void WriteError(string message, params object[] args)
        {
            ConsoleColor oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            Console.WriteLine(message, args);

            Console.ForegroundColor = oldColor;
        }
    }
}
