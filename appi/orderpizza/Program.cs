using System;
using PagliacciApi;

namespace orderpizza
{
    class Program
    {
        static void Main(string[] args)
        {
            ApiClient api = new ApiClient();

            string Email = Console.ReadLine();
            string Password = Console.ReadLine();

            // TODO
            api.Connect(Email, Password);

            Order o = api.RepeatLastOrder();

            Console.WriteLine("Repeating order. Delivering to {0}", o.Address.Address);

            foreach (var p in api.PaymentMethods)
            {
                Console.WriteLine("payment method ==> {0}", p.ToString());
            }

            o.Method = api.PaymentMethods[0];
            Console.WriteLine("Using method {0}", o.Method);
        }
    }
}
