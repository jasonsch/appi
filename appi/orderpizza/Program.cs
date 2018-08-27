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

            api.Connect(Email, Password);
        }
    }
}
