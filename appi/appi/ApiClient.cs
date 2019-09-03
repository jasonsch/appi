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
        public readonly List<PaymentMethod> PaymentMethods = new List<PaymentMethod>();
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

            // TODO -- Do on-demand.
            LoadAddresses();
            // LoadPaymentMethods();

            return true;
        }

        private void LoadPaymentMethods()
        {
            Match m;
            string html;
            string r = @"<tr id=""OnFileCredit-(\d+)"">\s*<td><img src=""/Images/(.*?)"" /></td>\s*<td>(.*?)</td>";

            html = GetUrl("https://order.pagliacci.com/Order/StepPayment");

            m = Regex.Match(html, r, RegexOptions.Singleline);
            while (m.Success)
            {
                // TODO
                // match ==> 3271336 / CreditSmallVisa.png / Credit card ending in 8451
                Console.WriteLine("payment: {0} / {1} / {2}", m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value); // TODO
                // TODO

                PaymentMethods.Add(new PaymentMethod(m.Groups[1].Value, GetCardTypeFromImageName(m.Groups[2].Value), m.Groups[3].Value));
                m = m.NextMatch();
            }
        }

        private static PaymentMethod.CardType GetCardTypeFromImageName(string ImageName)
        {
            /*
             * <img src="/Images/CreditSmallVisa.png" />
             * <img src="/Images/CreditSmallMaster.png" />
             * <img src="/Images/CreditSmallAmex.png" />            */
            if (ImageName == "CreditSmallVisa.png")
            {
                return PaymentMethod.CardType.Visa;
            }
            else if (ImageName == "CreditSmallMaster.png")
            {
                return PaymentMethod.CardType.Mastercard;
            }
            else
            {
                System.Diagnostics.Debug.Assert(ImageName == "CreditSmallAmex.png");
                return PaymentMethod.CardType.AmEx;
            }
        }

        private void LoadAddresses()
        {
            Match m;
            string html;

            html = GetUrl("https://order.pagliacci.com/Order/StepDelivery");

            m = Regex.Match(html, @"<p><input type=""radio"" name=""fileBuilding""\s* value=""(\d+)"" (checked=&quot;checked&quot;)?\s*/>\s*(.*?)\s*</p>", RegexOptions.Singleline);
            while (m.Success)
            {
                Console.WriteLine("address ==> {0} / {2} (default = {1})", m.Groups[1].Value, !string.IsNullOrEmpty(m.Groups[2].Value), m.Groups[3].Value);

                Addresses.Add(new DeliveryLocation(m.Groups[3].Value, m.Groups[1].Value, !string.IsNullOrEmpty(m.Groups[2].Value)));
                m = m.NextMatch();
            }
        }

        public Order CreateOrder()
        {
            return new Order(this, Addresses.Find(a => a.IsDefault));
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
            string Url = $"https://order.pagliacci.com/Order/OrderEdit/clone/{LastOrderID}";
            string json = "";

            json = GetUrl(Url);
            LastOrderJson LastOrderResponse = JsonConvert.DeserializeObject<LastOrderJson>(json);
            if (Convert.ToInt32(LastOrderResponse.Status) != 0)
            {
                Console.WriteLine("Trying to repeat the last order failed with status {0}: {1}", LastOrderResponse.Status, LastOrderResponse.Message);
                return null;
            }

            string html = GetUrl("https://order.pagliacci.com/Order/StepOrder"); // TODO -- We might not need to actually fetch this?

            Order o = new Order(this, Addresses.Find(a => a.IsDefault));

            LoadPaymentMethods();

            return o;
        }

        public bool RemovePaymentMethod(PaymentMethod Method)
        {
            /* TODO
            function removeCreditPayment()
{
    // ----- Remove the credit card payment. Don't bother if it isn't there.
    if ($("#Existing-CC").length == 0)
        return;

    // ----- Remove the card on the server side.
    var serverRequest = getUrl() + "OrderEdit/ccdel";
    var abortBlock = false;
    var alertText = "";
    $.ajax({
        url: serverRequest,
        async: false,
        dataType: "json"
    }).error(function () {
        alertText = "The payment could not be removed at this time due to a problem " +
            "with our system. Please contact us for assistance.";
        abortBlock = true;
    }).success(function (result) {
        // ----- The server returned a result, but it might be an error.
        if (result.Status != "0") {
            alertText = result.Message;
            abortBlock = true;
        }
    });
    if (abortBlock == true) {
        if (alertText.length > 0)
            showLineAlert(alertText);
        return;
    }

    // ----- Remove the payment from the display.
    $("#Existing-CC").remove();
    if ($("#ExistingPayments tr").length == 0)
    {
        $("#YesPaymentsApplied").hide();
        $("#NoPaymentsApplied").show();
    }
    refreshBalance();
    clearLineAlert();
}
             */

            return false; // TODO
        }

        public bool AddPaymentMethod()
        {
            /* TODO
            function addCreditCard()
{
    // ----- Add a new credit card payment to the order.
    var cardAmount = 0.0;
    var cardType = "";
    var lastFour = "";
    var cardNumber;
    var expireYear;
    var expireDate;
    var cardholderName;
    var billingZip;
    var isTemporary

    // ----- First, make sure a card hasn't been added already.
    if ($("#Existing-CC").length > 0)
    {
        showLineAlert("A credit card has already been applied to this order. " +
            "Please remove that card before adding a new credit card payment.");
        return;
    }

    // ----- Validate card number.
    cardNumber = digitsOnly($("#CreditNumber").val());
    if (cardNumber.length == 0)
    {
        showLineAlert("Please provide a valid credit card number.");
        return;
    }

    // ----- Validate expiration month and year.
    expireYear = digitsOnly($("#CreditExpireYear").val());
    if (expireYear.length != 4)
    {
        showLineAlert("Please provide a valid four-digit expiration year.");
        return;
    }
    if (parseInt(expireYear) < new Date().getFullYear())
    {
        showLineAlert("Credit card has expired. Please provide a valid expiration date.");
        return;
    }
    expireDate = $("#CreditExpireMonth").val() + "/" + expireYear;

    // ----- Validate cardholder name.
    cardholderName = $("#CreditName").val();
    if (cardholderName.length == 0)
    {
        showLineAlert("Please provide the cardholder name.");
        return;
    }

    // ----- Validate billing zip.
    billingZip = digitsOnly($("#CreditZip").val());
    if ((billingZip != "0") & (billingZip.length != 5))
    {
        showLineAlert("Please provide the billing Zip Code, or enter '0' for international cards.");
        return;
    }

    // ----- Get the temporary flag.
    if ($('#CreditTemp').is(":checked"))
        isTemporary = "Y";
    else
        isTemporary = "N";

    // ----- Bundle up the data for transmission.
    var bundle = toBase64(cardNumber + "\t" + expireDate + "\t" +
        cardholderName + "\t" + billingZip + "\t" + isTemporary);

    // ----- Add payment through server.
    var serverRequest = getUrl() + "OrderEdit/ccadd/new/" + bundle;
    var abortBlock = false;
    var alertText = "";
    $.ajax({
        url: serverRequest,
        async: false,
        dataType: "json"
    }).error(function () {
        alertText = "The payment could not be added at this time due to a problem " +
            "with our system. Please contact us for assistance.";
        abortBlock = true;
    }).success(function (result) {
        // ----- The server returned a result, but it might be an error.
        if (result.Status != "0") {
            alertText = result.Message;
            abortBlock = true;
        }
        else {
            cardAmount = parseFloat(result.NewPrice);
            cardType = result.ItemTitle;
            lastFour = result.ItemDetail;
        }
    });
    if (abortBlock == true) {
        if (alertText.length > 0)
            showLineAlert(alertText);
        return;
    }

    // ----- Update the display.
    displayAddedCreditCard(cardType, cardAmount, lastFour);
    hidePaymentPanels();
    clearLineAlert();
}
            */

            return false; // TODO
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
            // TODO
            // Console.WriteLine("response ==> {0}", responseString);

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

        internal bool SetDeliveryAddress(DeliveryLocation Address)
        {
            string Url = "https://order.pagliacci.com/Order/StepDelivery";
            var values = new Dictionary<string, string>
            {
                {"fileBuilding", Address.ID},
                {"forPickupField", "0" }
            };

            var content = new FormUrlEncodedContent(values);

            var response = Client.PostAsync(Url, content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;
            Console.WriteLine("SetDeliveryAddress got resonse ==> {0}", responseString); // TODO

            return true;
        }

        /*
         * TODO -- Accept coupons.
         * TODO -- Expose way to set "alternate phone".
         * TODO -- Expose way to add delivery instructions.
         */
        internal bool SubmitOrder(Order o)
        {
            if (!o.Address.IsDefault)
            {
                if (!SetDeliveryAddress(o.Address))
                {
                    Console.WriteLine("Couldn't set address to {0}!", o.Address);
                    return false;
                }
            }

            if (!UsePaymentMethod(o.Method))
            {
                Console.WriteLine("Couldn't set payment method"); // TODO
                return false;
            }

            string html = GetUrl("https://order.pagliacci.com/Order/StepConfirm");
            /*
            <form id="FormDetail" method="post" action="#">
            <input type="hidden" name="SyncToken" value="JWPINK3AWdXD5RRyJDYCqGWkAqVnViQc7D5qoldl" />            */
            Match m = Regex.Match(html, @"<input type=""hidden"" name=""SyncToken"" value=""(.*?)"" />");            if (!m.Success)
            {
                Console.WriteLine("Couldn't find sync token!"); // TODO                return false;            }            var values = new Dictionary<string, string>
            {
                {"SyncToken", m.Groups[1].Value}
            };

            var content = new FormUrlEncodedContent(values);

            var response = Client.PostAsync("https://order.pagliacci.com/Order/StepConfirm", content).Result;
            var responseString = response.Content.ReadAsStringAsync().Result;

            return true;
        }

        private bool UsePaymentMethod(PaymentMethod Method)
        {
            string Url = $"https://order.pagliacci.com/Order/OrderEdit/ccadd/file/{Method.ID}";
            string json = "";

            json = GetUrl(Url);
            LastOrderJson LastOrderResponse = JsonConvert.DeserializeObject<LastOrderJson>(json);
            /* TODO
            cardAmount = parseFloat(result.NewPrice);
            cardType = result.ItemTitle;
            lastFour = result.ItemDetail;
            */
            if (Convert.ToInt32(LastOrderResponse.Status) != 0)
            {
                Console.WriteLine("Trying to set the payment method failed with status {0}: {1}", LastOrderResponse.Status, LastOrderResponse.Message);
                return false;
            }

            return true;
        }
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