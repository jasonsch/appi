using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace PagliacciApi
{
    /*
     * Flow:
     * (1) GET the /Account/Login to seed the ASP.NET cookies.
     * (2) POST the email/password to /Account/Login to login.
     * (3) This takes us to the top-level ordering page that provides three options: "Click Here to Start", "Repeat Last Order", and "Check Gift Card Balance".
     * (4) "Click Here to Start" takes us to /Order/StepDelivery where we can choose our delivery address from previous orders or add a new one.
     *     Addresses are shown as strings but internally tracked by integer IDs. Note that there will be a hidden input field called SyncToken that we have to persist: <input type="hidden" name="SyncToken" value="yNj7FYPfKQHtesnGlastT1jPYQyDLSgrA8SVHdbV" />
     *
     * 
     */
    public class ApiClient
    {
        //
        // Used to repeat the user's previous order.
        //
        private string LastOrderID;

        private HttpClient Client;
        private readonly CookieContainer Cookies = new CookieContainer();
        private static readonly string LoginUrl = "https://order.pagliacci.com/Account/Login";

        // TODO
        #region Public properties
        public readonly List<DeliveryLocation> Addresses = new List<DeliveryLocation>();
        #endregion

        #region API
        public bool Connect(string Email, string Password)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            //
            // We hit the login page just to get any site/session cookies.
            //
            GetLoginPage();

            var Handler = new HttpClientHandler() { CookieContainer = Cookies };
            Client = new HttpClient(Handler);

            LoginUser(Email, Password);

            return true;
        }

        public Order CreateOrder()
        {
            // TODO -- Pass "this" to constructor?
            return new Order();
        }

        // {"Status":"0","Message":"","NewID":"-1","NewPrice":"0.00","ItemTitle":"","ItemDetail":""}
        private class LastOrderJson
        {
            public string Status;
            public string Message;
            public string NewID;
            public string NewPrice;
            public string ItemTitle;
            public string ItemDetail;
        }

        public Order RepeatLastOrder()
        {
            string Url = $"https://order.pagliacci.com/OrderEdit/clone/{LastOrderID}";
            string json = "";

            json = GetUrl(Url);
            LastOrderJson LastOrderResponse = JsonConvert.DeserializeObject<LastOrderJson>(json);
            if (Convert.ToInt32(LastOrderResponse.Status) != 0)
            {
                Console.WriteLine("Trying to repeat the last order failed with status {0}: {1}", LastOrderResponse.Status, LastOrderResponse.Message);
                return null;
            }

            string html = GetUrl("https://order.pagliacci.com/Order/StepOrder"); // TODO
            Console.WriteLine("html for steporder ==> " + html);

            Order o = new Order();

            // TODO
            return new Order();
        }
        #endregion

        #region Internal Worker Routines
        private void GetLoginPage()
        {
            GetUrl(LoginUrl);
        }

        private string GetUrl(string Url)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new System.Uri(Url));

            request.CookieContainer = Cookies;
            request.AllowAutoRedirect = true;
            request.KeepAlive = true;
            request.UserAgent = "Mozilla / 5.0(Windows NT 10.0; Win64; x64) AppleWebKit / 537.36(KHTML, like Gecko) Chrome / 68.0.3440.106 Safari / 537.36";
            request.UseDefaultCredentials = false;

            var Response = (HttpWebResponse)request.GetResponse();
            return new StreamReader(Response.GetResponseStream()).ReadToEnd();
        }

        // TODO -- Should be bool
        private void LoginUser(string Email, string Password)
        {
            var values = new Dictionary<string, string>
            {
                {"emailAddress", Email},
                {"password", Password}
            };

            var content = new FormUrlEncodedContent(values);

            var response = Client.PostAsync(LoginUrl, content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("response ==> {0}", responseString);

            LastOrderID = ParseOrderID(responseString);
        }

        // < div class="col-xs-10 col-sm-8"><input type = "button" value="Repeat Last Order" class="widebutton" onclick="cloneOrder(25638537);" /></div>
        private static string ParseOrderID(string html)
        {
            Match m;            m = Regex.Match(html, @"cloneOrder\((\d+)\)");            if (m.Success)
            {
                Console.WriteLine("Last order ID ==> " + m.Groups[1].Value); // TODO                return m.Groups[1].Value;            }            else
            {
                Console.WriteLine("Couldn't find previous order ID!");
                return null;            }        }
    #endregion

    #region Debug code
    // TODO
    private void DumpResponse(HttpWebResponse Response)
        {
            Console.WriteLine("Page ==> " + new System.IO.StreamReader(Response.GetResponseStream()).ReadToEnd());

            foreach (Cookie cook in Response.Cookies)
            {
                Console.WriteLine("Cookie:");
                Console.WriteLine("{0} = {1}", cook.Name, cook.Value);
                Console.WriteLine("Domain: {0}", cook.Domain);
                Console.WriteLine("Path: {0}", cook.Path);
                Console.WriteLine("Port: {0}", cook.Port);
                Console.WriteLine("Secure: {0}", cook.Secure);

                Console.WriteLine("When issued: {0}", cook.TimeStamp);
                Console.WriteLine("Expires: {0} (expired? {1})", cook.Expires, cook.Expired);
                Console.WriteLine("Don't save: {0}", cook.Discard);
                Console.WriteLine("Comment: {0}", cook.Comment);
                Console.WriteLine("Uri for comments: {0}", cook.CommentUri);
                Console.WriteLine("Version: RFC {0}", cook.Version == 1 ? "2109" : "2965");

                // Show the string representation of the cookie.
                Console.WriteLine("String: {0}", cook.ToString());
            }
        }
        // TODO
        #endregion
    }
}
